﻿using DataPackChecker.Shared;
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
        private class Filter {
            public List<(Regex Regex, bool Allow)> ResourceChecks { get; } = new List<(Regex Regex, bool Allow)>();
            public List<(Regex Regex, bool Allow)> CommandChecks { get; } = new List<(Regex Regex, bool Allow)>();
        }

        public override string Title => "Certain commands are not allowed in certain functions.";

        public override string Description => @"Some commands are not allowed in some functions. Each command will be tested with a filter.

A filter consists of multiple lists, one for resource location ([#]<namespace>:[path/]<name>) and one for commands. Each list contains regular expressions and the first match determines the verdict based on a + (allow) or - (disallow) prefix.

If the first match in the 'resources' list (matching any referencing function/tag) is prefixed with - AND the first match in the 'commands' list is prefixed with -, the command is disallowed.
Each command will produce an error for each of the filters with a double negative match.";

        public override List<string> GoodExamples => new List<string>() { @"{
    ""filters"": [
        {
            ""resources"": [
                ""-.*""
            ],
            ""commands"": [
                ""-(ban|ban-ip|pardon|kick|op|deop|forceload|stop) .*""
            ]
        },
        {
            ""resources"": [
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
            ""resources"": [
                ""+#<resource regex1>"",
                ""-#<resource regex2>"",
                ""+<resource regex3>"",
                ""-<resource regex4>"",
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
                    if (TryGetNegativeResourceMatch(filters[i], command.Value, out string reference)
                        && HasNegativeCommandMatch(filters[i], command.Key)) {
                        var type = reference.StartsWith('#') ? "tag" : "function";
                        output.Error(command.Key, $"This command is blacklisted by filter {i + 1} when referenced by {type} '{reference}'");
                    }
                }
            }
        }

        private bool TryGetNegativeResourceMatch(Filter filter, HashSet<string> references, out string identifier) {
            foreach (var check in filter.ResourceChecks) {
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

        private bool HasNegativeCommandMatch(Filter filter, Command command) {
            foreach (var check in filter.CommandChecks) {
                if (check.Regex.IsMatch(command.Raw)) {
                    return !check.Allow;
                }
            }
            return false;
        }

        private List<Filter> BuildFilters(JsonElement? config) {
            List<Filter> result = new List<Filter>();

            foreach (var filterJson in config.Value.GetProperty("filters").EnumerateArray()) {
                var filter = new Filter();
                result.Add(filter);
                foreach (var id in filterJson.GetProperty("resources").EnumerateArray()) {
                    filter.ResourceChecks.Add((
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
                            GetOrCreate(commands, c).Add(f.NamespacedIdentifier);
                        }
                    }
                }
                foreach (var ft in ns.TagData.FunctionTags) {
                    foreach (var ownerF in ft.References.SelectMany(refF => refF.ReferencesFlat)) {
                        foreach (var c in ownerF.CommandsFlat) {
                            if (c.ContentType != Command.Type.Command) continue;
                            GetOrCreate(commands, c).Add(ft.NamespacedIdentifier);
                        }
                    }
                }
            }

            return commands;
        }

        private HashSet<string> GetOrCreate(Dictionary<Command, HashSet<string>> references, Command key) {
            if (!references.TryGetValue(key, out HashSet<string> result)) {
                result = new HashSet<string>();
                references.Add(key, result);
            }
            return result;
        }

        private bool ValidateConfig(JsonElement? config) {
            return config.TryValue(out JsonElement c) && c.IsObject()
                && c.TryAsArray("filters", out JsonElement filters)
                && filters.EnumerateArray().All(f => f.IsObject()
                    && f.TryAsArray("resources", out JsonElement ids)
                    && ids.EnumerateArray().All(id => id.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+')))
                    && f.TryAsArray("commands", out JsonElement commands)
                    && commands.EnumerateArray().All(c => c.TryAsString(out string v)
                        && (v.StartsWith('-') || v.StartsWith('+'))));
        }
    }
}
