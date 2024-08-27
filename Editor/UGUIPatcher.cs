/*using ThunderKit.Core.Config;
using ThunderKit.Core.Config.Common;
using UnityEngine;
using UnityEditor;
using System;
using ThunderKit.Core.Utilities;
using UnityEditor.PackageManager;
using ThunderKit.Core.Data;
using System.IO;
using UnityEditorInternal;
using System.Linq;

namespace RiskOfThunder.RoR2Importer
{
    public class UGUIPatcher : OptionalExecutor
    {
        private const string PACKAGE_IDENTIFIER = "com.unity.ugui";
        private const string EDITOR_SCRIPTS_ASSEMBLYDEF_GUID = "343deaaf83e0cee4ca978e7df0b80d21";
        private const string RUNTIME_SCRIPTS_FOLDER_GUID = "aa14b70e6a58c5b4fa6663623e3dca91";
        private const string TEST_SCRIPTS_FOLDER_GUID = "d850db319a63d2546932ff76fe55a498";
        private const string BSDIFF_PATCH_GUID = "7985a0c9719cc774bab7ba2dc0a75e4a";
        private const string ASSEMBLY_FILE_NAME = "UnityEngine.UI.dll";
        public override int Priority => Constants.Priority.UGUIPatcher;
        public override string Name => "Unity GUI Patcher";
        public override string Description => $"Optional Executor that'll proceed to ensure proper Unity GUI functionality within the project by doing the following:\n" +
            $"* Ensure installation of UGUI\n" +
            $"* Embedding the package into the project\n" +
            $"* Deleting the Runtime and Test scripts to allow the game's UnityEngine.UI.dll to be imported into the project\n" +
            $"* Patching the game's UnityEngine.UI.dll to ensure it works with the editor scripts.\n" +
            $"* Adding the precompiled DLL to the Editor assembly definition\n\n" +
            $"This in turn will allow Unity GUI to exist in the project and proper utilization of its features.";

        public override bool Execute()
        {
            try
            {
                if(EnsureUGUIInstallation())
                    return false;
                
                if(EmbedUGUIPackage())
                    return false;

                DeleteRuntimeAndTestScripts();
                PatchAssembly();
                ModifyEditorAssemblyDef();
                Client.Remove(PACKAGE_IDENTIFIER);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
            }
            Debug.Log($"UGUIPatcher: Finished Execution.");
            return true;
        }

        private bool EnsureUGUIInstallation()
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
                Debug.Log($"UGUIPatcher: UnityGUI has been ensured in the project.");
                PackageHelper.ResolvePackages();
                return true;
            }
            return false;
        }

        private bool EmbedUGUIPackage()
        {
            var packagesList = Client.List();
            var escape = false;
            while(!packagesList.IsCompleted && !escape)
            {
                var x = escape;
            }

            var package = packagesList.Result.FirstOrDefault(q => q.name == PACKAGE_IDENTIFIER);

            if (package.source == UnityEditor.PackageManager.PackageSource.Embedded)
                return false;

            var result = Client.Embed(PACKAGE_IDENTIFIER);
            while (!result.IsCompleted && !escape)
            {
                var x = escape;
            }
            Debug.Log($"UGUIPatcher: UnityGUI has been embedded into the project.");

            PackageHelper.ResolvePackages();
            return true;
        }

        private void DeleteRuntimeAndTestScripts()
        {
            var runtimeScriptsFolderPath = AssetDatabase.GUIDToAssetPath(RUNTIME_SCRIPTS_FOLDER_GUID);
            FileUtil.DeleteFileOrDirectory(runtimeScriptsFolderPath);

            var runtimeScriptsTestPath = AssetDatabase.GUIDToAssetPath(TEST_SCRIPTS_FOLDER_GUID);
            FileUtil.DeleteFileOrDirectory(runtimeScriptsTestPath);

            Debug.Log($"UGUIPatcher: UnityGUI Runtime and Test Scripts have been deleted.");
        }

        private void PatchAssembly()
        {
            var settings = ThunderKitSetting.GetOrCreateSettings<ThunderKitSettings>();
            var originalAssembly = Path.GetFullPath(Path.Combine(settings.ManagedAssembliesPath, ASSEMBLY_FILE_NAME));
            var destinationAssembly = Path.GetFullPath(Path.Combine(settings.PackageFilePath, ASSEMBLY_FILE_NAME));
            var diffPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(BSDIFF_PATCH_GUID));
            var packagePath = Path.GetDirectoryName(destinationAssembly);

            if (File.Exists(destinationAssembly))
            {
                File.Delete(destinationAssembly);
            }

            Directory.CreateDirectory(packagePath);

            BsDiff.BsTool.Patch(originalAssembly, destinationAssembly, diffPath);

            var asmPath = destinationAssembly.Replace("\\", "/");
            var destinationMetadata = Path.Combine(settings.PackageFilePath, $"{ASSEMBLY_FILE_NAME}.meta");
            PackageHelper.WriteAssemblyMetaData(asmPath, destinationMetadata);

            var escape = false;
            while (EditorApplication.isUpdating && !escape)
            {
                var x = escape;
            }

            Debug.Log($"UGUIPatcher: The Game's {ASSEMBLY_FILE_NAME} has been patched.");
        }

        private void ModifyEditorAssemblyDef()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(EDITOR_SCRIPTS_ASSEMBLYDEF_GUID);
            AssemblyDefinitionAsset assemblyDefinitionAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);

            AssemblyDef assemblyDef = JsonUtility.FromJson<AssemblyDef>(assemblyDefinitionAsset.text);

            assemblyDef.overrideReferences = true;
            assemblyDef.precompiledReferences = assemblyDef.precompiledReferences ?? Array.Empty<string>();
            ArrayUtility.AddRange(ref assemblyDef.precompiledReferences, new string[] { ASSEMBLY_FILE_NAME });

            var modifiedJson = EditorJsonUtility.ToJson(assemblyDef, true);

            File.WriteAllText(Path.GetFullPath(assetPath), modifiedJson);

            Debug.Log($"UGUIPatcher: Added the precompiled dlls to the Editor AssemblyDef");
        }
    }
}*/