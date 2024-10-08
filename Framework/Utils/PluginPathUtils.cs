namespace Utils
{
    public static class PluginPathUtils
    {
        private const string ProjectPluginsFolderName = "ProjectPlugins";
        private static string projectPluginsDir = string.Empty;

        public static string ProjectPluginsDir
        {
            get
            {
                if (string.IsNullOrEmpty(projectPluginsDir)) projectPluginsDir = FindProjectPluginsDir();
                return projectPluginsDir;
            }
        }

        private static string FindProjectPluginsDir()
        {
            var current = Directory.GetCurrentDirectory();
            while (true)
            {
                var localFolders = Directory.GetDirectories(current);
                var projectPluginsFolders = localFolders.Where(l => l.EndsWith(ProjectPluginsFolderName)).ToArray();
                if (projectPluginsFolders.Length == 1)
                {
                    return projectPluginsFolders.Single();
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    var msg = $"Unable to locate '{ProjectPluginsFolderName}' folder. Travelled up from: '{Directory.GetCurrentDirectory()}'";
                    Console.WriteLine(msg);
                    throw new Exception(msg);
                }

                current = parent.FullName;
            }
        }
    }
}
