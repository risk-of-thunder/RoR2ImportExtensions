using ThunderKit.Core.Config;
using ThunderKit.Integrations.Thunderstore;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using UnityEditor.PackageManager;

namespace RiskOfThunder.RoR2Importer
{
    public class InstallMultiplayerHLAPI : OptionalExecutor
    {
        private const string API_REQUEST_URL = "https://api.github.com/repos/risk-of-thunder/UnityMultiplayerHLAPI/releases/latest";
        private const string GIT_URL = "https://github.com/risk-of-thunder/UnityMultiplayerHLAPI.git";
        public override int Priority => Constants.Priority.InstallMHLAPI;

        public override string Description => $"Installs the RoR2MultiplayerHLAPI Package from from GitHub";

        public override string Name => $"Install RoR2MultiplayerHLAPI";


        private IEnumerator _enumerator;

        public override bool Execute()
        {
            _enumerator ??= ExecuteCoroutine();
            while (_enumerator.MoveNext())
            {
                return false;
            }
            return true;
        }

        private IEnumerator ExecuteCoroutine()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(API_REQUEST_URL))
            {
                var asyncOp = webRequest.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    Debug.Log($"Waiting for Github API Request");
                    yield return null;
                }

                var release = JsonUtility.FromJson<GithubReleaseTag>(webRequest.downloadHandler.text);
                var request = Client.Add(GIT_URL + "#" + release);
                while (!request.IsCompleted)
                {
                    Debug.Log($"Waiting for Package Installation");
                    yield return null;
                }
            }
            yield break;
        }
        public override void Cleanup()
        {
            base.Cleanup();
            _enumerator = null;
        }
    }
}

