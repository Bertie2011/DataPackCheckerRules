using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using DataPackChecker.Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Blacklist {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class Identifier : CheckerRule {
        private static readonly char[] PrefixSeparators = new char[] { '.', '-', '_' };
        public override string Title => "Some in-game resource identifiers are not allowed to be used.";

        public override string Description => @"Not all identifiers of in-game resources like scoreboard objectives are allowed to be used in resource modifying commands. Based on identifier type, the 'namespaced' or 'plain' list is consulted from the configuration. Each list contains regular expressions, prefixed with + (allow) or - (disallow). A decision is made based on the first matching regex and if no match is found the identifier is allowed.

Plain:
- scoreboard objectives
- tags
- teams

Namespaced:
- bossbars
- data storage";

        public override List<string> GoodExamples { get; } = new List<string>() { @"{
    ""namespaced"": [],
    ""plain"": [
        ""+abc.*"",
        ""-ab.*""
    ]
}
scoreboard objectives add abc_objective1 dummy
scoreboard objectives add xyz_objective2 dummy" };

        public override List<string> BadExamples { get; } = new List<string>() { @"<See configuration above>
scoreboard objectives add abx_objective3 dummy" };

        public override List<string> ConfigExamples { get; } = new List<string>() { @"{
    ""namespaced"": [
        ""-<regex A>"",
        ""+<regex B>"",
        ""-<regex C>"",
        ...
    ],
    ""plain"": [
        ""-<regex A>"",
        ""+<regex B>"",
        ""-<regex C>"",
        ...
    ]
}" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.InvalidConfiguration<Identifier>();
                return;
            }

            List<(Regex Regex, bool Allow)> namespacedFilters = ParseRegexAllowList(config.Value, "namespaced");
            List<(Regex Regex, bool Allow)> plainFilters = ParseRegexAllowList(config.Value, "plain");

            foreach (var ns in pack.Namespaces) {
                foreach (var function in ns.Functions) {
                    foreach (var command in function.CommandsFlat) {
                        if (command.ContentType != Command.Type.Command) continue;
                        var (identifier, isNamespace) = FindIdentifier(command);
                        if (identifier == null) continue;

                        List<(Regex Regex, bool Allow)> filters = isNamespace ? namespacedFilters : plainFilters;
                        bool allow = true;
                        foreach (var filter in filters) {
                            if (filter.Regex.IsMatch(identifier)) {
                                allow = filter.Allow;
                                break;
                            }
                        }
                        if (!allow) {
                            output.Error(command, $"Identifier {identifier} is blacklisted.");
                        }
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config.TryValue(out JsonElement c) && c.IsObject() &&
                    c.TryAsArray("namespaced", out JsonElement namespaces) &&
                    namespaces.EnumerateArray().All(v => v.IsString()) &&
                    c.TryAsArray("plain", out JsonElement prefixes) &&
                    prefixes.EnumerateArray().All(v => v.IsString());
        }

        /// <summary>
        /// Returns the identifier and whether or not that identifier can be a full namespace.
        /// If this rule does not apply to the command, the returned Identifier is null.
        /// </summary>
        private (string Identifier, bool IsNamespace) FindIdentifier(Command command) {
            if (command.Raw.StartsWith("scoreboard objectives") && (
                command.Arguments[1] == "add" ||
                command.Arguments[1] == "modify" ||
                command.Arguments[1] == "remove")) return (command.Arguments[2], false);
            else if (command.CommandKey == "tag" && (
                command.Arguments[1] == "add" ||
                command.Arguments[1] == "remove")) return (command.Arguments[2], false);
            else if (command.CommandKey == "bossbar" && (
                command.Arguments[0] == "add" ||
                command.Arguments[0] == "remove" ||
                command.Arguments[0] == "set")) return (command.Arguments[1], true);
            else if (command.CommandKey == "team" && (
                command.Arguments[0] == "add" ||
                command.Arguments[0] == "empty" ||
                command.Arguments[0] == "join" ||
                command.Arguments[0] == "modify" ||
                command.Arguments[0] == "remove")) return (command.Arguments[1], false);
            else if (command.CommandKey == "data" && command.Arguments[1] == "storage" && (
                command.Arguments[0] == "merge" ||
                command.Arguments[0] == "modify" ||
                command.Arguments[0] == "remove")) return (command.Arguments[2], true);
            else return (null, false);
        }

        private List<(Regex Regex, bool Allow)> ParseRegexAllowList(JsonElement config, string key) {
            return config.GetProperty(key).EnumerateArray().Select(f => (
                new Regex($"^{f.GetString().Substring(1)}$"),
                f.GetString().StartsWith('+'))).ToList();
        }
    }
}
