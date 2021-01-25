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

namespace Core.Uniqueness {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    public class Identifier : CheckerRule {
        private static readonly char[] PrefixSeparators = new char[] { '.', '-', '_' };
        public override string Title => "All in-game resources must have a prefixed identifier.";

        public override string Description => @"In-game resources like scoreboard objectives have to be prefixed to prevent clashes. Only 1 prefix per namespace is allowed and should be separated by any of these characters: . _ -
If a namespace is allowed, all resources must be prefixed by the namespace of the function.

Prefixed:
- scoreboard objectives
- tags
- teams

Namespaced:
- bossbars
- data storage";

        public override List<string> GoodExamples { get; } = new List<string>() { "data modify storage my_namespace:my_storage ...", "scoreboard objectives add myns_my_objective ...\ntag ... add myns_my_tag" };

        public override List<string> BadExamples { get; } = new List<string>() { "data modify storage my_storage ...", "scoreboard objectives add myObjective ...", "scoreboard objectives add abc_my_objective ...\ntag ... add xyz_my_tag" };

        public override List<string> ConfigExamples { get; } = new List<string>() { @"Only one prefix per namespace is allowed without configuration.
For namespaced identifiers, the namespace must match that of the function.
The allowed prefixes/namespaces can be extended or overriden by supplying a configuration like this:
{
    ""namespaces"": [
        ""other namespace"",
        ...
    ],
    ""prefixes"": [
        ""other prefix"",
        ...
    ],
    ""extend"": true
}" };

        public override void Run(DataPack pack, JsonElement? config, Output output) {
            if (!ValidateConfig(config)) {
                output.InvalidConfiguration<Identifier>();
                return;
            }

            foreach (var ns in pack.Namespaces) {
                string foundPrefix = null;
                Function foundPrefixF = null;
                Command foundPrefixC = null;

                foreach (var function in ns.Functions) {
                    foreach (var command in function.CommandsFlat) {
                        var (prefix, isNamespace) = FindIdentifierPrefix(command);
                        if (prefix == null) continue;
                        else if (prefix == "") {
                            output.Error(command, "Identifier in command does not have a prefix.");
                            continue;
                        }

                        bool configMatch = config != null && (
                            isNamespace && config.Value.GetProperty("namespaces").EnumerateArray().Any(v => v.GetString() == prefix)
                            || !isNamespace && config.Value.GetProperty("prefixes").EnumerateArray().Any(v => v.GetString() == prefix));
                        bool append = config == null || config.Value.GetProperty("extend").GetBoolean();

                        if (!PrefixAllowed(prefix, isNamespace, foundPrefix, append, configMatch, ns)) {
                            if (isNamespace && append) output.Error(command, $"Identifier namespace does not match function namespace or any of the ones or config file.");
                            else if (isNamespace) output.Error(command, $"Identifier namespace does not match any of the ones in the config file.");
                            else if (foundPrefix != null) output.Error(command, $"Identifier prefix does not match {foundPrefix} found on {foundPrefixF.NamespacedIdentifier} line {foundPrefixC.Line} or any of the ones in the config file.");
                            else output.Error(command, $"Identifier prefix does not match any of the ones in the config file.");
                        } else if (!isNamespace && foundPrefix == null && append && !configMatch) {
                            foundPrefix = prefix;
                            foundPrefixF = function;
                            foundPrefixC = command;
                        }
                    }
                }
            }
        }

        private bool ValidateConfig(JsonElement? config) {
            return config == null || (config.TryValue(out JsonElement c) && c.IsObject() &&
                    c.TryAsArray("namespaces", out JsonElement namespaces) &&
                    namespaces.EnumerateArray().All(v => v.IsString()) &&
                    c.TryAsArray("prefixes", out JsonElement prefixes) &&
                    prefixes.EnumerateArray().All(v => v.IsString()) &&
                    c.IsBool("extend"));
        }

        /// <summary>
        /// Returns the prefix and whether or not that prefix can be a full namespace.
        /// If there is no prefix, the returned Prefix is an empty string.
        /// If this rule does not apply to the command, the returned Prefix is null.
        /// </summary>
        private (string Prefix, bool IsNamespace) FindIdentifierPrefix(Command command) {
            string full_identifier;
            bool isNamespace;
            if (command.ContentType != Command.Type.Command) return (null, false);
            else if (command.Raw.StartsWith("scoreboard objectives add ")) {
                full_identifier = command.Arguments[2];
                isNamespace = false;
            } else if (command.CommandKey == "tag" && command.Arguments[1] == "add") {
                full_identifier = command.Arguments[2];
                isNamespace = false;
            } else if (command.Raw.StartsWith("bossbar add ")) {
                full_identifier = command.Arguments[1];
                isNamespace = true;
            } else if (command.Raw.StartsWith("team add ")) {
                full_identifier = command.Arguments[1];
                isNamespace = false;
            } else if (command.Raw.StartsWith("data modify storage ")) {
                full_identifier = command.Arguments[2];
                isNamespace = true;
            } else return (null, false);

            var parts = full_identifier.Split(isNamespace ? new char[] { ':' } : PrefixSeparators, 2);
            return (parts.Length == 2 ? parts[0] : "", isNamespace);
        }

        private bool PrefixAllowed(string prefix, bool isNamespace, string foundPrefix, bool append, bool configMatch, Namespace ns) {
            if (configMatch) return true;
            else if (isNamespace) {
                return append && ns.Name == prefix;
            } else {
                return foundPrefix == null && append || foundPrefix != null && foundPrefix == prefix;
            }
        }
    }
}
