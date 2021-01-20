using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Text.Json;
using System.Linq;

namespace Style {
    public class UninstallFunction : CheckerRule {
        public override string Title => "There must be an uninstall function.";

        public override string Description => "Providing an uninstall function will help remove traces and leave a clean world behind before the data pack is removed.";

        public override string GoodExample => "Having an uninstall.mcfunction file somewhere.";

        public override string BadExample => "Not having an uninstall function";

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!pack.Namespaces.Any(ns => ns.Functions.Any(f => f.Name == "uninstall"))) {
                output.Error("Data pack does not contain uninstall function.");
            }
        }
    }
}
