# Data Pack Checker Rules
This repository contains the source code and [releases](https://github.com/Bertie2011/DataPackCheckerRules/releases) of rules made by and/or supported by the creator of [Data Pack Checker](https://github.com/Bertie2011/DataPackChecker).

View [the documentation](https://github.com/Bertie2011/DataPackChecker/blob/main/README.md) on how to start using the rules.

## Uniqueness.dll
<details><summary><b>Uniqueness.ResourceLocation</b><blockquote>All data pack files must be in a subfolder with the same name.</blockquote></summary>
Assuming the namespace is author specific, putting all resources in subfolders will prevent clashes with other data packs of the same author. By default each namespace can have its own subfolder, which can be extended or overridden by a list of names in the configuration.
</details>
