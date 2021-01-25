using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using DataPackChecker.Shared.Data.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;
using DataPackChecker.Shared.Util;
using System.Text.RegularExpressions;

namespace Core.Blacklist {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    class Commands : CheckerRule {
        private class CommandInfo {
            public Function Owner { get; set; }
            public HashSet<string> ReferencedBy { get; } = new HashSet<string>();
        }

        private class Filter {
            public List<(Regex Regex, bool Allow)> IdentifierChecks { get; } = new List<(Regex Regex, bool Allow)>();
            public List<(Regex Regex, bool Allow)> CommandChecks { get; } = new List<(Regex Regex, bool Allow)>();
        }

        public override string Title => "Certain commands are not allowed in certain functions.";

        public override string Description => @"Some commands are not allowed in some functions. Each command will be tested with a filter.

A filter consists of a list of regular expressions that are matched against the functions/tags that reference the command in order. An identifier follows the pattern '<namespace>:<path>/<name>', where tags are prefixed with #. Expressions must also be prefixed by + (allow) or - (check commands). If none of the referencing functions/tags match any of the identifier regexes, the next filter is considered.

When a command has a referencing function/tag with a negative (prefixed with -) match, the command is matched against another list of regular expressions. This happens in similar fashion, meaning that each expression is matched in order and must be prefixed by + (allow) or - (disallow). If none of the command regexes match, the next filter is considered.

If none of the filters give a double negative match (for location of referencing tags/functions and the command itself), the command is allowed.";

        public override List<string> GoodExamples => new List<string>() {@"{
    ""filters"": [
        {
            ""identifiers"": [
                ""-.*""
            ],
            ""commands"": [
                ""-(ban|ban-ip|pardon|kick|op|deop|forceload|stop) .*""
            ]
        },
        {
            ""identifiers"": [
                ""-#minecraft:load""
            ],
            ""commands"": [
                ""-(say|me|tellraw|msg|w|teammsg|tell|title) .*""
            ]
        }
    ]
}
#minecraft:load - {""values"":[""my_namespace:load""]}
my_namespace:load - scoreboard objectives add obj dummy
my_namespace:my_function - scoreboard players add @s obj 1" };

        public override List<string> BadExamples => new List<string>() { @"<See configuration above>
my_namespace:load - tellraw @a {""text"":""THIS AWESOME DATA PACK IS MADE BY MEEEE!!!1"",""bold"":true}
my_namespace:my_function - execute as @a at @s if block ~ ~-1 ~ air run ban @s[type=player] Flying" };

        public override List<string> ConfigExamples => new List<string>() { @"{
    ""filters"": [
        {
            ""identifiers"": [
                ""+#<identifier regex1>"",
                ""-#<identifier regex2>"",
                ""+<identifier regex3>"",
                ""-<identifier regex4>"",
                ...
            ],
            ""commands"": [
                ""+<command regex1>"",
                ""-<command regex2>"",
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
            Dictionary<Command, HashSet<string>> commands = BuildReferencesStore(pack);
            List<Filter> filters = BuildFilters(config);
            foreach (var command in commands) {
                for (int i = 0; i < filters.Count; i++) {
                    if (TryGetNegativeIdentifierMatch(filters[i], command.Value, out string reference)
                        && HasNegativeCommandMatch(filters[i], command.Key)) {
                        var type = reference.StartsWith('#') ? "tag" : "function";
                        output.Error(command.Key, $"This command is blacklisted by filter {i + 1} when referenced by {type} '{reference}'");
                    }
                }
            }
        }

        private bool TryGetNegativeIdentifierMatch(Filter filter, HashSet<string> references, out string identifier) {
            identifier = default;
            foreach (var reference in references) {
                foreach (var check in filter.IdentifierChecks) {
                    if (check.Regex.IsMatch(reference)) {
                        if (!check.Allow) {
                            identifier = reference;
                            return true;
                        }
                        break;
                    }
                }
            }
            return false;
        }

        private bool HasNegativeCommandMatch(Filter filter, Command command) {
            foreach (var check in filter.CommandChecks) {
                if (check.Regex.IsMatch(command.Raw)) {
                    if (!check.Allow) return true;
                    break;
                }
            }
            return false;
        }

        private List<Filter> BuildFilters(JsonElement? config) {
            List<Filter> result = new List<Filter>();

            foreach (var filterJson in config.Value.GetProperty("filters").EnumerateArray()) {
                var filter = new Filter();
                result.Add(filter);
                foreach (var id in filterJson.GetProperty("identifiers").EnumerateArray()) {
                    filter.IdentifierChecks.Add((
                        new Regex($"^{id.GetString().Substring(1)}$"),
                        id.GetString().StartsWith('+')));
                }
                foreach (var command in filterJson.GetProperty("commands").EnumerateArray()) {
                    filter.CommandChecks.Add((
                        new Regex($"^{command.GetString().Substring(1)}$"),
                        command.GetString().StartsWith('+')));
                }
            }

            return result;
        }

        private Dictionary<Command, HashSet<string>> BuildReferencesStore(DataPack pack) {
            Dictionary<Command, HashSet<string>> commands = new Dictionary<Command, HashSet<string>>();

            foreach (var ns in pack.Namespaces) {
                foreach (var f in ns.Functions) {
                    foreach (var ownerF in f.ReferencesFlat) {
                        foreach (var c in ownerF.CommandsFlat) {
                            if (c.ContentType != Command.Type.Command) continue;
                            if (!commands.TryGetValue(c, out HashSet<string> info)) {
                                info = new HashSet<string>();
                                commands.Add(c, info);
                            }
                            info.Add(f.NamespacedIdentifier);
                        }
                    }
                }
                foreach (var ft in ns.TagData.FunctionTags) {
                    foreach (var ownerF in ft.References.SelectMany(refF => refF.ReferencesFlat)) {
                        foreach (var c in ownerF.CommandsFlat) {
                            if (c.ContentType != Command.Type.Command) continue;
                            commands[c].Add(ft.NamespacedIdentifier);
                        }
                    }
                }
            }

            return commands;
        }

        private bool ValidateConfig(JsonElement? config) {
            return config.TryValue(out JsonElement c) && c.IsObject()
                && c.TryAsArray("filters", out JsonElement filters)
                && filters.EnumerateArray().All(f => f.IsObject()
                    && f.TryAsArray("identifiers", out JsonElement ids)
                    && ids.EnumerateArray().All(id => id.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+')))
                    && f.TryAsArray("commands", out JsonElement commands)
                    && commands.EnumerateArray().All(c => c.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+'))));
        }
    }
}
