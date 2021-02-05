using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using DataPackChecker.Shared.Data.Resources;

namespace Core.Style {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class UninstallFunction : CheckerRule {
        public override string Title => "There must be an uninstall function.";

        public override string Description => "Providing an uninstall function will help remove traces and leave a clean world behind before the data pack is removed. An uninstall function must contain commands that remove all created in-game resources. The function file can be located in any directory.";

        public override List<string> GoodExamples { get; } = new List<string>() { "load.mcfunction: scoreboard objectives add my_obj dummy\nuninstall.mcfunction: scoreboard objectives remove my_obj" };

        public override List<string> BadExamples { get; } = new List<string>() { "Not having an uninstall function", "load.mcfunction: team add my_team\nuninstall.mcfunction: <missing team remove command>" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            var uninstalls = pack.Namespaces.SelectMany(ns => ns.Functions).Where(f => f.Name == "uninstall").ToList();
            if (uninstalls.Count > 1) {
                output.Error("Data pack cannot contain more than one uninstall function:\n" +
                    string.Join('\n', uninstalls.Select(f => f.NamespacedIdentifier)));
            } else if (uninstalls.Count == 0) {
                output.Error("Data pack does not contain uninstall function.");
            }
            var uninstall = uninstalls[0];
            var missing = new HashSet<string>();

            foreach (var ns in pack.Namespaces) {
                foreach (var function in ns.Functions) {
                    foreach (var command in function.CommandsFlat) {
                        if (command.ContentType != Command.Type.Command) continue;
                        var uninstallCommand = TryUninstall(command);
                        if (uninstallCommand == null) continue;
                        if (!uninstall.Commands.Any(c => c.Raw == uninstallCommand) && missing.Add(uninstallCommand)) {
                            output.Error(uninstall, $"Uninstall function does not contain '{uninstallCommand}' for {function.NamespacedIdentifier} line {command.Line}.");
                        }
                    }
                }
            }
        }

        private string TryUninstall(Command command) {
            if (command.ContentType != Command.Type.Command) return null;
            else if (command.Raw.StartsWith("scoreboard objectives add ")) {
                return $"scoreboard objectives remove {command.Arguments[2]}";
            } else if (command.Raw.StartsWith("bossbar add ")) {
                return $"bossbar remove {command.Arguments[1]}";
            } else if (command.Raw.StartsWith("team add ")) {
                return $"team remove {command.Arguments[1]}";
            } else if (command.Raw.StartsWith("data modify storage ")) {
                var identifier = command.Arguments[3].Split(new char[] { '.', '[', '{' }, 2)[0];
                return $"data remove storage {command.Arguments[2]} {identifier}";
            } else return null;
        }
    }
}
