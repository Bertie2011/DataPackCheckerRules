using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Uniqueness {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class ResourceLocation : CheckerRule {
        public override string Title => "All data pack files must be in a subfolder with the same name.";

        public override string Description => "Assuming the namespace is author specific, putting all resources in subfolders will prevent clashes with other data packs of the same author.";

        public override string GoodExample => "data/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/MyDataPack/name.json\ndata/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/MyDataPack/AnotherFolder/name.json";

        public override string BadExample => "data/<namespace>/functions/name.mcfunction\ndata/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/SomethingElse/name.json";

        public override string ConfigExample => @"Only one subfolder name per namespace is allowed without configuration.
Otherwise, the allowed subfolders can be extended or overriden by supplying a configuration like this:
{
    ""options"": [
        ""other subfolder"",
        ""...""
    ],
    ""extend"": true
}";

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.Error(new InvalidDataException("Configuration is not valid, see '-i Uniqueness.ResourceLocation'."));
                return;
            }

            foreach (var ns in pack.Namespaces) {
                if (ns.Name == "minecraft") continue;

                string subfolder = null;
                Namespace subfolderNS = null;
                Resource subfolderR = null;

                var resources = new List<Resource>()
                    .Concat(ns.Advancements)
                    .Concat(ns.DimensionData.Dimensions)
                    .Concat(ns.DimensionData.DimensionTypes)
                    .Concat(ns.Functions)
                    .Concat(ns.LootTables)
                    .Concat(ns.Predicates)
                    .Concat(ns.Recipes)
                    .Concat(ns.Structures)
                    .Concat(ns.Tags)
                    .Concat(ns.WorldGenData.Biomes)
                    .Concat(ns.WorldGenData.ConfiguredCarvers)
                    .Concat(ns.WorldGenData.ConfiguredFeatures)
                    .Concat(ns.WorldGenData.ConfiguredStructureFeatures)
                    .Concat(ns.WorldGenData.ConfiguredSurfaceBuilders)
                    .Concat(ns.WorldGenData.NoiseSettings)
                    .Concat(ns.WorldGenData.ProcessorLists)
                    .Concat(ns.WorldGenData.TemplatePools);

                foreach (var resource in resources) {
                    var path = resource.Path.Split('/', '\\')[0];
                    bool configMatch = config != null && config.Value.GetProperty("options").EnumerateArray().Any(v => v.GetString() == path);
                    bool append = config == null || config.Value.GetProperty("extend").GetBoolean();

                    if (string.IsNullOrWhiteSpace(resource.Path)) output.Error(ns, resource, "Resource is not in a subfolder");
                    else if (!FolderAllowed(path, subfolder, append, configMatch)) {
                        if (subfolder != null) output.Error(ns, resource, $"Resource subfolder does not match {output.GetResourceIdentifier(subfolderNS, subfolderR)} or any of the ones in the config file.");
                        else output.Error(ns, resource, $"Resource subfolder does not match any of the ones in the config file.");
                    } else if (subfolder == null && append && !configMatch) {
                        subfolder = path;
                        subfolderNS = ns;
                        subfolderR = resource;
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config == null ||
                (config.Value.ValueKind == JsonValueKind.Object &&
                    config.Value.TryGetProperty("options", out JsonElement options) &&
                    options.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String) &&
                    config.Value.TryGetProperty("extend", out JsonElement extend) &&
                    (extend.ValueKind == JsonValueKind.False || extend.ValueKind == JsonValueKind.True));
        }

        private bool FolderAllowed(string path, string subfolder, bool append, bool configMatch) {
            return (subfolder == null && (append || configMatch))
                || (subfolder != null && (subfolder == path || configMatch));
        }
    }
}
