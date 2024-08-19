
# RoR2 Import Extensions

RoR2 Import Extensions is a *thunderkit extension* aiming to reduce the time it takes to properly set up a RoR2 Thunderkit Project.

It is extremely recommended to install this and use it in the process of setting up a project, RoR2ImportExtensions can also be used as a use case example on how to extend the Thunderkit Importing Process.

### Extensions:
Note: Higher priority means it runs earlier

| Extension Name | Priority | Effect | Level of Recomendation |
|--|--|--|--|
| PostProcessing Package Installer | 3.25M | Installs the PostProcessing package version 2.3.0 and prevents the game's PostProcessing DLL from being imported | Recommended if working with PP
|Unity GUI Patcher|3.24M| Patcher that'll modify the game's UnityGUI assembly and installs & modifies the UnityGUI Package to allow proper functionality of the UnityGUI package in the editor.|Highly Recommended|
|TextMeshPro Patcher|3.23M| Patcher that'll modify the game's TextMeshPro assembly and installs & modifies the TextMeshPro Package to allow proper functionality of the TextMeshPro package in the editor.|Highly Recommended|
|Assembly Publicizer|3.125M|Publicizes the listed assemblies with N-Strip, publicized assemblies retain their editor functionality and inspector look| Recommended if publicizing is needed|
|MMHook Generator|3.12M|Creates MMHook assemblies for the listed assemblies, allowing for hooking ingame methods to run code injection|Extremely Recommended
| ROR2 LegacyResourceAPI Patcher | 2.75M | Patches the LegacyResourcesAPI dll to improve it's stability in the editor | Extremely Recommended |
|Set Deferred Shading|1.9995M|Ensures that the Graphics Tiers have their Rendering Path set to Deferred after importing Project settings|Highly Recommended
|Configure Addressable Graphics Settings|-5.01K|Assigns the Risk of Rain 2 DeferredShading and DeferredReflectionCustom shaders in the Addressable Graphics settings and by proxy in the Project's Graphics Settings|Recommended
|Ensure RoR2 Thunderstore Source|-125k|Ensures the creation of a thunderstore source that points to https://thunderstore.io|Recommended
|Install BepInExPack|-135K|Installs the latest version of BepInExPack|Extremely Recommended
|R2API Submodule Installer|-145K|Allows you to pick and choose what submodules of R2API to install to your project.|Optional but Recommended|
|Install RoR2MultiplayerHLAPI|-155K|Installs the latest version of the RoR2MultiplayerHLAPI and prevents the game's com.unity.mulitplayerhlapi.runtime.dll from being imported| Recommended
|Install RoR2EditorKit|-160K|Installs the latest version of RoR2EditorKit| Optional, but recommended|

## Planned Features

* Make the RoR2MultiplayerHLAPI and RoR2EditorKit extension points install the latest release from github instead of utilizing the Thunderstore versions.

* Got any ideas? suggest them in the modding discord's "Editor Extensions" channel!