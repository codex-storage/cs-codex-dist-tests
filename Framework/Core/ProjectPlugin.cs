using Utils;

namespace Core
{
    public interface IProjectPlugin
    {
        void Announce();
        void Decommission();
    }

    public interface IHasLogPrefix
    {
        string LogPrefix { get; }
    }

    public interface IHasMetadata
    {
        void AddMetadata(IAddMetadata metadata);
    }

    public static class ProjectPlugin
    {
        /// <summary>
        /// On some platforms and in some cases, not all required plugin assemblies are automatically loaded into the app domain.
        /// In this case, the runtime needs a slight push to load it before the EntryPoint class is instantiated.
        /// Used ProjectPlugin.Load<>() before you create an EntryPoint to ensure all plugins you want to use are loaded.
        /// </summary>
        public static void Load<T>() where T : IProjectPlugin
        {
            var type = typeof(T);
            FrameworkAssert.That(type != null, $"Unable to load plugin.");
        }
    }
}
