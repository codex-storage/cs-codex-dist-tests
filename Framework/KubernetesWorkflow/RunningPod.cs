using k8s;
using k8s.Models;
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

        [JsonIgnore]
        internal RunnerLocation RunnerLocation { get; set; }

        public Port GetServicePorts(ContainerRecipe recipe, string tag)
        {
            if (InternalService != null)
            {
                var p = InternalService.GetServicePortForRecipeAndTag(recipe, tag);
                if (p != null) return p;
            }

            if (ExternalService != null)
            {
                var p = ExternalService.GetServicePortForRecipeAndTag(recipe, tag);
                if (p != null) return p;
            }

            throw new Exception($"Unable to find port by tag '{tag}' for recipe '{recipe.Name}'.");
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

    public class RunningDeployment
    {
        public RunningDeployment(string name, string podLabel)
        {
            Name = name;
            PodLabel = podLabel;
        }

        public string Name { get; }
        public string PodLabel { get; }

        public V1Pod GetPod(K8sClient client, string k8sNamespace)
        {
            var allPods = client.Run(c => c.ListNamespacedPod(k8sNamespace));
            var pods = allPods.Items.Where(p => p.GetLabel(K8sController.PodLabelKey) == PodLabel).ToArray();

            if (pods.Length != 1) throw new Exception("Expected to find only 1 pod by podLabel.");
            return pods[0];
        }
    }

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

    public class PodInfo
    {
        public PodInfo(string name, string ip, string k8sNodeName)
        {
            Name = name;
            Ip = ip;
            K8SNodeName = k8sNodeName;
        }

        public string Name { get; }
        public string Ip { get; }
        public string K8SNodeName { get; }
    }
}
