using CodexClient;
using Core;
using Utils;
using System.Diagnostics;

namespace CodexPlugin
{
    public class BinaryCodexStarter : ICodexStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly NumberSource numberSource = new NumberSource(1);
        private readonly FreePortFinder freePortFinder = new FreePortFinder();

        public BinaryCodexStarter(IPluginTools pluginTools, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
        }

        public ICodexInstance[] BringOnline(CodexSetup codexSetup)
        {
            LogSeparator();
            Log($"Starting {codexSetup.Describe()}...");

            return StartCodexBinaries(codexSetup, codexSetup.NumberOfNodes);
        }

        private ICodexInstance[] StartCodexBinaries(CodexStartupConfig startupConfig, int numberOfNodes)
        {
            var result = new List<ICodexInstance>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                result.Add(StartBinary(startupConfig));
            }

            return result.ToArray();
        }

        private ICodexInstance StartBinary(CodexStartupConfig config)
        {
            var portMap = new CodexPortMap(freePortFinder);
            var factory = new CodexProcessRecipe(portMap, numberSource);
            var recipe = factory.Initialize(config);

            var name = "codex_" + numberSource.GetNextNumber();
            var startInfo = new ProcessStartInfo(
                fileName: recipe.Cmd,
                arguments: recipe.Args
            );
            //startInfo.UseShellExecute = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            if (process == null || process.HasExited)
            {
                throw new Exception("Failed to start");
            }

            var local = "localhost";
            var instance = new CodexInstance(
                name: name,
                imageName: "binary",
                startUtc: DateTime.UtcNow,
                discoveryEndpoint: new Address("Disc", local, portMap.DiscPort),
                apiEndpoint: new Address("Api", "http://" + local, portMap.ApiPort),
                listenEndpoint: new Address("Listen", local, portMap.ListenPort),
                ethAccount: null,
                metricsEndpoint: null
            );

            var pc = new BinaryProcessControl(process, name);
            processControlMap.Add(instance, pc);

            return instance;
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }
    }
}
