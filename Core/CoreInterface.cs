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
            var keys = coreAssociations.Where(p => p.Value == entryPoint).ToArray();
            if (keys.Length == 0) return;
            
            foreach (var key in keys)
            {
                coreAssociations.Remove(key.Key);
            }
        }
    }
}
