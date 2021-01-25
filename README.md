# Data Pack Checker Rules
This repository contains the source code and [releases](https://github.com/Bertie2011/DataPackCheckerRules/releases) of rules made by and/or supported by the creator of [Data Pack Checker](https://github.com/Bertie2011/DataPackChecker). There is a recommended configuration that all data pack creators should use [here](https://github.com/Bertie2011/DataPackCheckerRules/blob/main/RecommendedConfig.json).

View [the documentation](https://github.com/Bertie2011/DataPackChecker/blob/main/README.md) on how to start using the rules.

## Blacklist.dll
<details><summary><b>Core.Blacklist.Commands</b><blockquote>Certain commands are not allowed in certain functions.</blockquote></summary>
Some commands are not allowed in some functions. Each command will be tested with a filter.<br><br>
A filter consists of a list of regular expressions that are matched against the functions/tags that reference the command in order. An identifier follows the pattern '{namespace}:{path}/{name}', where tags are prefixed with #. Expressions must also be prefixed by + (allow) or - (check commands). If none of the referencing functions/tags match any of the identifier regexes, the next filter is considered.<br><br>
When a command has a referencing function/tag with a negative (prefixed with -) match, the command is matched against another list of regular expressions. This happens in similar fashion, meaning that each expression is matched in order and must be prefixed by + (allow) or - (disallow). If none of the command regexes match, the next filter is considered.<br><br>
If none of the filters give a double negative match (for location of referencing tags/functions and the command itself), the command is allowed.
</details>
<details><summary><b>Core.Blacklist.ResourceLocation</b><blockquote>Certain resource locations are blacklisted.</blockquote></summary>
Some resource locations are blacklisted. Each resource file path (starting with 'data/') is matched against a list of regular expressions until one matches. Based on a +/- prefix, the file will be allowed or disallowed. If none of the expressions match, the location is allowed. Use [^/]+ to allow any path element.
</details>

## Compatibility.dll
<details><summary><b>Core.Compatibility.NoTagReplace</b><blockquote>Tags must not overwrite entries defined in lower priority data packs.</blockquote></summary>
Setting 'replace' to true in a tag can prevent other data packs from working correctly.
</details>
<details><summary><b>Core.Compatibility.VersionId</b><blockquote>The version identifier must be correct.</blockquote></summary>
The version identifier 'pack_format' in pack.mcmeta has to match the number set in the configuration.
</details>

## Style.dll
<details><summary><b>Core.Style.AsAtComments</b><blockquote>A function must start with comments describing the 'as/at' context.</blockquote></summary>
In each function a certain context is assumed, which includes the meaning of @s, @p and ~ ~ ~. Writing down that context at the top of the function might save some time debugging and help out if the function is revisited in the future. Spacing within the lines is not checked.
</details>
<details><summary><b>Core.Style.UninstallFunction</b><blockquote>There must be an uninstall function.</blockquote></summary>
Providing an uninstall function will help remove traces and leave a clean world behind before the data pack is removed.
</details>

## Uniqueness.dll
<details><summary><b>Core.Uniqueness.Identifier</b><blockquote>All in-game resources must have a prefixed identifier.</blockquote></summary>
In-game resources like scoreboard objectives have to be prefixed to prevent clashes. Only 1 prefix per namespace is allowed and should be separated by any of these characters: . _ -<br>
If a namespace is allowed, all resources must be prefixed by the namespace of the function.<br><br>

Prefixed:
- scoreboard objectives
- tags
- teams

Namespaced:
- bossbars
- data storage

Configuration can be used to extend or overwrite the default rules with a custom set of prefixes and namespaces.
</details>
<details><summary><b>Core.Uniqueness.ResourceLocation</b><blockquote>All data pack files must be in a subfolder with the same name.</blockquote></summary>
Assuming the namespace is author specific, putting all resources in subfolders will prevent clashes with other data packs of the same author. By default each namespace can have its own subfolder, which can be extended or overridden by a list of names in the configuration.
</details>
