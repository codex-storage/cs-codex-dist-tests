using BlockchainUtils;
using Core;

namespace GethPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static GethDeployment DeployGeth(this CoreInterface ci, Action<IGethSetup> setup)
        {
            return Plugin(ci).DeployGeth(setup);
        }

        public static IGethNode WrapGethDeployment(this CoreInterface ci, GethDeployment deployment, BlockCache blockCache)
        {
            return Plugin(ci).WrapGethDeployment(deployment, blockCache);
        }

        public static IGethNode StartGethNode(this CoreInterface ci, BlockCache blockCache, Action<IGethSetup> setup)
        {
            var deploy = DeployGeth(ci, setup);
            return WrapGethDeployment(ci, deploy, blockCache);
        }

        private static GethPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<GethPlugin>();
        }
    }
}
