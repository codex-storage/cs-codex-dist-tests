namespace KubernetesWorkflow
{
    public class StartupWorkflow
    {
        private readonly NumberSource containerNumberSource;
        private readonly K8sController k8SController;
        private readonly RecipeComponentFactory componentFactory = new RecipeComponentFactory();

        public StartupWorkflow(NumberSource containerNumberSource, K8sController k8SController)
        {
            this.containerNumberSource = containerNumberSource;
            this.k8SController = k8SController;
        }

        public RunningContainers Start(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            var recipes = CreateRecipes(numberOfContainers, recipeFactory, startupConfig);

            var runningPod = k8SController.BringOnline(recipes);

            return new RunningContainers(startupConfig, runningPod, CreateContainers(runningPod, recipes));
        }

        private static RunningContainer[] CreateContainers(RunningPod runningPod, ContainerRecipe[] recipes)
        {
            return recipes.Select(r => new RunningContainer(runningPod, r, runningPod.GetServicePortsForContainerRecipe(r))).ToArray();
        }

        private ContainerRecipe[] CreateRecipes(int numberOfContainers, ContainerRecipeFactory recipeFactory, StartupConfig startupConfig)
        {
            var result = new List<ContainerRecipe>();
            for (var i = 0; i < numberOfContainers; i++)
            {
                result.Add(recipeFactory.CreateRecipe(containerNumberSource.GetNextNumber(), componentFactory, startupConfig));
            }

            return result.ToArray();
        }
    }
}
