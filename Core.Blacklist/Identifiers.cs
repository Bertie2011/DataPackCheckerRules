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
    public class Identifiers : CheckerRule {
        private class IdentifierLoc {
            public Command Command { get; }
            public string Identifier { get; }
            public bool IsNamespace { get; }
            public IdentifierLoc(Command command, string identifier, bool isNamespace) {
                Command = command;
                Identifier = identifier;
                IsNamespace = isNamespace;
            }
            public override bool Equals(object obj) {
                return obj is IdentifierLoc loc &&
                       EqualityComparer<Command>.Default.Equals(Command, loc.Command) &&
                       Identifier == loc.Identifier;
            }

            public override int GetHashCode() {
                return HashCode.Combine(Command, Identifier);
            }
        }
        private class IdentifierInfo {
            public HashSet<string> ReferencedBy { get; } = new HashSet<string>();
        }

        private class Filter {
            public List<(Regex Regex, bool Allow)> ResourceChecks { get; set; }
            public List<(Regex Regex, bool Allow)> NamespacedChecks { get; set; }
            public List<(Regex Regex, bool Allow)> PlainChecks { get; set; }
        }

        public override string Title => "Certain identifiers are not allowed in certain functions.";

        public override string Description => @"Some identifiers of in-game resources are not allowed in some functions. Each identifier (e.g. an objective name) will be tested with a filter.

A filter consists of multiple lists, one for resource location ([#]<namespace>:[path/]<name>), one for namespaced identifiers and one for plain identifiers. Each list contains regular expressions and the first match determines the verdict based on a + (allow) or - (disallow) prefix.

If the first match in the 'resources' list (matching any referencing function/tag) is prefixed with - AND the first match in the 'namespace'/'plain' list is prefixed with -, the identifier is disallowed.
Each identifier will produce an error for each of the filters with a double negative match.

Plain:
- scoreboard objectives
- tags
- teams

Namespaced:
- bossbars
- data storage";


        public override List<string> GoodExamples { get; } = new List<string>() { @"{
    ""filters"": [
        {
            ""resources"": [
                ""-.*""
            ],
            ""namespaced"": [],
            ""plain"": [
                ""+abc.*"",
                ""-ab.*""
            ]
        }
    ]
}
scoreboard objectives add abc_objective1 dummy
scoreboard objectives add xyz_objective2 dummy" };

        public override List<string> BadExamples { get; } = new List<string>() { @"<See configuration above>
scoreboard objectives add abx_objective3 dummy" };

        public override List<string> ConfigExamples { get; } = new List<string>() { @"{
    ""filters"": [
        {
            ""resources"": [
                ""+#<resource regex1>"",
                ""-#<resource regex2>"",
                ""+<resource regex3>"",
                ""-<resource regex4>"",
                ...
            ],
            ""namespaced"": [
                ""+<identifier regex1>"",
                ""-<identifier regex2>"",
                ...
            ],
            ""plain"": [
                ""+<identifier regex1>"",
                ""-<identifier regex2>"",
                ...
            ]
        },
        ...
    ]
}" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.InvalidConfiguration<ResourceLocation>();
                return;
            }
            Dictionary<IdentifierLoc, IdentifierInfo> identifiers = BuildReferencesStore(pack);
            List<Filter> filters = BuildFilters(config);
            foreach (var identifier in identifiers) {
                for (int i = 0; i < filters.Count; i++) {
                    List<(Regex Regex, bool Allow)> identifierFilters = identifier.Key.IsNamespace ? filters[i].NamespacedChecks : filters[i].PlainChecks;
                    if (TryGetNegativeResourceMatch(filters[i].ResourceChecks, identifier.Value.ReferencedBy, out string reference)
                        && HasNegativeIdentifierMatch(identifierFilters, identifier.Key.Identifier)) {
                        var type = reference.StartsWith('#') ? "tag" : "function";
                        output.Error(identifier.Key.Command, $"The identifier {identifier.Key.Identifier} is blacklisted by filter {i + 1} when referenced by {type} '{reference}'");
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config.TryValue(out JsonElement c) && c.IsObject()
                && c.TryAsArray("filters", out JsonElement filters)
                && filters.EnumerateArray().All(f => f.IsObject()
                    && f.TryAsArray("resources", out JsonElement ids)
                    && ids.EnumerateArray().All(id => id.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+')))
                    && f.TryAsArray("namespaced", out JsonElement namespaced)
                    && namespaced.EnumerateArray().All(i => i.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+')))
                    && f.TryAsArray("plain", out JsonElement plain)
                    && plain.EnumerateArray().All(i => i.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+'))));
        }

        private List<Filter> BuildFilters(JsonElement? config) {
            List<Filter> result = new List<Filter>();

            foreach (var filterJson in config.Value.GetProperty("filters").EnumerateArray()) {
                var filter = new Filter();
                result.Add(filter);
                filter.ResourceChecks = ParseRegexAllowList(filterJson.GetProperty("resources"));
                filter.NamespacedChecks = ParseRegexAllowList(filterJson.GetProperty("namespaced"));
                filter.PlainChecks = ParseRegexAllowList(filterJson.GetProperty("plain"));
            }

            return result;
        }

        private Dictionary<IdentifierLoc, IdentifierInfo> BuildReferencesStore(DataPack pack) {
            Dictionary<IdentifierLoc, IdentifierInfo> identifiers = new Dictionary<IdentifierLoc, IdentifierInfo>();

            foreach (var ns in pack.Namespaces) {
                foreach (var f in ns.Functions) {
                    foreach (var ownerF in f.ReferencesFlat) {
                        foreach (var c in ownerF.CommandsFlat) {
                            if (c.ContentType != Command.Type.Command) continue;
                            GetOrCreate(identifiers, c)?.ReferencedBy.Add(f.NamespacedIdentifier);
                        }
                    }
                }
                foreach (var ft in ns.TagData.FunctionTags) {
                    foreach (var ownerF in ft.References.SelectMany(refF => refF.ReferencesFlat)) {
                        foreach (var c in ownerF.CommandsFlat) {
                            if (c.ContentType != Command.Type.Command) continue;
                            GetOrCreate(identifiers, c)?.ReferencedBy.Add(ft.NamespacedIdentifier);
                        }
                    }
                }
            }

            return identifiers;
        }
        private IdentifierInfo GetOrCreate(Dictionary<IdentifierLoc, IdentifierInfo> references, Command command) {
            var (identifier, isNamespace) = FindIdentifier(command);
            if (identifier == null) return null;

            IdentifierLoc key = new IdentifierLoc(command, identifier, isNamespace);
            if (!references.TryGetValue(key, out IdentifierInfo result)) {
                result = new IdentifierInfo();
                references.Add(key, result);
            }
            return result;
        }

        private bool TryGetNegativeResourceMatch(List<(Regex Regex, bool Allow)> resourceChecks, HashSet<string> references, out string identifier) {
            foreach (var check in resourceChecks) {
                foreach (var reference in references) {
                    if (check.Regex.IsMatch(reference)) {
                        identifier = reference;
                        return !check.Allow;
                    }
                }
            }
            identifier = default;
            return false;
        }

        private bool HasNegativeIdentifierMatch(List<(Regex Regex, bool Allow)> checks, string identifier) {
            foreach (var check in checks) {
                if (check.Regex.IsMatch(identifier)) {
                    return !check.Allow;
                }
            }
            return false;
        }

        private List<(Regex Regex, bool Allow)> ParseRegexAllowList(JsonElement array) {
            return array.EnumerateArray().Select(f => (
                new Regex($"^{f.GetString().Substring(1)}$"),
                f.GetString().StartsWith('+'))).ToList();
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
    }
}
