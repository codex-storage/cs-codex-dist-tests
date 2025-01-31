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
        private readonly static NumberSource numberSource = new NumberSource(1);
        private readonly static FreePortFinder freePortFinder = new FreePortFinder();
        private readonly static object _lock = new object();
        private readonly static string dataParentDir = "codex_disttest_datadirs";
        private readonly static CodexExePath codexExePath = new CodexExePath();

        static BinaryCodexStarter()
        {
            StopAllCodexProcesses();
            DeleteParentDataDir();
        }

        public BinaryCodexStarter(IPluginTools pluginTools, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
        }

        public ICodexInstance[] BringOnline(CodexSetup codexSetup)
        {
            lock (_lock)
            {
                LogSeparator();
                Log($"Starting {codexSetup.Describe()}...");

                return StartCodexBinaries(codexSetup, codexSetup.NumberOfNodes);
            }
        }

        public void Decommission()
        {
            lock (_lock)
            {
                processControlMap.StopAll();
            }
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
            var name = GetName(config);
            var dataDir = Path.Combine(dataParentDir, $"datadir_{numberSource.GetNextNumber()}");
            var pconfig = new CodexProcessConfig(name, freePortFinder, dataDir);
            Log(pconfig);

            var factory = new CodexProcessRecipe(pconfig, codexExePath);
            var recipe = factory.Initialize(config);

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
                discoveryEndpoint: new Address("Disc", pconfig.LocalIpAddrs.ToString(), pconfig.DiscPort),
                apiEndpoint: new Address("Api", "http://" + local, pconfig.ApiPort),
                listenEndpoint: new Address("Listen", local, pconfig.ListenPort),
                ethAccount: null,
                metricsEndpoint: null
            );

            var pc = new BinaryProcessControl(pluginTools.GetLog(), process, pconfig);
            processControlMap.Add(instance, pc);

            return instance;
        }

        private string GetName(CodexStartupConfig config)
        {
            if (!string.IsNullOrEmpty(config.NameOverride))
            {
                return config.NameOverride + "_" + numberSource.GetNextNumber();
            }
            return "codex_" + numberSource.GetNextNumber();
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(CodexProcessConfig pconfig)
        {
            Log(
                "NodeConfig:Name=" + pconfig.Name +
                "ApiPort=" + pconfig.ApiPort +
                "DiscPort=" + pconfig.DiscPort +
                "ListenPort=" + pconfig.ListenPort +
                "DataDir=" + pconfig.DataDir
            );
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }

        private static void DeleteParentDataDir()
        {
            if (Directory.Exists(dataParentDir))
            {
                Directory.Delete(dataParentDir, true);
            }
        }

        private static void StopAllCodexProcesses()
        {
            var processes = Process.GetProcesses();
            var codexes = processes.Where(p =>
                p.ProcessName.ToLowerInvariant() == "codex" &&
                p.MainModule != null &&
                p.MainModule.FileName == codexExePath.Get()
            ).ToArray();

            foreach (var c in codexes)
            {
                c.Kill();
                c.WaitForExit();
            }
        }
    }
}
