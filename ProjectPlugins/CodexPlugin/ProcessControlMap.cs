using CodexClient;

namespace CodexPlugin
{
    public class ProcessControlMap : IProcessControlFactory
    {
        private readonly Dictionary<string, IProcessControl> processControlMap = new Dictionary<string, IProcessControl>();

        public void Add(ICodexInstance instance, IProcessControl control)
        {
            processControlMap.Add(instance.Name, control);
        }

        public void Remove(ICodexInstance instance)
        {
            processControlMap.Remove(instance.Name);
        }

        public IProcessControl CreateProcessControl(ICodexInstance instance)
        {
            return Get(instance);
        }

        public IProcessControl Get(ICodexInstance instance)
        {
            return processControlMap[instance.Name];
        }
    }
}
