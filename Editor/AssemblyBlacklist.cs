using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ThunderKit.Core.Config;
using ThunderKit.Core.Data;
using UnityEditor.PackageManager;

namespace RiskOfThunder.RoR2Importer
{
    public class AssemblyBlacklist : BlacklistProcessor
    {
        public override string Name => "RoR2 Assembly Blacklist";

        public override int Priority => 1_000;

        public override IEnumerable<string> Process(IEnumerable<string> blacklist)
        {
            var importConfiguration = ThunderKitSetting.GetOrCreateSettings<ImportConfiguration>();

            if (importConfiguration.ConfigurationExecutors.OfType<InstallMultiplayerHLAPI>().Any(ie => ie.enabled) || IsRiskOfThunderHLAPIInstalled())
                blacklist = blacklist.Append("com.unity.multiplayer-hlapi.Runtime.dll");

            if (importConfiguration.ConfigurationExecutors.OfType<PostProcessingInstaller>().Any(ie => ie.enabled))
                blacklist = blacklist.Append("Unity.Postprocessing.Runtime.dll");

            return blacklist;
        }

        private bool IsRiskOfThunderHLAPIInstalled()
        {
            Debug.Log("Checking if HLAPI from RiskOfThunder is installed.");
            var listRequest = Client.List();
            while(!listRequest.IsCompleted)
            {
                Debug.Log("Waiting for list completion");
            }

            var list = listRequest.Result;

            if(list.FirstOrDefault(q => q.name == "com.unity.multiplayer-hlapi") != null)
            {
                Debug.Log("RiskOfThunder HLAPI installed, blacklisting precompiled runtime dll.");
                return true;
            }
            return false;
        }
    }
}