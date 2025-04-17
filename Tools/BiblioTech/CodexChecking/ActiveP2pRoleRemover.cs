using BiblioTech.Rewards;
using Discord;
using Logging;
using System.Threading.Tasks;

namespace BiblioTech.CodexChecking
{
    public class ActiveP2pRoleRemover
    {
        private readonly Configuration config;
        private readonly ILog log;
        private readonly CheckRepo repo;

        public ActiveP2pRoleRemover(Configuration config, ILog log, CheckRepo repo)
        {
            this.config = config;
            this.log = log;
            this.repo = repo;
        }

        public void Start()
        {
            if (config.ActiveP2pRoleDurationMinutes > 0)
            {
                Task.Run(Worker);
            }
        }

        private void Worker()
        {
            var loopDelay = TimeSpan.FromMinutes(config.ActiveP2pRoleDurationMinutes) / 60;
            var min = TimeSpan.FromMinutes(10.0);
            if (loopDelay < min) loopDelay = min;

            try
            {
                while (true)
                {
                    Thread.Sleep(loopDelay);
                    CheckP2pRoleRemoval();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in {nameof(ActiveP2pRoleRemover)}: {ex}");
                Environment.Exit(1);
            }
        }

        private void CheckP2pRoleRemoval()
        {
            var expiryMoment = DateTime.UtcNow - TimeSpan.FromMinutes(config.ActiveP2pRoleDurationMinutes);

            Program.RoleDriver.IterateUsersWithRoles(
                (g, u, r) => OnUserWithRole(g, u, r, expiryMoment),
                Program.Config.ActiveP2pParticipantRoleId);
        }

        private async Task OnUserWithRole(IRoleGiver giver, IUser user, ulong roleId, DateTime expiryMoment)
        {
            var report = repo.GetOrCreate(user.Id);
            if (report.UploadCheck.CompletedUtc > expiryMoment) return;
            if (report.DownloadCheck.CompletedUtc > expiryMoment) return;

            await giver.RemoveActiveP2pParticipant(user.Id);
        }

        private bool ShouldRemoveRole(IUser user, DateTime expiryMoment)
        {
            var report = repo.GetOrCreate(user.Id);

            if (report.UploadCheck.CompletedUtc > expiryMoment) return false;
            if (report.DownloadCheck.CompletedUtc > expiryMoment) return false;

            return true;
        }
    }
}
