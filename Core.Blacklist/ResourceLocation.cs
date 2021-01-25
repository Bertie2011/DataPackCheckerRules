using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using DataPackChecker.Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Blacklist {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class ResourceLocation : CheckerRule {
        public override string Title => "Certain resource locations are blacklisted.";

        public override string Description => "Some resource locations are blacklisted. Each resource file path (starting with 'data/') is matched against a list of regular expressions until one matches. Based on a +/- prefix, the file will be allowed or disallowed. If none of the expressions match, the location is allowed. Use [^/]+ to allow any path element.";

        public override List<string> GoodExamples => new List<string> { @"{
    ""filters"":[
        ""+data/[^/]+/functions/abc/.*"",
        ""-data/[^/]+/functions/.*""
    ]
}
data/my_namespace/functions/abc/my_function.mcfunction
data/my_namespace/predicates/my_predicate.json" };

        public override List<string> BadExamples => new List<string> { @"<See configuration above>
data/my_namespace/functions/my_function.mcfunction" };

        public override List<string> ConfigExamples => new List<string> { @"{
    ""filters"": [
        ""+<regex A>"",
        ""-<regex B>"",
        ""+<regex C>""
    ]
}

Which means: If path matches regex A, allow (+ prefix).
Otherwise if path matches regex B, disallow (- prefix).
Otherwise if path matches regex C, allow (+ prefix).
Otherwise allow." };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.InvalidConfiguration<ResourceLocation>();
                return;
            }

            List<(Regex Regex, bool Allow)> filters = config.Value.GetProperty("filters").EnumerateArray().Select(f => (
                    new Regex($"^{f.GetString().Substring(1)}$"),
                    f.GetString().StartsWith('+')))
                .ToList();

            foreach (var ns in pack.Namespaces) {
                foreach (var resource in ns.AllResources) {
                    var path = ns.FolderPath + '/' + resource.FilePath;
                    bool allow = true;
                    foreach (var filter in filters) {
                        if (filter.Regex.IsMatch(path)) {
                            allow = filter.Allow;
                            break;
                        }
                    }
                    if (!allow) {
                        output.Error(resource, "Resource location is blacklisted.");
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config.TryValue(out JsonElement c) && c.IsObject()
                && c.TryAsArray("filters", out JsonElement filters)
                && filters.EnumerateArray().All(f => f.TryAsString(out string v)
                    && (v.StartsWith('+') || v.StartsWith('-')));
        }
    }
}
