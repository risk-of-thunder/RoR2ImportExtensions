using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ThunderKit.Core.Config;
using ThunderKit.Core.Data;
using ThunderKit.Integrations.Thunderstore;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using ThunderKit.Core.Utilities;
using ThunderKit.Common.Configuration;
using ThunderKit.Core.Actions;

namespace RiskOfThunder.RoR2Importer
{
    public class R2APISubmoduleInstaller : OptionalExecutor
    {
        [Serializable]
        public class SerializedR2APIDependencies
        {
            [SerializeField]
            private string[] hardDependencies;

            public ReadOnlyCollection<string> GetDependencies(ThunderstoreSource source)
            {
                List<string> dependencies = new List<string>();
                for (int i = 0; i < hardDependencies.Length; i++)
                {
                    string dependencyId = hardDependencies[i];
                    if (string.IsNullOrEmpty(dependencyId) || string.IsNullOrWhiteSpace(dependencyId))
                    {
                        Debug.LogWarning($"Submodule Hard Dependency Warning: dependency at index {i} is null, empty, or whitespace.");
                        continue;
                    }

                    string[] splitID = dependencyId.Split('-');
                    if (splitID.Length <= 1)
                    {
                        Debug.LogWarning($"Submodule Hard Dependency Warning: dependency at index {i} is not formatted correctly, expected a split dependency ID array length of 2 or greater, got 1 or less");
                        continue;
                    }

                    string finalizedID = string.Join("-", splitID[0], splitID[1]);
                    dependencies.Add(finalizedID);
                }

                return new ReadOnlyCollection<string>(dependencies);
            }

            public SerializedR2APIDependencies() { }
            public SerializedR2APIDependencies(IEnumerable<SubmoduleInstallationData> toSerialize)
            {
                hardDependencies = toSerialize.Where(x => x != null && x.shouldInstall).Select(x => x.dependedncyID).ToArray();
            }
        }
        [Serializable]
        public class SubmoduleInstallationData
        {
            public string submoduleName;
            [TextArea(2, 3)]
            public string description;
            public string dependedncyID;
            public bool shouldInstall;
            public bool isHardDependency;
            public string[] dependencies;

            public SubmoduleInstallationData(ThunderKit.Core.Data.PackageVersion version, bool shouldInstall, bool isHardDependency)
            {
                this.submoduleName = version.group.name;
                this.description = version.group.Description;

                var splitDependencyId = version.dependencyId.Split('-');
                this.dependedncyID = string.Join("-", splitDependencyId[0], splitDependencyId[1]);
                this.shouldInstall = shouldInstall;
                dependencies = version.dependencies.Select(dep => dep.group.name).ToArray();
                this.isHardDependency = isHardDependency;
            }
        }

        private const string THUNDERSTORE_ADDRESS = "https://thunderstore.io";
        private const string AUTHOR_NAME = "RiskofThunder";
        private const string SUBMODULE_STARTING_WORDS = "R2API";
        private const string TRANSIENT_STORE_NAME = "transient-store";
        public override string Description => "Allows you to install any and all of R2API's SubModules.";
        public override int Priority => Constants.Priority.InstallR2API;
        public override string Name => $"R2API Submodule Installer";
        protected override string UITemplatePath => AssetDatabase.GUIDToAssetPath("751bced02e8b4e247ad9c3a75bd38321");

        /// <summary>
        /// The current submodule that's trying to be installed.
        /// </summary>
        public int submoduleIndex = -1;
        public bool serializeSelectionIntoJson = true;
        public List<SubmoduleInstallationData> r2apiSubmodules = new List<SubmoduleInstallationData>();
        public ReadOnlyCollection<string> hardDependencies;
        private SerializedObject serializedObject;
        private ListView listViewInstance;
        private ThunderstoreSource transientStore;
        public sealed override bool Execute()
        {
            try
            {
                //not initialized? begin initialization
                if (submoduleIndex == -1)
                {
                    submoduleIndex = 0;
                }
                transientStore = GetThunderstoreSource();
                //Finish when all submodules have tried to install.
                if (submoduleIndex >= r2apiSubmodules.Count)
                {
                    if (serializeSelectionIntoJson)
                    {
                        SerializeSelection();
                    }
                    Cleanup();
                    return true;
                }

                //there are still steps to execute, execute it and return false until we finish.
                ExecuteStep();
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                //We really want the step to return to -1 if an excecption gets thrown...
                Cleanup();
            }
            return true;
        }

        private void ExecuteStep()
        {
            SubmoduleInstallationData data = r2apiSubmodules[submoduleIndex];

            //If the current step's data shouldnt install, go to the next one.
            if (!data.shouldInstall)
            {
                submoduleIndex++;
                return;
            }

            bool? installInfo = InstallSubmodule(data);

            if (installInfo == true || installInfo == null)//Package installed or no package with id
            {
                submoduleIndex++;
                return;
            }
        }

        //Return true if the package installed, null if package with id doesnt exist, false if it cant install
        private bool? InstallSubmodule(SubmoduleInstallationData installationData)
        {
            try
            {
                if (transientStore.Packages == null || transientStore.Packages.Count == 0)
                {
                    Debug.LogWarning($"PackageSource at \"{THUNDERSTORE_ADDRESS}\" has no packages");
                    return false;
                }

                var package = transientStore.Packages.FirstOrDefault(pkg => pkg.DependencyId == installationData.dependedncyID);
                if (package == null)
                {
                    Debug.LogWarning($"Could not find package with DependencyId of \"{installationData.dependedncyID}\"");
                    return null;
                }

                if (package.Installed)
                {
                    if (IsInstalledVerisonTheLatestVersion(package))
                    {
                        Debug.LogWarning($"Not installing latest version of package with DependencyId of \"{installationData.dependedncyID}\" because it's already installed.");
                        return true;
                    }

                    //Version installed is not latest, delete it and then try again
                    Debug.LogWarning($"Uninstalling current version of package with DependencyId of \"{installationData.dependedncyID}\" because a newer one exists.");
                    UninstallPackage(package);
                    return false;
                }

                Debug.Log($"Installing latest version of package \"{installationData.dependedncyID}\"");
                var task = transientStore.InstallPackage(package, "latest");
                while (!task.IsCompleted)
                {
                    Debug.Log("Waiting for Completion...");
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }


        private bool IsInstalledVerisonTheLatestVersion(PackageGroup package)
        {
            var latestPackageVersion = package["latest"];

            Version latestVersion = new Version(latestPackageVersion.version);
            Version installedVersion = new Version(package.InstalledVersion);

            return installedVersion == latestVersion;
        }

        private void UninstallPackage(PackageGroup package)
        {
            var packageName = package.PackageManifest.name;
            ScriptingSymbolManager.RemoveScriptingDefine(packageName);
            var deletePackage = CreateInstance<DeletePackage>();
            deletePackage.directory = package.InstallDirectory;
            deletePackage.TryDelete();
            DestroyImmediate(deletePackage);
            AssetDatabase.Refresh();
        }

        private ThunderstoreSource GetThunderstoreSource()
        {
            var packageSource = PackageSourceSettings.PackageSources.OfType<ThunderstoreSource>().FirstOrDefault(src => src.Url == THUNDERSTORE_ADDRESS);
            if (!packageSource)
            {
                if (transientStore)
                {
                    packageSource = transientStore;
                    return packageSource;
                }
                packageSource = CreateInstance<ThunderstoreSource>();
                packageSource.Url = THUNDERSTORE_ADDRESS;
                packageSource.name = TRANSIENT_STORE_NAME;
                packageSource.ReloadPages(false);
                return packageSource;
            }
            else if (packageSource.Packages == null || packageSource.Packages.Count == 0)
            {
                packageSource.ReloadPages(false);
                return packageSource;
            }
            return packageSource;
        }

        private void Awake() => UpdateDependencies(false);
        private void OnEnable() => UpdateDependencies(false);

        private async void UpdateDependencies(bool forced)
        {
            transientStore = GetThunderstoreSource();

            while(transientStore.Packages == null || transientStore.Packages.Count == 0)
            {
                transientStore.ReloadPages(true);
                await Task.Delay(1000);
            }

            if (transientStore.Packages == null || transientStore.Packages.Count == 0)
            {
                Debug.LogWarning($"PackageSource at \"{THUNDERSTORE_ADDRESS}\" has no packages");
                Cleanup();
                return;
            }

            if(hardDependencies == null)
            {
                var serializedSelection = AssetDatabase.LoadAssetAtPath<TextAsset>("ASsets/ThunderKitSettings/SerializedR2APISubmoduleDependencies.json");
                if(serializedSelection)
                {
                    SerializedR2APIDependencies serializedDependencies = JsonUtility.FromJson<SerializedR2APIDependencies>(serializedSelection.text);
                    hardDependencies = serializedDependencies.GetDependencies(transientStore);
                }
            }

            var riskOfThunderPackages = transientStore.Packages.Where(pkg => pkg.Author == AUTHOR_NAME && pkg.PackageName.StartsWith(SUBMODULE_STARTING_WORDS)).ToList();

            if(riskOfThunderPackages == null || riskOfThunderPackages.Count == 0)
            {
                Debug.LogWarning($"Could not find any package that starts with {SUBMODULE_STARTING_WORDS} and it's author is {AUTHOR_NAME}");
                Cleanup();
                return;
            }

            if(!forced && riskOfThunderPackages.Count == r2apiSubmodules.Count)
            {
                Debug.Log("Not updating Dependencies for R2APISubmoduleInstaller as there is no difference between the current amount of Submodules and the cached count.");
                Cleanup();
                return;
            }

            UpdateDependencyList(riskOfThunderPackages);
            Cleanup();
        }

        private void UpdateDependencyList(List<PackageGroup> r2apiPackages)
        {
            r2apiSubmodules.Clear();
            foreach (var submodule in r2apiPackages)
            {
                bool isHardDependency = hardDependencies == null ? false : hardDependencies.Contains(submodule.DependencyId);
                r2apiSubmodules.Add(new SubmoduleInstallationData(submodule["latest"], true, isHardDependency));
            }

            var ordered = r2apiSubmodules.OrderBy(x => x.submoduleName).ToList();
            var coreIndex = r2apiSubmodules.FindIndex(x => x.dependedncyID == "RiskofThunder-R2API_Core");
            if(coreIndex != -1)
            {
                var core = ordered[coreIndex];
                ordered.Remove(core);
                ordered.Insert(0, core);
            }

            r2apiSubmodules = ordered;
        }
        protected override VisualElement CreateProperties()
        {
            if (hardDependencies == null)
            {
                var serializedSelection = AssetDatabase.LoadAssetAtPath<TextAsset>("ASsets/ThunderKitSettings/SerializedR2APISubmoduleDependencies.json");
                if (serializedSelection)
                {
                    SerializedR2APIDependencies serializedDependencies = JsonUtility.FromJson<SerializedR2APIDependencies>(serializedSelection.text);
                    hardDependencies = serializedDependencies.GetDependencies(transientStore);
                }
            }
            serializedObject = new SerializedObject(this);

            var root = base.CreateProperties();
            var buttonContainer = root.Q<VisualElement>("ButtonContainer");

            buttonContainer.Q<Button>("enableAll").clickable.clicked += EnableAllSubmodules;
            buttonContainer.Q<Button>("disableAll").clickable.clicked += DisableAllSubmodules;
            buttonContainer.Q<Button>("forceUpdatePackages").clickable.clicked += ForceUpdatePackages;
            if(hardDependencies != null)
            {
                buttonContainer.Add(new IMGUIContainer(() => EditorGUILayout.HelpBox("Some submodule dependencies have been marked as Hard dependencies by the serialized JSON data, said data can be found in your ThunderKitSettings folder.", MessageType.Info)));
            }

            listViewInstance = root.Q<ListView>("submoduleListView");

            return root;
        }

        private void ForceUpdatePackages()
        {
            UpdateDependencies(true);    
        }

        private void DisableAllSubmodules()
        {
            for(int i = 0; i < r2apiSubmodules.Count; i++)
            {
                var submodule = r2apiSubmodules[i];

                bool isHardDependency = hardDependencies == null ? false : hardDependencies.Contains(submodule.dependedncyID);
                if (submodule.submoduleName.Contains("Core") || isHardDependency)
                    continue;

                submodule.shouldInstall = false;
            }
            RefreshListView();
        }

        private void EnableAllSubmodules()
        {
            for(int i = 0; i < r2apiSubmodules.Count; i++)
            {
                var submodule = r2apiSubmodules[i];

                bool isHardDependency = hardDependencies == null ? false : hardDependencies.Contains(submodule.dependedncyID);
                if (submodule.submoduleName.Contains("Core") || isHardDependency)
                    continue;

                submodule.shouldInstall = true;
            }
            RefreshListView();
        }

        public void RefreshListView()
        {
            listViewInstance.Unbind();
            listViewInstance.Clear();
            listViewInstance.Bind(serializedObject);
        }
        public override void Cleanup()
        {
            if(transientStore)
            {
                DestroyImmediate(transientStore);
            }
            submoduleIndex = -1;
        }

        private void SerializeSelection()
        {
            SerializedR2APIDependencies newSerializedSelection = new SerializedR2APIDependencies(r2apiSubmodules);
            string json = JsonUtility.ToJson(newSerializedSelection, true);

            string relativePath = "Assets/ThunderKitSettings/SerializedR2APISubmoduleDependencies.json";
            string fullPath = Path.GetFullPath(relativePath);
            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
        }
    }

    [CustomPropertyDrawer(typeof(R2APISubmoduleInstaller.SubmoduleInstallationData))]
    public class SubmoduleInstallationDataDrawer : PropertyDrawer
    {
        R2APISubmoduleInstaller importer;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            importer = (R2APISubmoduleInstaller)property.serializedObject.targetObject;

            Toggle toggle = new Toggle
            {
                label = property.FindPropertyRelative(nameof(R2APISubmoduleInstaller.SubmoduleInstallationData.submoduleName)).stringValue.Substring("R2API_".Length),
                value = property.FindPropertyRelative(nameof(R2APISubmoduleInstaller.SubmoduleInstallationData.shouldInstall)).boolValue,
                tooltip = property.FindPropertyRelative(nameof(R2APISubmoduleInstaller.SubmoduleInstallationData.description)).stringValue,
                name = GetIndex(property).ToString()
            };

            ShouldBeEnabledOrDisabled(toggle);
            toggle.RegisterValueChangedCallback(OnToggleSet);
            return toggle;
        }

        private void ShouldBeEnabledOrDisabled(Toggle toggle)
        {
            var submodule = importer.r2apiSubmodules[int.Parse(toggle.name, CultureInfo.InvariantCulture)];

            //Always install serialized hard dependencies.
            var isSerializedHardDependency = importer.hardDependencies == null ? false : importer.hardDependencies.Contains(submodule.dependedncyID);
            if (submodule.isHardDependency || isSerializedHardDependency)
            {
                submodule.isHardDependency = true;
                toggle.SetEnabled(false);
                return;
            }
            if(submodule.dependedncyID.Contains("Core"))
            {
                //Core module should always be installed.
                toggle.SetEnabled(false);
            }

            if(submodule.dependencies.Contains("R2API_ContentManagement"))
            {
                //If the submodule is dependant on content management, and said module is not enabled, disable the toggle, alongside disabling the value.
                var contentManagementModule = importer.r2apiSubmodules.First(x => x.submoduleName == "R2API_ContentManagement");
                if(!contentManagementModule.shouldInstall)
                {
                    submodule.shouldInstall = false;
                    toggle.value = false;
                    toggle.SetEnabled(false);
                }
            }
        }

        private int GetIndex(SerializedProperty prop)
        {
            var path = prop.propertyPath;

            var split = path.Split('.');
            var match = split.Where(s => s.StartsWith("data[")).FirstOrDefault();
            List<char> nums = new List<char>();
            foreach(char c in match)
            {
                if(char.IsDigit(c))
                {
                    nums.Add(c);
                }
            }
            string num = new string(nums.ToArray());
            return int.Parse(num, CultureInfo.InvariantCulture);
        }

        private void OnToggleSet(ChangeEvent<bool> evt)
        {
            Toggle toggle = evt.target as Toggle;
            int index = int.Parse(toggle.name, CultureInfo.InvariantCulture);
            importer.r2apiSubmodules[index].shouldInstall = evt.newValue;
            ShouldBeEnabledOrDisabled(evt.target as Toggle);
            EditorUtility.SetDirty(importer);
            importer.RefreshListView();
        }
    }
}

