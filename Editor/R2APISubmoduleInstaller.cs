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

namespace RiskOfThunder.RoR2Importer
{
    public class R2APISubmoduleInstaller : OptionalExecutor
    {
        [Serializable]
        public struct SubmoduleInstallationData
        {
            public string submoduleName;
            public string dependedncyID;
            public bool shouldInstall;
        
            public SubmoduleInstallationData(ThunderKit.Core.Data.PackageVersion version, bool shouldInstall)
            {
                this.submoduleName = version.group.name;
                this.dependedncyID = version.dependencyId;
                this.shouldInstall = shouldInstall;
            }
        }

        private const string THUNDERSTORE_ADDRESS = "https://thunderstore.io";
        private const string DEPENDENCY_ID = "tristanmcpherson-R2API";
        private const string TRANSIENT_STORE_NAME = "transient-store";
        public override int Priority => Constants.Priority.InstallR2API;
        public override string Description => $"Installs the R2API, a community supported common API for modding Risk of Rain 2";
        public override string Name => $"R2APISubmoduleInstaller";
        protected override string UITemplatePath => AssetDatabase.GUIDToAssetPath("751bced02e8b4e247ad9c3a75bd38321");

        public List<SubmoduleInstallationData> r2apiSubmodules = new List<SubmoduleInstallationData>();
        private ThunderstoreSource transientStore;
        private ListView listViewInstance;

        private void Awake() => UpdateDependencies(false);
        private void OnEnable() => UpdateDependencies(false);

        private async void UpdateDependencies(bool forced)
        {
            var store = GetThunderstoreSource();

            while(store.Packages == null || store.Packages.Count == 0)
            {
                store.ReloadPages();
                await Task.Delay(1000);
            }

            if (store.Packages == null || store.Packages.Count == 0)
            {
                Debug.LogWarning($"PackageSource at \"{THUNDERSTORE_ADDRESS}\" has no packages");
                return;
            }

            var package = store.Packages.FirstOrDefault(pkg => pkg.DependencyId == DEPENDENCY_ID);
            if(package == null)
            {
                Debug.LogWarning($"Could not find package with DependencyId of \"{DEPENDENCY_ID}\"");
                return;
            }

            var latestR2APIVersion = package["latest"];
            
            if(!forced && latestR2APIVersion.dependencies.Length == r2apiSubmodules.Count)
            {
                Debug.Log("Not updating Dependencies for R2APISubmoduleInstaller as the latest R2API version dependencies count matches the cached count.");
                return;
            }

            r2apiSubmodules.Clear();
            foreach(var dependency in latestR2APIVersion.dependencies)
            {
                r2apiSubmodules.Add(new SubmoduleInstallationData(dependency, true));
            }
        }

        private ThunderstoreSource GetThunderstoreSource()
        {
            var packageSource = PackageSourceSettings.PackageSources.OfType<ThunderstoreSource>().FirstOrDefault(src => src.Url == THUNDERSTORE_ADDRESS);
            if(!packageSource)
            {
                if(transientStore)
                {
                    packageSource = transientStore;
                    return packageSource;
                }
                packageSource = CreateInstance<ThunderstoreSource>();
                packageSource.Url = THUNDERSTORE_ADDRESS;
                packageSource.name = TRANSIENT_STORE_NAME;
                packageSource.ReloadPages();
                return packageSource;
            }
            else if (packageSource.Packages == null || packageSource.Packages.Count == 0)
            {
                packageSource.ReloadPages();
                return packageSource;
            }
            return packageSource;
        }
        public sealed override bool Execute()
        {
            return false;
        }

        protected override VisualElement CreateProperties()
        {
            var root = base.CreateProperties();
            var buttonContainer = root.Q<VisualElement>("ButtonContainer");

            buttonContainer.Q<Button>("enableAll").clickable.clicked += EnableAllSubmodules;
            buttonContainer.Q<Button>("disableAll").clickable.clicked += DisableAllSubmodules;
            buttonContainer.Q<Button>("forceUpdatePackages").clickable.clicked += ForceUpdatePackages;

            listViewInstance = root.Q<ListView>("submoduleListView");
            listViewInstance.itemsSource = r2apiSubmodules;
            listViewInstance.makeItem = CreateToggle;
            listViewInstance.bindItem = BindItem;
            listViewInstance.Refresh();

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
                submodule.shouldInstall = false;
                r2apiSubmodules[i] = submodule;
            }
            listViewInstance?.Refresh();
        }

        private void EnableAllSubmodules()
        {
            for(int i = 0; i < r2apiSubmodules.Count; i++)
            {
                var submodule = r2apiSubmodules[i];
                submodule.shouldInstall = true;
                r2apiSubmodules[i] = submodule;
            }
            listViewInstance?.Refresh();
        }

        private VisualElement CreateToggle() => new Toggle();

        private void BindItem(VisualElement element, int i)
        {
            Toggle toggle = (Toggle)element;
            SubmoduleInstallationData submodule = r2apiSubmodules[i];

            toggle.label = submodule.submoduleName;
            toggle.value = submodule.shouldInstall;
            toggle.name = i.ToString();
            toggle.RegisterValueChangedCallback(OnSubmoduleToggle);
        }

        private void OnSubmoduleToggle(ChangeEvent<bool> evt)
        {
            int index = int.Parse(((VisualElement)evt.target).name, CultureInfo.InvariantCulture);
            SubmoduleInstallationData data = r2apiSubmodules[index];
            data.shouldInstall = evt.newValue;
            r2apiSubmodules[index] = data;
        }
    }
}

