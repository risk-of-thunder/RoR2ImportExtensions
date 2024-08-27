/*using System;
using System.IO;
using System.Linq;
using ThunderKit.Core.Config;
using ThunderKit.Core.Config.Common;
using ThunderKit.Core.Data;
using ThunderKit.Core.Utilities;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace RiskOfThunder.RoR2Importer
{
    public class TextMeshProPatcher : OptionalExecutor
    {
        private const string PACKAGE_IDENTIFIER = "com.unity.textmeshpro";
        private const string EDITOR_SCRIPTS_ASSEMBLYDEF_GUID = "6546d7765b4165b40850b3667f981c26";
        private const string RUNTIME_SCRIPTS_FOLDER_GUID = "5fc988a1d5b04aee9a5222502b201a45";
        private const string TEST_SCRIPTS_FOLDER_GUID = "2ce4bbcc4722440890a03312706037fe";
        private const string BSDIFF_PATCH_GUID = "2b2b1ea627d301f43acdc84dcfd1725a";
        private const string MODIFIED_ESSENTIALS_PACKAGE_GUID = "3bac71d49fd5faa4e8fcfd5f0e217a3b";
        private const string REGULAR_ESSENTIALS_PACKAGE_GUID = "ce4ff17ca867d2b48b5c8a4181611901";
        private const string ASSEMBLY_FILE_NAME = "Unity.TextMeshPro.dll";
        public override int Priority => Constants.Priority.TextMeshProPatcher;
        public override string Description => $"Optional Executor that'll proceed to ensure proper TMPro functionality within the project by doing the following:\n" +
            $"* Ensure installation of TMPro\n" +
            $"* Embedding the package into the project\n" +
            $"* Deleting the Runtime and Test scripts to allow the game's Unity.TextMeshPro.dll to be imported into the project\n" +
            $"* Patching the game's TextMeshPro dll to ensure it works with the editor scripts.\n" +
            $"* Adding the precompiled dll to the editor assembly definition\n" +
            $"* Replacing the TMPro Essentials package with a modified one.\n\n" +
            $"This in turn will allow TMPro to exist in the project and proper utilization of it using the game's HGTextMeshProUI component.";

        public override bool Execute()
        {

            try
            {
                if (EnsureTMProInstallation())
                    return false;


                if (EmbedTMProPackage())
                    return false;

                DeleteRuntimeAndTestScripts();
                PatchAssembly();
                ModifyEditorAssemblyDef();
                ReplaceTMProEssentialsPackage();
                Client.Remove(PACKAGE_IDENTIFIER);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
            }
            Debug.Log("TMProPatcher: Finished Execution.");
            return true;
        }

        private bool EnsureTMProInstallation()
        {
            var packagesList = Client.List();
            var escape = false;
            while (!packagesList.IsCompleted && !escape)
            {
                var x = escape;
            }

            //TMPro is not installed, install it.
            if (packagesList.Result.FirstOrDefault(q => q.name == PACKAGE_IDENTIFIER) == null)
            {
                var result = Client.Add(PACKAGE_IDENTIFIER);
                escape = false;
                while (!result.IsCompleted && !escape)
                {
                    var x = escape;
                }
                Debug.Log($"TMProPatcher: TextMeshPro has been ensured in the project.");

                PackageHelper.ResolvePackages();
                return true;
            }
            return false;
        }

        private bool EmbedTMProPackage()
        {
            var packagesList = Client.List();
            var escape = false;
            while (!packagesList.IsCompleted && !escape)
            {
                var x = escape;
            }

            var package = packagesList.Result.FirstOrDefault(q => q.name == PACKAGE_IDENTIFIER);

            if (package.source == UnityEditor.PackageManager.PackageSource.Embedded)
                return false;

            var result = Client.Embed(PACKAGE_IDENTIFIER);
            escape = false;
            while (!result.IsCompleted && !escape)
            {
                var x = escape;
            }
            Debug.Log($"TMProPatcher: TextMeshPro has been embedded into the project.");
            return true;
        }

        private void DeleteRuntimeAndTestScripts()
        {
            var runtimeScriptsFolderPath = AssetDatabase.GUIDToAssetPath(RUNTIME_SCRIPTS_FOLDER_GUID);
            FileUtil.DeleteFileOrDirectory(runtimeScriptsFolderPath);

            var runtimeScriptsTestPath = AssetDatabase.GUIDToAssetPath(TEST_SCRIPTS_FOLDER_GUID);
            FileUtil.DeleteFileOrDirectory(runtimeScriptsTestPath);

            Debug.Log($"TMProPatcher: TextMeshPro Runtime and Test Scripts have been deleted.");
        }

        private void PatchAssembly()
        {
            var settings = ThunderKitSetting.GetOrCreateSettings<ThunderKitSettings>();
            var originalAssembly = Path.GetFullPath(Path.Combine(settings.ManagedAssembliesPath, ASSEMBLY_FILE_NAME));
            var destinationAssembly = Path.Combine(settings.PackageFilePath, ASSEMBLY_FILE_NAME);
            var diffPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(BSDIFF_PATCH_GUID));
            var packagePath = Path.GetDirectoryName(destinationAssembly);

            if(File.Exists(destinationAssembly))
            {
                File.Delete(destinationAssembly);
            }

            Directory.CreateDirectory(packagePath);

            BsDiff.BsTool.Patch(originalAssembly, destinationAssembly, diffPath);

            var asmPath = destinationAssembly.Replace("\\", "/");
            var destinationMetadata = Path.Combine(settings.PackageFilePath, $"{ASSEMBLY_FILE_NAME}.meta");
            PackageHelper.WriteAssemblyMetaData(asmPath, destinationMetadata);

            var escape = false;
            while(EditorApplication.isUpdating && !escape)
            {
                var x = escape;
            }

            Debug.Log($"TMProPatcher: The Game's TextMeshPro.dll has been patched.");
        }

        private void ModifyEditorAssemblyDef()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(EDITOR_SCRIPTS_ASSEMBLYDEF_GUID);
            AssemblyDefinitionAsset assemblyDefinitionAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);

            AssemblyDef assemblyDef = JsonUtility.FromJson<AssemblyDef>(assemblyDefinitionAsset.text);

            assemblyDef.overrideReferences = true;
            assemblyDef.precompiledReferences = assemblyDef.precompiledReferences ?? Array.Empty<string>();
            ArrayUtility.AddRange(ref assemblyDef.precompiledReferences, new string[] { ASSEMBLY_FILE_NAME, "UnityEngine.UI.dll" });

            var modifiedJson = EditorJsonUtility.ToJson(assemblyDef, true);
            File.WriteAllText(Path.GetFullPath(assetPath), modifiedJson);

            Debug.Log($"TMProPatcher: Added the precompiled dlls to the Editor AssemblyDef");
        }

        private void ReplaceTMProEssentialsPackage()
        {
            var src = AssetDatabase.GUIDToAssetPath(MODIFIED_ESSENTIALS_PACKAGE_GUID);
            var dest = AssetDatabase.GUIDToAssetPath(REGULAR_ESSENTIALS_PACKAGE_GUID);

            FileUtil.ReplaceFile(src, dest);
            Debug.Log("TMProPatcher: Replaced the TMProEssentials UnityPackage.");
        }
    }
}*/