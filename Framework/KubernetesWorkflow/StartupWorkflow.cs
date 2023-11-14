using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace KubernetesWorkflow
{
    public interface IStartupWorkflow
    {
        IKnownLocations GetAvailableLocations();
        RunningContainers Start(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig);
        RunningContainers Start(int numberOfContainers, ILocation location, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig);
        PodInfo GetPodInfo(RunningContainer container);
        PodInfo GetPodInfo(RunningContainers containers);
        CrashWatcher CreateCrashWatcher(RunningContainer container);
        void Stop(RunningContainers containers);
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
        private readonly string k8sNamespace;
        private readonly RecipeComponentFactory componentFactory = new RecipeComponentFactory();
        private readonly LocationProvider locationProvider;

        internal StartupWorkflow(ILog log, WorkflowNumberSource numberSource, K8sCluster cluster, string k8sNamespace)
        {
            this.log = log;
            this.numberSource = numberSource;
            this.cluster = cluster;
            this.k8sNamespace = k8sNamespace;

            locationProvider = new LocationProvider(log, K8s);
        }

        public IKnownLocations GetAvailableLocations()
        {
            return locationProvider.GetAvailableLocations();
        }

        public RunningContainers Start(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            return Start(numberOfContainers, KnownLocations.UnspecifiedLocation, recipeFactory, startupConfig);
        }

        public RunningContainers Start(int numberOfContainers, ILocation location, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            return K8s(controller =>
            {
                componentFactory.Update(controller);

                var recipes = CreateRecipes(numberOfContainers, recipeFactory, startupConfig);
                var startResult = controller.BringOnline(recipes, location);
                var containers = CreateContainers(startResult, recipes, startupConfig);

                var rc = new RunningContainers(startupConfig, startResult, containers);
                cluster.Configuration.Hooks.OnContainersStarted(rc);

                if (startResult.ExternalService != null)
                {
                    componentFactory.Update(controller);
                }
                return rc;
            });
        }

        public PodInfo GetPodInfo(RunningContainer container)
        {
            return K8s(c => c.GetPodInfo(container.RunningContainers.StartResult.Deployment));
        }

        public PodInfo GetPodInfo(RunningContainers containers)
        {
            return K8s(c => c.GetPodInfo(containers.StartResult.Deployment));
        }

        public CrashWatcher CreateCrashWatcher(RunningContainer container)
        {
            return K8s(c => c.CreateCrashWatcher(container));
        }

        public void Stop(RunningContainers runningContainers)
        {
            K8s(controller =>
            {
                controller.Stop(runningContainers.StartResult);
                cluster.Configuration.Hooks.OnContainersStopped(runningContainers);
            });
        }

        public void DownloadContainerLog(RunningContainer container, ILogHandler logHandler, int? tailLines = null)
        {
            K8s(controller =>
            {
                controller.DownloadPodLog(container, logHandler, tailLines);
            });
        }

        public string ExecuteCommand(RunningContainer container, string command, params string[] args)
        {
            return K8s(controller =>
            {
                return controller.ExecuteCommand(container, command, args);
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

        private RunningContainer[] CreateContainers(StartResult startResult, ContainerRecipe[] recipes, StartupConfig startupConfig)
        {
            log.Debug();
            return recipes.Select(r =>
            {
                var name = GetContainerName(r, startupConfig);
                var addresses = CreateContainerAddresses(startResult, r);
                log.Debug($"{r}={name} -> container addresses: {string.Join(Environment.NewLine, addresses.Select(a => a.ToString()))}");

                return new RunningContainer(name, r, addresses);

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

        private ContainerAddress[] CreateContainerAddresses(StartResult startResult, ContainerRecipe recipe)
        {
            var result = new List<ContainerAddress>();
            foreach (var exposedPort in recipe.ExposedPorts)
            {
                result.Add(new ContainerAddress(exposedPort.Tag, GetContainerExternalAddress(startResult, recipe, exposedPort.Tag), false));
                result.Add(new ContainerAddress(exposedPort.Tag, GetContainerInternalAddress(startResult, recipe, exposedPort.Tag), true));
            }
            foreach (var internalPort in recipe.InternalPorts)
            {
                result.Add(new ContainerAddress(internalPort.Tag, GetContainerInternalAddress(startResult, recipe, internalPort.Tag), true));
            }

            return result.ToArray();
        }

        private static Address GetContainerExternalAddress(StartResult startResult, ContainerRecipe recipe, string tag)
        {
            var port = startResult.GetExternalServicePorts(recipe, tag);

            return new Address(
                startResult.Cluster.HostAddress,
                port.Number);
        }

        private Address GetContainerInternalAddress(StartResult startResult, ContainerRecipe recipe, string tag)
        {
            var namespaceName = startResult.Cluster.Configuration.KubernetesNamespace;
            var serviceName = startResult.InternalService!.Name;
            var port = startResult.GetInternalServicePorts(recipe, tag);

            return new Address(
                $"http://{serviceName}.{namespaceName}.svc.cluster.local",
                port.Number);
        }
        
        private ContainerRecipe[] CreateRecipes(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            log.Debug();
            var result = new List<ContainerRecipe>();
            for (var i = 0; i < numberOfContainers; i++)
            {
                var recipe = recipeFactory.CreateRecipe(i, numberSource.GetContainerNumber(), componentFactory, startupConfig);
                CheckPorts(recipe);

                if (cluster.Configuration.AddAppPodLabel) recipe.PodLabels.Add("app", recipeFactory.AppName);
                cluster.Configuration.Hooks.OnContainerRecipeCreated(recipe);
                result.Add(recipe);
            }

            return result.ToArray();
        }

        private void CheckPorts(ContainerRecipe recipe)
        {
            var allTags =
                recipe.ExposedPorts.Concat(recipe.InternalPorts)
                .Select(p => K8sNameUtils.Format(p.Tag)).ToArray();

            if (allTags.Length != allTags.Distinct().Count())
            {
                throw new Exception("Duplicate port tags found in recipe for " + recipe.Name);
            }
        }

        private void K8s(Action<K8sController> action)
        {
            try
            {
                var controller = new K8sController(log, cluster, numberSource, k8sNamespace);
                action(controller);
                controller.Dispose();
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                log.Error(JsonConvert.SerializeObject(ex.Response));
                throw;
            }
        }

        private T K8s<T>(Func<K8sController, T> action)
        {
            try
            {
                var controller = new K8sController(log, cluster, numberSource, k8sNamespace);
                var result = action(controller);
                controller.Dispose();
                return result;
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                log.Error(JsonConvert.SerializeObject(ex.Response));
                throw;
            }
        }
    }
}
