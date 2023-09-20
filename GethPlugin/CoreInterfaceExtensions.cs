using Core;

namespace GethPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IGethDeployment DeployGeth(this CoreInterface ci, Action<IGethSetup> setup)
        {
            return Plugin(ci).DeployGeth(setup);
        }

        public static IGethNode WrapGethDeployment(this CoreInterface ci, IGethDeployment deployment)
        {
            return Plugin(ci).WrapGethDeployment(deployment);
        }

        public static IGethNode StartGethNode(this CoreInterface ci, Action<IGethSetup> setup)
        {
            var deploy = DeployGeth(ci, setup);
            return WrapGethDeployment(ci, deploy);
        }

        private static GethPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<GethPlugin>();
        }
    }
}
