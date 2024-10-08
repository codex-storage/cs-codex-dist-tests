using Utils;

namespace CodexContractsPlugin
{
    public class SelfUpdater
    {
        public void Update(string abi, string bytecode)
        {
            var filePath = GetMarketplaceFilePath();
            var content = GenerateContent(abi, bytecode);
            var contentLines = content.Split("\r\n");

            var beginWith = new string[]
            {
                "using Nethereum.ABI.FunctionEncoding.Attributes;",
                "using Nethereum.Contracts;",
                "using System.Numerics;",
                "",
                "// Generated code, do not modify.",
                "",
                "#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.",
                "namespace CodexContractsPlugin.Marketplace",
                "{"
            };

            var endWith = new string[]
            {
                "}",
                "#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable."
            };

            File.Delete(filePath);
            File.WriteAllLines(filePath,
                beginWith.Concat(
                contentLines.Concat(
                endWith))
            );

            throw new Exception("Oh no! CodexContracts were updated. Current build of CodexContractsPlugin is incompatible. " +
                "But fear not! SelfUpdater.cs has automatically updated the plugin. Just rebuild and rerun and it should work. " +
                "Just in case, manual update instructions are found here: 'CodexContractsPlugin/Marketplace/README.md'.");
        }

        private string GetMarketplaceFilePath()
        {
            var projectPluginDir = PluginPathUtils.ProjectPluginsDir;
            var path = Path.Combine(projectPluginDir, "CodexContractsPlugin", "Marketplace", "Marketplace.cs");
            if (!File.Exists(path)) throw new Exception("Marketplace file not found. Expected: " + path);
            return path;
        }

        private string GenerateContent(string abi, string bytecode)
        {
            var deserializer = new Nethereum.Generators.Net.GeneratorModelABIDeserialiser();
            var abiModel = deserializer.DeserialiseABI(abi);
            var abiCtor = abiModel.Constructor;
            var c = new Nethereum.Generators.CQS.ContractDeploymentCQSMessageGenerator(abiCtor, "namespace", bytecode, "Marketplace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
            var lines = "";
            lines += c.GenerateClass();
            lines += "\r\n";

            foreach (var eventAbi in abiModel.Events)
            {
                var d = new Nethereum.Generators.DTOs.EventDTOGenerator(eventAbi, "namespace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
                lines += d.GenerateClass();
                lines += "\r\n";
            }

            foreach (var errorAbi in abiModel.Errors)
            {
                var e = new Nethereum.Generators.DTOs.ErrorDTOGenerator(errorAbi, "namespace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
                lines += e.GenerateClass();
                lines += "\r\n";
            }

            foreach (var funcAbi in abiModel.Functions)
            {
                var f = new Nethereum.Generators.DTOs.FunctionOutputDTOGenerator(funcAbi, "namespace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
                var ff = new Nethereum.Generators.CQS.FunctionCQSMessageGenerator(funcAbi, "namespace", "funcoutput", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
                lines += f.GenerateClass();
                lines += "\r\n";
                lines += ff.GenerateClass();
                lines += "\r\n";
            }

            foreach (var structAbi in abiModel.Structs)
            {
                var g = new Nethereum.Generators.DTOs.StructTypeGenerator(structAbi, "namespace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
                lines += g.GenerateClass();
                lines += "\r\n";
            }

            return lines;
        }
    }
}
