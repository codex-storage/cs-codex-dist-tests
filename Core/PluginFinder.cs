using System.Reflection;

namespace Core
{
    public static class PluginFinder
    {
        private static Type[]? pluginTypes = null;

        public static Type[] GetPluginTypes()
        {
            if (pluginTypes != null) return pluginTypes;

            // Reflection can be costly. Do this only once.
            FindAndLoadPluginAssemblies();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            pluginTypes = assemblies.SelectMany(a => a.GetTypes().Where(t =>
                typeof(IProjectPlugin).IsAssignableFrom(t) &&
                !t.IsAbstract)
            ).ToArray();

            return pluginTypes;
        }

        private static void FindAndLoadPluginAssemblies()
        {
            var files = Directory.GetFiles(".");
            foreach (var file in files)
            {
                var f = file.ToLowerInvariant();
                if (f.Contains("plugin") && f.EndsWith("dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        Assembly.Load(name);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to load plugin from file '{name}'.", ex);
                    }
                }
            }
        }
    }
}
