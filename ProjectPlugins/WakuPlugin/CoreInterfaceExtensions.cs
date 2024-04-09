using Core;
using KubernetesWorkflow.Types;

namespace WakuPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainers[] DeployWakuNodes(this CoreInterface ci, int number, Action<IWakuSetup> setup)
        {
            return Plugin(ci).DeployWakuNodes(number, setup);
        }

        public static IWakuNode WrapWakuContainer(this CoreInterface ci, RunningContainer container)
        {
            return Plugin(ci).WrapWakuContainer(container);
        }

        public static IWakuNode StartWakuNode(this CoreInterface ci)
        {
            return ci.StartWakuNode(s => { });
        }

        public static IWakuNode StartWakuNode(this CoreInterface ci, Action<IWakuSetup> setup)
        {
            var rc = ci.DeployWakuNodes(1, setup);
            return ci.WrapWakuContainer(rc.First().Containers.First());
        }

        private static WakuPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<WakuPlugin>();
        }
    }
}
