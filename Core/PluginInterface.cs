namespace Core
{
    public abstract class PluginInterface
    {
        public abstract T GetPlugin<T>() where T : IProjectPlugin;
    }
}
