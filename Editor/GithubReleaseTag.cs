using System;

namespace RiskOfThunder.RoR2Importer
{
    [Serializable]
    internal class GithubReleaseTag
    {
        public string tag_name;

        public override string ToString()
        {
            return tag_name;
        }
    }
}