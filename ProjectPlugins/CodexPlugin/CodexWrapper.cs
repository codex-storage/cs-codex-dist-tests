using CodexClient;
using CodexClient.Hooks;
using Core;
using Logging;

namespace CodexPlugin
{
    public class CodexWrapper
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly CodexHooksFactory hooksFactory;
        private DebugInfoVersion? versionResponse;

        public CodexWrapper(IPluginTools pluginTools, ProcessControlMap processControlMap, CodexHooksFactory hooksFactory)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
            this.hooksFactory = hooksFactory;
        }

        public string GetCodexId()
        {
            if (versionResponse != null) return versionResponse.Version;
            return "unknown";
        }

        public string GetCodexRevision()
        {
            if (versionResponse != null) return versionResponse.Revision;
            return "unknown";
        }

        public ICodexNodeGroup WrapCodexInstances(ICodexInstance[] instances)
        {
            var codexNodeFactory = new CodexNodeFactory(
                log: pluginTools.GetLog(),
                fileManager: pluginTools.GetFileManager(),
                hooksFactory: hooksFactory,
                httpFactory: pluginTools,
                processControlFactory: processControlMap);

            var group = CreateCodexGroup(instances, codexNodeFactory);

            pluginTools.GetLog().Log($"Codex version: {group.Version}");
            versionResponse = group.Version;

            return group;
        }

        private CodexNodeGroup CreateCodexGroup(ICodexInstance[] instances, CodexNodeFactory codexNodeFactory)
        {
            var nodes = instances.Select(codexNodeFactory.CreateCodexNode).ToArray();
            var group = new CodexNodeGroup(pluginTools, nodes);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                CodexNodesNotOnline(instances);
                throw;
            }

            return group;
        }

        private void CodexNodesNotOnline(ICodexInstance[] instances)
        {
            pluginTools.GetLog().Log("Codex nodes failed to start");
            var log = pluginTools.GetLog();
            foreach (var i in instances)
            {
                var pc = processControlMap.Get(i);
                pc.DownloadLog(log.CreateSubfile(i.Name + "_failed_to_start"));
            }
        }
    }
}
