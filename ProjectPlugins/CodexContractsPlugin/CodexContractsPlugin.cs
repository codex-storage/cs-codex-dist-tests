﻿using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public class CodexContractsPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly CodexContractsStarter starter;

        public CodexContractsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new CodexContractsStarter(tools);
        }

        public string LogPrefix => "(CodexContracts) ";

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            tools.GetLog().Log($"Loaded Codex-Marketplace SmartContracts");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexcontractsid", "dynamic");
        }

        public void Decommission()
        {
        }

        public CodexContractsDeployment DeployContracts(CoreInterface ci, IGethNode gethNode, CodexClient.DebugInfoVersion versionInfo)
        {
            return starter.Deploy(ci, gethNode, versionInfo);
        }

        public ICodexContracts WrapDeploy(IGethNode gethNode, CodexContractsDeployment deployment)
        {
            deployment = SerializeGate.Gate(deployment);
            return starter.Wrap(gethNode, deployment);
        }
    }
}
