
# RoR2 Import Extensions

RoR2 Import Extensions is a *thunderkit extension* aiming to reduce the time it takes to properly set up a RoR2 Thunderkit Project.

It is extremely recommended to install this and use it in the process of setting up a project, RoR2ImportExtensions can also be used as a use case example on how to extend the Thunderkit Importing Process.
### Extensions:
Note: Higher priority means it runs earlier

| Extension Name | Priority | Effect | Level of Recomendation |
|--|--|--|--|
| PostProcessing Package Installer | 3.25M | Installs the PostProcessing package version 2.3.0 and prevents the game's PostProcessing DLL from being imported | Recommended if working with PP
|TextMeshPro Uninstaller|3.24M|Removes Unity TextMeshPro due to compatibility issues with the games modified TextMeshPro library and ensures that Unity.TextMeshPro.dll is copied from the games directory|Highly Recommended|
|Unity GUI Uninstaller|3.23M|Removes Unity GUI due to compatibility issues with the game's modified TextMeshPro library and ensures that the Unity.UI.dll is copied from the games directory|Highly Recommended|
|Assembly Publicizer|3.125M|Publicizes the listed assemblies with N-Strip, publicized assemblies retain their editor functionality and inspector look| Recommended if publicizing is needed|
|MMHook Generator|3.12M|Creates MMHook assemblies for the listed assemblies, allowing for hooking ingame methods to run code injection|Extremel Recommended
|RoR2 LegacyResourceAPI Patcher|2.75M|Patches the game's LegacyResourcesAPI to improve editor functionality and greatly reduce hangs/freezes|Extremely Recommended|
|Set Deferred Shading|1.9995M|Ensures that the Graphics Tiers have their Rendering Path set to Deferred after importing Project settings|Highly Recommended
|Configure Addressable Graphics Settings|-5.01K|Assigns the Risk of Rain 2 DeferredShading and DeferredReflectionCustom shaders in the Addressable Graphics settings and by proxy in the Project's Graphics Settings|Recommended
|Ensure RoR2 Thunderstore Source|-125k|Ensures the creation of a thunderstore source that points to https://thunderstore.io|Recommended
|Install BepInExPack|-135K|Installs the latest version of BepInExPack|Extremely Recommended
|R2API Submodule Installer|-145K|Allows you to pick and choose what submodules of R2API to install to your project.|Optional but Recommended|
|Install RoR2MultiplayerHLAPI|-155K|Installs the latest version of the RoR2MultiplayerHLAPI and prevents the game's com.unity.mulitplayerhlapi.runtime.dll from being imported|Extremely Recommended
|Install RoR2EditorKit|-160K|Installs the latest version of RoR2EditorKit| Optional, but recommended|

## Planned Features
* Got any ideas? suggest them in the modding discord's "Editor Extensions" channel!

## Changelog

### 1.3.5

- Updated to use ThunderKit 7.0.0
- Now installs the R2API Submodules in one big batch instead of one by one

### 1.3.4

- Fixed an issue where executing the R2APISubmoduleInstaller would result in an invalid project state
- R2APISubmoduleInstaller now uninstalls old versions of Submodules that where added as dependencies if a newer version exists.

### 1.3.3

- Fixed an issue where NotSupportedException would throw when the MMHook Generator Configuration was trying to cache assemblies
- R2APISubmoduleInstaller doesnt destroy the Thunderstore Source if it has been ensured
- Fixed issue where R2APISubmoduleInstaller wouldnt properly install submodules and cause a loop spamming a warning message about no packages found.

### 1.3.1

- Added a method to serialize R2API submodule installation as hard dependencies.
	- These hard dependencies are managed by a serialized JSON file.
	- The hard dependencies will persist even if the import configuration asset is removed/deleted

### 1.3.0

- Updated Install R2API to the R2API Submodule Installer, updating the importer to properly support the Split Assemblies update.
- Added a safety catch to the MMHook generator so it doesnt accidentally create duplicate MMHook assemblies.

### 1.2.0

- FixPluginTypesSerialization is now in the BepInExPack by default, removed the option for installing the FixPluginTypesSerialization standalone
- Added the MMHook Generator, used for automatically generating MonoMod hook assemblies.

### 1.1.1

- Fix `README.md`

### 1.1.0

- Add SetDeferredShading to fix Graphics settings not collected by Import Project settings

### 1.0.2

- Add importer to fix graphics settings, enabling consistent rendering of shaders in the editor

### 1.0.1

- Fix legacy api patcher path by @PassivePicasso in #3

### 1.0.0

- Initial Release