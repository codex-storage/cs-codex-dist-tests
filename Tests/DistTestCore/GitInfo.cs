using KubernetesWorkflow;
using LibGit2Sharp;
using System.Reflection;

namespace DistTestCore
{
    public static class GitInfo
    {
        private static string? status = null;

        public static string GetStatus()
        {
            if (status == null) status = DetermineStatus();
            return status;
        }

        private static string DetermineStatus()
        {
            var path = FindGitPath();
            if (path == null) return "unknown";

            using var repo = new Repository(path);
            var isModified = repo.RetrieveStatus().Any(IsModified);
            var sha = repo.Head.Tip.Sha.Substring(0, 7);

            return K8sNameUtils.Format(sha + (isModified ? "_modified" : "_clean"));
        }

        private static bool IsModified(StatusEntry e)
        {
            return e.State == FileStatus.ModifiedInIndex ||
                e.State == FileStatus.NewInIndex ||
                e.State == FileStatus.RenamedInIndex ||
                e.State == FileStatus.DeletedFromIndex ||

                e.State == FileStatus.ModifiedInWorkdir ||
                e.State == FileStatus.NewInWorkdir ||
                e.State == FileStatus.RenamedInWorkdir ||
                e.State == FileStatus.DeletedFromWorkdir;
        }

        private static string? FindGitPath()
        {
            var path = Repository.Discover(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            if (!string.IsNullOrEmpty(path)) return path;
                
            path = Repository.Discover(Directory.GetCurrentDirectory());
            if (!string.IsNullOrEmpty(path)) return path;

            return null;
        }
    }
}
