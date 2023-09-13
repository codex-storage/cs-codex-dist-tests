namespace Core
{
    public sealed class CoreInterface
    {
        private readonly EntryPoint entryPoint;

        internal CoreInterface(EntryPoint entryPoint)
        {
            this.entryPoint = entryPoint;
        }

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return entryPoint.GetPlugin<T>();
        }
    }
}
