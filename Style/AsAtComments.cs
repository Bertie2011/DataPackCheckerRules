using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core.Style {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class AsAtComments : CheckerRule {
        public override string Title => "A function must start with comments describing the 'as/at' context.";

        public override string Description => "In each function a certain context is assumed, which includes the meaning of @s, @p and ~ ~ ~. Writing down that context at the top of the function might save some time debugging and help out if the function is revisited in the future.";

        public override List<string> GoodExamples { get; } = new List<string>() { "# As: Some entity\n# At: Some place", "# As/At: Some entity" };

        public override List<string> BadExamples { get; } = new List<string>() { "# As: Some entity (missing At)", "<command>\n#As/At: Some entity" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            foreach (var ns in pack.Namespaces) {
                foreach (var f in ns.Functions) {
                    bool asComment = false;
                    bool atComment = false;
                    for (int i = 0; i < Math.Min(f.Commands.Count, 2); i++) {
                        var c = f.Commands[i];
                        if (c.ContentType != Command.Type.Comment) continue;
                        if (c.Raw.StartsWith("As:", true, null) && c.Raw.Length > 4) asComment = true;
                        else if (c.Raw.StartsWith("At:", true, null) && c.Raw.Length > 4) atComment = true;
                        else if ((c.Raw.StartsWith("As/At:", true, null) || c.Raw.StartsWith("At/As:", true, null)) && c.Raw.Length > 7) atComment = asComment = true;
                    }

                    if (!asComment || !atComment) {
                        output.Error(ns, f, "Function does not start with as/at comments.");
                    }
                }
            }
        }
    }
}
