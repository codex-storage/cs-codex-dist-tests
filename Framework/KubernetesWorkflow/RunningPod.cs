using k8s;
using k8s.Models;
using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;
using Newtonsoft.Json;

namespace KubernetesWorkflow
{
    public class StartResult
    {
        public StartResult(
            K8sCluster cluster,
            ContainerRecipe[] containerRecipes, 
            RunningDeployment deployment,
            RunningService? internalService,
            RunningService? externalService)
        {
            Cluster = cluster;
            ContainerRecipes = containerRecipes;
            Deployment = deployment;
            InternalService = internalService;
            ExternalService = externalService;
        }

        public K8sCluster Cluster { get; }
        public ContainerRecipe[] ContainerRecipes { get; }
        public RunningDeployment Deployment { get; }
        public RunningService? InternalService { get; }
        public RunningService? ExternalService { get; }

        public Port GetInternalServicePorts(ContainerRecipe recipe, string tag)
        {
            if (InternalService != null)
            {
                var p = InternalService.GetServicePortForRecipeAndTag(recipe, tag);
                if (p != null) return p;
            }

            throw new Exception($"Unable to find internal port by tag '{tag}' for recipe '{recipe.Name}'.");
        }

        public Port GetExternalServicePorts(ContainerRecipe recipe, string tag)
        {
            if (ExternalService != null)
            {
                var p = ExternalService.GetServicePortForRecipeAndTag(recipe, tag);
                if (p != null) return p;
            }

            throw new Exception($"Unable to find external port by tag '{tag}' for recipe '{recipe.Name}'.");
        }

        public Port[] GetServicePortsForContainer(ContainerRecipe recipe)
        {
            if (InternalService != null)
            {
                var p = InternalService.GetServicePortsForRecipe(recipe);
                if (p.Any()) return p;
            }
            if (ExternalService != null)
            {
                var p = ExternalService.GetServicePortsForRecipe(recipe);
                if (p.Any()) return p;
            }

            return Array.Empty<Port>();
        }
    }
}
