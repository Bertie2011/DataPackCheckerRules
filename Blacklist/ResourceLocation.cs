using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Blacklist {
    public class ResourceLocation : CheckerRule {
        public override string Title => "Certain resource locations are blacklisted.";

        public override string Description => "Some resource locations are blacklisted. Each resource file path (starting with 'data/') is matched against a list of regular expressions until one matches. Based on a +/- prefix, the file will be allowed or disallowed. If none of the expressions match, a decision is made based on the defaultAllow boolean. Use [^/]+ to allow any path element.";

        public override List<string> GoodExamples => new List<string> { @"{
    ""filters"":[
        ""+data/[^/]+/functions/abc/.*"",
        ""-data/[^/]+/functions/.*""
    ],
    ""defaultAllow"": true
}
data/my_namespace/functions/abc/my_function.mcfunction
data/my_namespace/predicates/my_predicate.json" };

        public override List<string> BadExamples => new List<string> {@"<See configuration above>
data/my_namespace/functions/my_function.mcfunction" };

        public override List<string> ConfigExamples => new List<string> { @"{
    ""filters"": [
        ""+<regex A>"",
        ""-<regex B>"",
        ""+<regex C>""
    ],
    ""defaultAllow"": false
}

Which means: If path matches regex A, allow (+ prefix).
Otherwise if path matches regex B, disallow (- prefix).
Otherwise if path matches regex C, allow (+ prefix).
Otherwise disallow (defaultAllow is false)." };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if(!ValidateConfig(config)) {
                output.InvalidConfiguration<ResourceLocation>();
                return;
            }

            List<(Regex Regex, bool Allow)> filters = config.Value.GetProperty("filters").EnumerateArray()
                .Select(f => (new Regex(f.GetString().Substring(1)), f.GetString().StartsWith('+'))).ToList();
            bool defaultAllow = config.Value.GetProperty("defaultAllow").GetBoolean();

            foreach (var ns in pack.Namespaces) {
                foreach (var resource in ns.AllResources) {
                    var path = ns.FolderPath + '/' + resource.FilePath;
                    bool allow = defaultAllow;
                    foreach (var filter in filters) {
                        if (filter.Regex.IsMatch(path)) {
                            allow = filter.Allow;
                            break;
                        }
                    }
                    if (!allow) {
                        output.Error(ns, resource, "Resource location is blacklisted.");
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config != null && config.Value.ValueKind == JsonValueKind.Object
                && config.Value.TryGetProperty("filters", out JsonElement filters)
                && filters.ValueKind == JsonValueKind.Array
                && filters.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String
                    && (v.GetString().StartsWith('+') || v.GetString().StartsWith('-')))
                && config.Value.TryGetProperty("defaultAllow", out JsonElement defaultAllow)
                && (defaultAllow.ValueKind == JsonValueKind.True || defaultAllow.ValueKind == JsonValueKind.False);
        }
    }
}
