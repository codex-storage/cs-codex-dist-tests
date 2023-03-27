using ICSharpCode.SharpZipLib.Tar;
using k8s;
using System.Text;

namespace CodexDistTestCore
{
    // From: https://github.com/kubernetes-client/csharp/blob/master/examples/cp/Cp.cs
    public class K8sCp
    {
        private readonly Kubernetes client;

        public K8sCp(Kubernetes client)
        {
            this.client = client;
        }

        public async Task<int> CopyFileToPodAsync(string podName, string @namespace, string containerName, Stream inputFileStream, string destinationFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handler = new ExecAsyncCallback(async (stdIn, stdOut, stdError) =>
            {
                var fileInfo = new FileInfo(destinationFilePath);
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var tarOutputStream = new TarOutputStream(memoryStream, Encoding.Default))
                        {
                            tarOutputStream.IsStreamOwner = false;

                            var fileSize = inputFileStream.Length;
                            var entry = TarEntry.CreateTarEntry(fileInfo.Name);

                            entry.Size = fileSize;

                            tarOutputStream.PutNextEntry(entry);
                            await inputFileStream.CopyToAsync(tarOutputStream);
                            tarOutputStream.CloseEntry();
                        }

                        memoryStream.Position = 0;

                        await memoryStream.CopyToAsync(stdIn);
                        await stdIn.FlushAsync();
                    }

                }
                catch (Exception ex)
                {
                    throw new IOException($"Copy command failed: {ex.Message}");
                }

                using StreamReader streamReader = new StreamReader(stdError);
                while (streamReader.EndOfStream == false)
                {
                    string error = await streamReader.ReadToEndAsync();
                    throw new IOException($"Copy command failed: {error}");
                }
            });

            string destinationFolder = GetFolderName(destinationFilePath);

            return await client.NamespacedPodExecAsync(
                podName,
                @namespace,
                containerName,
                new string[] { "sh", "-c", $"tar xmf - -C {destinationFolder}" },
                false,
                handler,
                cancellationToken);
        }

        private static string GetFolderName(string filePath)
        {
            var folderName = Path.GetDirectoryName(filePath);

            return string.IsNullOrEmpty(folderName) ? "." : folderName;
        }
    }
}
