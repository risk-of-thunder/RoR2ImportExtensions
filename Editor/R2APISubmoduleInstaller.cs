using ThunderKit.Core.Config;
using System;
using System.Linq;
using ThunderKit.Core.Data;
using ThunderKit.Core.Utilities;
using UnityEditor;
using UnityEngine;
using ThunderKit.Integrations.Thunderstore;
using UObject = UnityEngine.Object;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;

namespace RiskOfThunder.RoR2Importer
{
    public class R2APISubmoduleInstaller : OptionalExecutor
    {
        [Serializable]
        public class SubmoduleInstallationData
        {
            public string submoduleName;
            [TextArea(2, 3)]
            public string description;
            public string dependedncyID;
            public bool shouldInstall;
            public string[] dependencies;
        
            public SubmoduleInstallationData(ThunderKit.Core.Data.PackageVersion version, bool shouldInstall)
            {
                this.submoduleName = version.group.name;
                this.description = version.group.Description;

                var splitDependencyId = version.dependencyId.Split('-');
                this.dependedncyID = string.Join("-", splitDependencyId[0], splitDependencyId[1]);
                this.shouldInstall = shouldInstall;
                dependencies = version.dependencies.Select(dep => dep.group.name).ToArray();
            }
        }

        private const string THUNDERSTORE_ADDRESS = "https://thunderstore.io";
        private const string AUTHOR_NAME = "RiskofThunder";
        private const string SUBMODULE_STARTING_WORDS = "R2API";
        private const string TRANSIENT_STORE_NAME = "transient-store";
        public override int Priority => Constants.Priority.InstallR2API;
        public override string Name => $"R2APISubmoduleInstaller";
        protected override string UITemplatePath => AssetDatabase.GUIDToAssetPath("751bced02e8b4e247ad9c3a75bd38321");

        public List<SubmoduleInstallationData> r2apiSubmodules = new List<SubmoduleInstallationData>();
        private SerializedObject serializedObject;
        private ListView listViewInstance;
        private ThunderstoreSource transientStore;
        private Task task;
        public sealed override bool Execute()
        {
            transientStore = GetThunderstoreSource();
            EditorApplication.LockReloadAssemblies();
            if(task == null)
            {
                task = new Task(ExecuteAsync);
                task.Start();
            }

            while (!task.IsCompleted)
                return false;

            EditorApplication.UnlockReloadAssemblies();
            return true;
        }

        private async void ExecuteAsync()
        {
            int i = 0;
            while(i < r2apiSubmodules.Count)
            {
                SubmoduleInstallationData data = r2apiSubmodules[i];
                if(!data.shouldInstall)
                {
                    i++;
                    continue;
                }

                while(EditorApplication.isCompiling)
                {
                    Debug.Log("Editor application compiling...");
                    await Task.Delay(10);
                }

                bool? installInfo = await InstallSubmoduleAsync(data);

                if(installInfo == true || installInfo == null) //Package installed or no package with id
                {
                    i++;
                    continue;
                }
            }

            Cleanup();
        }

        //Return true if the package installed, null if package with id doesnt exist, false if it cant install
        private async Task<bool?> InstallSubmoduleAsync(SubmoduleInstallationData installationData)
        {
            try
            {
                if(transientStore.Packages == null || transientStore.Packages.Count == 0)
                {
                    Debug.LogWarning($"PackageSource at \"{THUNDERSTORE_ADDRESS}\" has no packages");
                    await Task.Delay(5000);
                    return false;
                }

                var package = transientStore.Packages.FirstOrDefault(pkg => pkg.DependencyId == installationData.dependedncyID);
                if(package == null)
                {
                    Debug.LogWarning($"Could not find package with DependencyId of \"{installationData.dependedncyID}\"");
                    return null;
                }

                if (package.Installed)
                {
                    Debug.LogWarning($"Not installing package with DependencyId of \"{installationData.dependedncyID}\" because it's already installed");
                    return true;
                }

                Debug.Log($"Installing latest version of package \"{installationData.dependedncyID}\"");
                var task = transientStore.InstallPackage(package, "latest");
                while(!task.IsCompleted)
                {
                    Debug.Log("Waiting for Completion...");
                    await Task.Delay(25);
                }
                return true;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return true;
            }
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

            r2apiSubmodules.Clear();
            foreach (var submodule in riskOfThunderPackages)
            { 
                if(submodule.DependencyId.Contains("Core"))
                {
                    r2apiSubmodules.Insert(0, new SubmoduleInstallationData(submodule["latest"], true));
                }
                else if(submodule.DependencyId.Contains("ContentManagement"))
                {
                    r2apiSubmodules.Insert(0, new SubmoduleInstallationData(submodule["latest"], true));
                }
                else
                {
                    r2apiSubmodules.Add(new SubmoduleInstallationData(submodule["latest"], true));
                }
            }
            Cleanup();
        }

        protected override VisualElement CreateProperties()
        {
            serializedObject = new SerializedObject(this);

            var root = base.CreateProperties();
            var buttonContainer = root.Q<VisualElement>("ButtonContainer");

            buttonContainer.Q<Button>("enableAll").clickable.clicked += EnableAllSubmodules;
            buttonContainer.Q<Button>("disableAll").clickable.clicked += DisableAllSubmodules;
            buttonContainer.Q<Button>("forceUpdatePackages").clickable.clicked += ForceUpdatePackages;

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
                
                if (submodule.submoduleName.Contains("Core"))
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

                if (submodule.submoduleName.Contains("Core"))
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
                DestroyImmediate(transientStore, true);
            }

            task?.Dispose();
            task = null;
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

