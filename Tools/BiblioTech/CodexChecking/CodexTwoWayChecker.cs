﻿using CodexClient;
using FileUtils;
using Logging;
using Utils;

namespace BiblioTech.CodexChecking
{
    public interface ICheckResponseHandler
    {
        Task CheckNotStarted();
        Task NowCompleted(string checkName);
        Task GiveRoleReward();
        
        Task InvalidData();
        Task CouldNotDownloadCid();
        Task GiveCidToUser(string cid);
        Task GiveDataFileToUser(string fileContent);
        Task CodexUnavailable();

        Task ToAdminChannel(string msg);
    }

    public class CodexTwoWayChecker
    {
        private readonly ILog log;
        private readonly Configuration config;
        private readonly CheckRepo repo;
        private readonly CodexWrapper codexWrapper;

        public CodexTwoWayChecker(ILog log, Configuration config, CheckRepo repo, CodexWrapper codexWrapper)
        {
            this.log = log;
            this.config = config;
            this.repo = repo;
            this.codexWrapper = codexWrapper;
        }

        public async Task StartDownloadCheck(ICheckResponseHandler handler, ulong userId)
        {
            var check = repo.GetOrCreate(userId).DownloadCheck;
            if (IsUniqueDataStale(check))
            {
                check.UniqueData = GenerateUniqueData();
                repo.SaveChanges();
            }

            var cid = UploadData(check.UniqueData);
            if (cid == null)
            {
                await handler.CodexUnavailable();
                return;
            }

            await handler.GiveCidToUser(cid);
        }

        public async Task VerifyDownloadCheck(ICheckResponseHandler handler, ulong userId, string receivedData)
        {
            var check = repo.GetOrCreate(userId).DownloadCheck;
            if (string.IsNullOrEmpty(check.UniqueData))
            {
                await handler.CheckNotStarted();
                return;
            }

            Log($"Verifying for downloadCheck: received: '{receivedData}' check: '{check.UniqueData}'");
            if (string.IsNullOrEmpty(receivedData) || receivedData != check.UniqueData)
            {
                await handler.InvalidData();
                return;
            }

            await CheckNowCompleted(handler, check, userId, "DownloadCheck");
        }

        public async Task StartUploadCheck(ICheckResponseHandler handler, ulong userId)
        {
            var check = repo.GetOrCreate(userId).UploadCheck;
            if (IsUniqueDataStale(check))
            {
                check.UniqueData = GenerateUniqueData();
                repo.SaveChanges();
            }
            
            await handler.GiveDataFileToUser(check.UniqueData);
        }

        public async Task VerifyUploadCheck(ICheckResponseHandler handler, ulong userId, string receivedCid)
        {
            var check = repo.GetOrCreate(userId).UploadCheck;
            if (string.IsNullOrEmpty(receivedCid))
            {
                await handler.InvalidData();
                return;
            }

            var manifest = GetManifest(receivedCid);
            if (manifest == null)
            {
                await handler.CouldNotDownloadCid();
                return;
            }

            if (IsManifestLengthCompatible(handler, check, manifest))
            {
                var correct = IsContentCorrect(handler, check, receivedCid);
                if (!correct.HasValue) {
                    await handler.CodexUnavailable();
                    return;
                }
                if (correct.Value)
                {
                    await CheckNowCompleted(handler, check, userId, "UploadCheck");
                    return;
                }
            }

            await handler.InvalidData();
        }

        private string GenerateUniqueData()
        {
            return $"{RandomBusyMessage.Get().Substring(5)}{RandomUtils.GenerateRandomString(12)}";
        }

        private bool IsUniqueDataStale(TransferCheck check)
        {
            var expiry = DateTime.UtcNow - TimeSpan.FromMinutes(10.0);

            return
                string.IsNullOrEmpty(check.UniqueData) ||
                check.CompletedUtc < expiry;
        }

        private string? UploadData(string uniqueData)
        {
            var filePath = Path.Combine(config.ChecksDataPath, Guid.NewGuid().ToString());

            try
            {
                File.WriteAllText(filePath, uniqueData);
                var file = new TrackedFile(log, filePath, "checkData");

                return codexWrapper.OnCodex(node =>
                {
                    return node.UploadFile(file).Id;
                });
            }
            catch (Exception ex)
            {
                log.Error("Exception when uploading data: " + ex);
                throw;
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        private Manifest? GetManifest(string receivedCid)
        {
            try
            {
                return codexWrapper.OnCodex(node =>
                {
                    return node.DownloadManifestOnly(new ContentId(receivedCid)).Manifest;
                });
            }
            catch
            {
                return null;
            }
        }

        private bool IsManifestLengthCompatible(ICheckResponseHandler handler, TransferCheck check, Manifest manifest)
        {
            var dataLength = check.UniqueData.Length;
            var manifestLength = manifest.DatasetSize.SizeInBytes;

            Log($"Checking manifest length: dataLength={dataLength},manifestLength={manifestLength}");

            return
                manifestLength > (dataLength - 1) &&
                manifestLength < (dataLength + 1);
        }

        private bool? IsContentCorrect(ICheckResponseHandler handler, TransferCheck check, string receivedCid)
        {
            try
            {
                var content = codexWrapper.OnCodex(node =>
                {
                    var file = node.DownloadContent(new ContentId(receivedCid));
                    if (file == null) return string.Empty;
                    try
                    {
                        return File.ReadAllText(file.Filename).Trim();
                    }
                    finally
                    {
                        if (File.Exists(file.Filename)) File.Delete(file.Filename);
                    }
                });

                if (content == null) return null;

                Log($"Checking content: content={content},check={check.UniqueData}");
                return content == check.UniqueData;
            }
            catch
            { 
                return false;
            }
        }

        private async Task CheckNowCompleted(ICheckResponseHandler handler, TransferCheck check, ulong userId, string checkName)
        {
            await handler.NowCompleted(checkName);

            check.CompletedUtc = DateTime.UtcNow;
            repo.SaveChanges();

            await CheckUserForRoleRewards(handler, userId);
        }

        private async Task CheckUserForRoleRewards(ICheckResponseHandler handler, ulong userId)
        {
            var check = repo.GetOrCreate(userId);

            if (check.UploadCheck.CompletedUtc != DateTime.MinValue &&
                check.DownloadCheck.CompletedUtc != DateTime.MinValue)
            {
                await handler.GiveRoleReward();
            }
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
