using KubernetesWorkflow.Recipe;

namespace KubernetesWorkflow.Types
{
    public class ContainerRecipePortMapEntry
    {
        public ContainerRecipePortMapEntry(int recipeNumber, Port[] ports)
        {
            RecipeNumber = recipeNumber;
            Ports = ports;
        }

        public int RecipeNumber { get; }
        public Port[] Ports { get; }
    }
}
