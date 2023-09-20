using Logging;
using Utils;

namespace KubernetesWorkflow
{
    public interface IStartupWorkflow
    {
        RunningContainers Start(int numberOfContainers, Location location, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig);
        void Stop(RunningContainers runningContainers);
        void DownloadContainerLog(RunningContainer container, ILogHandler logHandler, int? tailLines = null);
        string ExecuteCommand(RunningContainer container, string command, params string[] args);
        void DeleteNamespace();
        void DeleteNamespacesStartingWith(string namespacePrefix);
    }

    public class StartupWorkflow : IStartupWorkflow
    {
        private readonly ILog log;
        private readonly WorkflowNumberSource numberSource;
        private readonly K8sCluster cluster;
        private readonly KnownK8sPods knownK8SPods;
        private readonly string k8sNamespace;
        private readonly RecipeComponentFactory componentFactory = new RecipeComponentFactory();

        internal StartupWorkflow(ILog log, WorkflowNumberSource numberSource, K8sCluster cluster, KnownK8sPods knownK8SPods, string k8sNamespace)
        {
            this.log = log;
            this.numberSource = numberSource;
            this.cluster = cluster;
            this.knownK8SPods = knownK8SPods;
            this.k8sNamespace = k8sNamespace;
        }

        public RunningContainers Start(int numberOfContainers, Location location, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            return K8s(controller =>
            {
                var recipes = CreateRecipes(numberOfContainers, recipeFactory, startupConfig);
                var runningPod = controller.BringOnline(recipes, location);
                var containers = CreateContainers(runningPod, recipes, startupConfig);

                if (startupConfig.CreateCrashWatcher) CreateCrashWatchers(controller, containers);

                var rc = new RunningContainers(startupConfig, runningPod, containers);
                cluster.Configuration.Hooks.OnContainersStarted(rc);
                return rc;
            });
        }

        public void Stop(RunningContainers runningContainers)
        {
            K8s(controller =>
            {
                controller.Stop(runningContainers.RunningPod);
                cluster.Configuration.Hooks.OnContainersStopped(runningContainers);
            });
        }

        public void DownloadContainerLog(RunningContainer container, ILogHandler logHandler, int? tailLines = null)
        {
            K8s(controller =>
            {
                controller.DownloadPodLog(container.Pod, container.Recipe, logHandler, tailLines);
            });
        }

        public string ExecuteCommand(RunningContainer container, string command, params string[] args)
        {
            return K8s(controller =>
            {
                return controller.ExecuteCommand(container.Pod, container.Recipe.Name, command, args);
            });
        }

        public void DeleteNamespace()
        {
            K8s(controller =>
            {
                controller.DeleteNamespace();
            });
        }

        public void DeleteNamespacesStartingWith(string namespacePrefix)
        {
            K8s(controller =>
            {
                controller.DeleteAllNamespacesStartingWith(namespacePrefix);
            });
        }

        private void CreateCrashWatchers(K8sController controller, RunningContainer[] runningContainers)
        {
            foreach (var container in runningContainers)
            {
                container.CrashWatcher = controller.CreateCrashWatcher(container);
            }
        }

        private RunningContainer[] CreateContainers(RunningPod runningPod, ContainerRecipe[] recipes, StartupConfig startupConfig)
        {
            log.Debug();
            return recipes.Select(r =>
            {
                var servicePorts = runningPod.GetServicePortsForContainerRecipe(r);
                log.Debug($"{r} -> service ports: {string.Join(",", servicePorts.Select(p => p.Number))}");

                var name = GetContainerName(r, startupConfig);

                return new RunningContainer(runningPod, r, servicePorts, name,
                    GetContainerExternalAddress(runningPod, servicePorts),
                    GetContainerInternalAddress(r));

            }).ToArray();
        }

        private string GetContainerName(ContainerRecipe recipe, StartupConfig startupConfig)
        {
            if (startupConfig == null) return "";
            if (!string.IsNullOrEmpty(startupConfig.NameOverride))
            {
                return $"<{startupConfig.NameOverride}{recipe.Number}>";
            }
            else
            {
                return $"<{recipe.Name}>";
            }
        }

        private Address GetContainerExternalAddress(RunningPod pod, Port[] servicePorts)
        {
            return new Address(
                pod.Cluster.HostAddress,
                GetServicePort(servicePorts));
        }

        private Address GetContainerInternalAddress(ContainerRecipe recipe)
        {
            var serviceName = "service-" + numberSource.WorkflowNumber;
            var port = GetInternalPort(recipe);

            return new Address(
                $"http://{serviceName}.{k8sNamespace}.svc.cluster.local",
                port);
        }

        private static int GetServicePort(Port[] servicePorts)
        {
            if (servicePorts.Any()) return servicePorts.First().Number;
            return 0;
        }

        private static int GetInternalPort(ContainerRecipe recipe)
        {
            if (recipe.ExposedPorts.Any()) return recipe.ExposedPorts.First().Number;
            return 0;
        }

        private ContainerRecipe[] CreateRecipes(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            log.Debug();
            var result = new List<ContainerRecipe>();
            for (var i = 0; i < numberOfContainers; i++)
            {
                var recipe = recipeFactory.CreateRecipe(i, numberSource.GetContainerNumber(), componentFactory, startupConfig);
                if (cluster.Configuration.AddAppPodLabel) recipe.PodLabels.Add("app", recipeFactory.AppName);
                cluster.Configuration.Hooks.OnContainerRecipeCreated(recipe);
                result.Add(recipe);
            }

            return result.ToArray();
        }

        private void K8s(Action<K8sController> action)
        {
            var controller = new K8sController(log, cluster, knownK8SPods, numberSource, k8sNamespace);
            action(controller);
            controller.Dispose();
        }

        private T K8s<T>(Func<K8sController, T> action)
        {
            var controller = new K8sController(log, cluster, knownK8SPods, numberSource, k8sNamespace);
            var result = action(controller);
            controller.Dispose();
            return result;
        }
    }

    public interface ILogHandler
    {
        void Log(Stream log);
    }

    public abstract class LogHandler : ILogHandler
    {
        public void Log(Stream log)
        {
            using var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                ProcessLine(line);
                line = reader.ReadLine();
            }
        }

        protected abstract void ProcessLine(string line);
    }
}
