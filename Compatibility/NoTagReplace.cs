using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Text.Json;

namespace Compatibility {
    public class NoTagReplace : CheckerRule {
        public override string Title => "Tags must not overwrite entries defined in lower priority data packs.";

        public override string Description => "Setting replace to true in a tag can prevent other data packs from working correctly.";

        public override string GoodExample => "{\"replace\":false,\"values\":[ ... ]}\n{\"values\":[ ... ]}";

        public override string BadExample => "{\"replace\":true,\"values\":[ ... ]}";

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            foreach (var ns in pack.Namespaces) {
                foreach (var tag in ns.Tags) {
                    if (tag.Content.TryGetProperty("replace", out JsonElement replace) && replace.GetBoolean()) {
                        output.Error(ns, tag, "Tag cannot replace contents of lower priority data packs, remove 'replace: true'.");
                    }
                }
            }
        }
    }
}
