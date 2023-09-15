using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public static class CoreInterfaceExtensions
    {
        //public static RunningContainers[] StartCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        //{
        //    return Plugin(ci).StartCodexNodes(number, setup);
        //}

        //public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, RunningContainers[] containers)
        //{
        //    return Plugin(ci).WrapCodexContainers(containers);
        //}

        //public static IOnlineCodexNode SetupCodexNode(this CoreInterface ci)
        //{
        //    return ci.SetupCodexNodes(1)[0];
        //}

        //public static IOnlineCodexNode SetupCodexNode(this CoreInterface ci, Action<ICodexSetup> setup)
        //{
        //    return ci.SetupCodexNodes(1, setup)[0];
        //}

        //public static ICodexNodeGroup SetupCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        //{
        //    var rc = ci.StartCodexNodes(number, setup);
        //    return ci.WrapCodexContainers(rc);
        //}

        //public static ICodexNodeGroup SetupCodexNodes(this CoreInterface ci, int number)
        //{
        //    return ci.SetupCodexNodes(number, s => { });
        //}

        //private static CodexPlugin Plugin(CoreInterface ci)
        //{
        //    return ci.GetPlugin<CodexPlugin>();
        //}
    }
}
