using KubernetesWorkflow.Recipe;

namespace KubernetesWorkflow.Types
{
    public class RunningService
    {
        public RunningService(string name, List<ContainerRecipePortMapEntry> result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; }
        public List<ContainerRecipePortMapEntry> Result { get; }

        public Port? GetServicePortForRecipeAndTag(ContainerRecipe recipe, string tag)
        {
            return GetServicePortsForRecipe(recipe).SingleOrDefault(p => p.Tag == tag);
        }

        public Port[] GetServicePortsForRecipe(ContainerRecipe recipe)
        {
            return Result
                .Where(p => p.RecipeNumber == recipe.Number)
                .SelectMany(p => p.Ports)
                .ToArray();
        }
    }
}
