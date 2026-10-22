using System;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Vampire.Editor
{
    // The old cream/snail stage remains retired. The new colon stage is part of the current game.
    public sealed class WebDemoStageScope:IDisposable
    {
        public WebDemoStageScope()
        {
            if(Directory.Exists("Assets/Resources/Stage2Snails")||File.Exists("Assets/Resources/StageTwoDefinition.asset"))
                throw new InvalidOperationException("Retired Stage 2 resources must not be restored into this release.");
        }
        public static void Verify(BuildReport report)
        {
            var leaked=report.packedAssets.SelectMany(p=>p.contents).Where(c=>c.sourceAssetPath.Contains("/Stage2Snails/")||c.sourceAssetPath.EndsWith("/StageTwoDefinition.asset")).ToArray();
            if(leaked.Length>0)throw new InvalidOperationException("Stage 2 assets leaked into build.");
            Debug.Log("[WebDemoStageScope] PASS: no retired cream/snail Stage 2 content.");
        }
        public void Dispose(){}
    }
}
