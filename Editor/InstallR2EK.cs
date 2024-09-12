using System.Collections;
using ThunderKit.Core.Config;
using ThunderKit.Integrations.Thunderstore;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfThunder.RoR2Importer
{
    public class InstallR2EK : OptionalExecutor
    {
        private const string API_REQUEST_URL = "https://api.github.com/repos/risk-of-thunder/RoR2EditorKit/releases/latest";
        private const string GIT_URL = "https://github.com/risk-of-thunder/RoR2EditorKit.git";
        public override int Priority => Constants.Priority.InstallR2EK;

        public override string Description => "Installs the RoR2 Editor Kit package from GitHub";

        public override string Name => "Install RoR2 Editor Kit";


        private IEnumerator _enumerator;

        public override bool Execute()
        {
            _enumerator ??= ExecuteCoroutine();
            while(_enumerator.MoveNext())
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
                while(!asyncOp.isDone)
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

