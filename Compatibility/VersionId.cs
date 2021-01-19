using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Compatibility {
    public class VersionId : CheckerRule {
        public override string Title => "The version identifier must be correct.";

        public override string Description => "The version identifier 'pack_format' in pack.mcmeta has to match the number set in the configuration.";

        public override string GoodExample => "Configuration number: 7 AND Data pack number: 7";

        public override string BadExample => "Configuration number: 7 AND Data pack number: 6";

        public override string ConfigExample => @"{
    ""version"": 7
}";

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (config == null || config.Value.ValueKind != JsonValueKind.Object
                || !config.Value.TryGetProperty("version", out JsonElement version)
                || version.ValueKind != JsonValueKind.Number) {
                output.InvalidConfiguration<VersionId>();
            } else {
                var versionConfig = config.Value.GetProperty("version").GetInt32();
                var versionPack = pack.Meta.GetProperty("pack").GetProperty("pack_format").GetInt32();
                if (versionConfig != versionPack) {
                    output.Error(new ArgumentException("The data pack version is not " + versionConfig));
                }
            }
        }
    }
}
