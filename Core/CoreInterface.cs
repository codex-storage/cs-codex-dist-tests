namespace Core
{
    public class CoreInterface
    {
        private static readonly Dictionary<CoreInterface, EntryPoint> coreAssociations = new Dictionary<CoreInterface, EntryPoint>();

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return coreAssociations[this].GetPlugin<T>();
        }

        internal static void Associate(CoreInterface coreInterface, EntryPoint entryPoint)
        {
            coreAssociations.Add(coreInterface, entryPoint);
        }

        internal static void Desociate(EntryPoint entryPoint)
        {
            var key = coreAssociations.Single(p => p.Value == entryPoint).Key;
            coreAssociations.Remove(key);
        }
    }
}
