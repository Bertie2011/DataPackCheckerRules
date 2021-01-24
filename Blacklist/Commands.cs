﻿using DataPackChecker.Shared;
using DataPackChecker.Shared.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core.Blacklist {
    /// <summary>
    /// Written by Bertie2011
    /// </summary>
    class Commands : CheckerRule {
        public override string Title => "Certain commands are not allowed in certain functions.";

        public override string Description => @"Some commands are not allowed in some functions. Each tag and function will be tested with a filter.

A filter consists of a list of regular expressions that are matched against the function/tag identifier in order. An identifier follows the pattern '<namespace>:<path>/<name>', where tags are prefixed with #. Expressions must also be prefixed by + (next filter) or - (check commands). If none of the identifier regexes match, the next filter is considered.

When a function/tag has a negative (prefixed with -) match in a filter, all commands found in that function/tag and their references to other functions/tags are matched against another list of regular expressions. This happens in similar fashion, meaning that each expression is matched in order and must be prefixed by + (next filter) or - (disallow). If none of the command regexes match, the next filter is considered.

If none of the filters give a double negative match (for location and command), the command is allowed.";

        public override List<string> GoodExamples => new List<string>() {@"{
    ""filters"": [
        {
            ""identifiers"": [
                ""-.*""
            ],
            ""commands"": [
                ""-(ban|ban-ip|pardon|kick|op|deop|forceload|stop).*""
            ]
        },
        {
            ""identifiers"": [
                ""-#minecraft:load""
            ],
            ""commands"": [
                ""-(say|me|tellraw|msg|w|teammsg|tell|title).*""
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
            throw new NotImplementedException();
        }
    }
}