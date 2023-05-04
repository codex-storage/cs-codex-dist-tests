using Logging;

namespace KubernetesWorkflow
{
    public class StartupWorkflow
    {
        private readonly BaseLog log;
        private readonly WorkflowNumberSource numberSource;
        private readonly K8sCluster cluster;
        private readonly KnownK8sPods knownK8SPods;
        private readonly string testNamespace;
        private readonly RecipeComponentFactory componentFactory = new RecipeComponentFactory();

        internal StartupWorkflow(BaseLog log, WorkflowNumberSource numberSource, K8sCluster cluster, KnownK8sPods knownK8SPods, string testNamespace)
        {
            this.log = log;
            this.numberSource = numberSource;
            this.cluster = cluster;
            this.knownK8SPods = knownK8SPods;
            this.testNamespace = testNamespace;
        }

        public RunningContainers Start(int numberOfContainers, Location location, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            return K8s(controller =>
            {
                var recipes = CreateRecipes(numberOfContainers, recipeFactory, startupConfig);

                var runningPod = controller.BringOnline(recipes, location);

                return new RunningContainers(startupConfig, runningPod, CreateContainers(runningPod, recipes, startupConfig));
            });
        }

        public void Stop(RunningContainers runningContainers)
        {
            K8s(controller =>
            {
                controller.Stop(runningContainers.RunningPod);
            });
        }

        public void DownloadContainerLog(RunningContainer container, ILogHandler logHandler)
        {
            K8s(controller =>
            {
                controller.DownloadPodLog(container.Pod, container.Recipe, logHandler);
            });
        }

        public string ExecuteCommand(RunningContainer container, string command, params string[] args)
        {
            return K8s(controller =>
            {
                return controller.ExecuteCommand(container.Pod, container.Recipe.Name, command, args);
            });
        }

        public void DeleteAllResources()
        {
            K8s(controller =>
            {
                controller.DeleteAllResources();
            });
        }

        public void DeleteTestResources()
        {
            K8s(controller =>
            {
                controller.DeleteTestNamespace();
            });
        }

        private RunningContainer[] CreateContainers(RunningPod runningPod, ContainerRecipe[] recipes, StartupConfig startupConfig)
        {
            log.Debug();
            return recipes.Select(r =>
            {
                var servicePorts = runningPod.GetServicePortsForContainerRecipe(r);
                log.Debug($"{r} -> service ports: {string.Join(",", servicePorts.Select(p => p.Number))}");

                return new RunningContainer(runningPod, r, servicePorts, startupConfig);
            }).ToArray();
        }

        private ContainerRecipe[] CreateRecipes(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            log.Debug();
            var result = new List<ContainerRecipe>();
            for (var i = 0; i < numberOfContainers; i++)
            {
                result.Add(recipeFactory.CreateRecipe(i, numberSource.GetContainerNumber(), componentFactory, startupConfig));
            }

            return result.ToArray();
        }

        private void K8s(Action<K8sController> action)
        {
            var controller = new K8sController(log, cluster, knownK8SPods, numberSource, testNamespace);
            action(controller);
            controller.Dispose();
        }

        private T K8s<T>(Func<K8sController, T> action)
        {
            var controller = new K8sController(log, cluster, knownK8SPods, numberSource, testNamespace);
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
