using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Core.Compatibility {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class NoTagReplace : CheckerRule {
        public override string Title => "Tags must not overwrite entries defined in lower priority data packs.";

        public override string Description => "Setting 'replace' to true in a tag can prevent other data packs from working correctly.";

        public override List<string> GoodExamples { get; } = new List<string>() { "{\"replace\":false,\"values\":[ ... ]}", "{\"values\":[ ... ]}" };

        public override List<string> BadExamples { get; } = new List<string>() { "{\"replace\":true,\"values\":[ ... ]}" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            foreach (var ns in pack.Namespaces) {
                foreach (var tag in ns.TagData.AllTags) {
                    if (tag.Content.TryGetProperty("replace", out JsonElement replace) && replace.GetBoolean()) {
                        output.Error(ns, tag, "Tag cannot replace contents of lower priority data packs, remove 'replace: true'.");
                    }
                }
            }
        }
    }
}
