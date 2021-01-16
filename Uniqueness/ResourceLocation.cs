using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Uniqueness {
    public class ResourceLocation : CheckerRule {
        public override string Title => "All data pack files must be in a subfolder with the same name.";

        public override string Description => "Assuming the namespace is author specific, putting all resources in subfolders will prevent clashes with other data packs of the same author.";

        public override string GoodExample => "data/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/MyDataPack/name.json\n\tdata/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/MyDataPack/AnotherFolder/name.json";

        public override string BadExample => "data/<namespace>/functions/name.mcfunction\n\tdata/<namespace>/functions/MyDataPack/name.mcfunction AND data/<namespace>/predicates/SomethingElse/name.json";

        public override void Run(DataPack pack, JsonElement config, Output output) {
            string subfolder = null;
            Namespace subfolderNS = null;
            Resource subfolderR = null;
            foreach (var ns in pack.Namespaces) {
                if (ns.Name == "minecraft") continue;
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
                    if (string.IsNullOrWhiteSpace(resource.Path)) output.Error(ns, resource, "Resource is not in a subfolder");
                    else if (subfolder != null && resource.Path.Split('/', '\\')[0] != subfolder) {
                        output.Error(ns, resource, $"Resource does not have the same subfolder as {output.GetResourceIdentifier(subfolderNS, subfolderR)}");
                    } else {
                        subfolder = resource.Path.Split('/', '\\')[0];
                        subfolderNS = ns;
                        subfolderR = resource;
                    }
                }
            }
        }
    }
}
