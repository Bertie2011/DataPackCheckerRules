﻿using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using DataPackChecker.Shared.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Core.Uniqueness {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class ResourceLocation : CheckerRule {
        public override string Title => "All data pack files must be in a subfolder with the same name.";

        public override string Description => "Assuming the namespace is author specific, putting all resources in subfolders will prevent clashes with other data packs of the same author.\nThe minecraft namespace is not checked.";

        public override List<string> GoodExamples { get; } = new List<string>() { "data/<namespace>/functions/MyDataPack/name.mcfunction\ndata/<namespace>/predicates/MyDataPack/name.json", "data/<namespace>/functions/MyDataPack/name.mcfunction\ndata/<namespace>/predicates/MyDataPack/AnotherFolder/name.json" };

        public override List<string> BadExamples { get; } = new List<string>() { "data/<namespace>/functions/name.mcfunction", "data/<namespace>/functions/MyDataPack/name.mcfunction\ndata/<namespace>/predicates/SomethingElse/name.json" };

        public override List<string> ConfigExamples { get; } = new List<string>() { @"Only one subfolder name per namespace is allowed without configuration.
Otherwise, the allowed subfolders can be extended or overriden by supplying a configuration like this:
{
    ""options"": [
        ""other subfolder"",
        ...
    ],
    ""extend"": true
}" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.InvalidConfiguration<ResourceLocation>();
                return;
            }

            foreach (var ns in pack.Namespaces) {
                if (ns.Name == "minecraft") continue;

                string subfolder = null;
                Resource subfolderR = null;

                foreach (var resource in ns.AllResources) {
                    var path = resource.IdentifierPath.Split('/', '\\')[0];
                    if (string.IsNullOrWhiteSpace(resource.IdentifierPath)) {
                        output.Error(resource, "Resource is not in a subfolder");
                        continue;
                    }

                    bool configMatch = config != null && config.Value.GetProperty("options").EnumerateArray().Any(v => v.GetString() == path);
                    bool append = config == null || config.Value.GetProperty("extend").GetBoolean();

                    if (!FolderAllowed(path, subfolder, append, configMatch)) {
                        if (subfolder != null) output.Error(resource, $"Resource subfolder does not match {subfolderR.NamespacedIdentifier} or any of the ones in the config file.");
                        else output.Error(resource, $"Resource subfolder does not match any of the ones in the config file.");
                    } else if (subfolder == null && append && !configMatch) {
                        subfolder = path;
                        subfolderR = resource;
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config == null || (config.TryValue(out JsonElement c) && c.IsObject() &&
                    c.TryAsArray("options", out JsonElement options) &&
                    options.EnumerateArray().All(v => v.IsString()) &&
                    c.IsBool("extend"));
        }

        private bool FolderAllowed(string path, string subfolder, bool append, bool configMatch) {
            return configMatch || subfolder == null && append
                || subfolder != null && subfolder == path;
        }
    }
}
