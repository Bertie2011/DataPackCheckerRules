using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Uniqueness {
    public class Identifiers : CheckerRule {
        public override string Title => "All in-game resources must be prefixed by a short identifier and a separator.";

        public override string Description => "In order to prevent clashes with resources of other data packs, all in-game resources should be prefixed.\nPossible separators are: . _ -";

        public override string GoodExample => "abc_my_objective AND abc_my_tag\nxyz.my.objective AND xyz.my.tag";

        public override string BadExample => "someObjective\nabc_my_objective AND xyz_my_tag";

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            throw new NotImplementedException();
        }
    }
}
