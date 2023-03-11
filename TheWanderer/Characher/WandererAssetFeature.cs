using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Characher
{
    class WandererAssetFeature : FeatureBase
    {
        bool ResourceLoaded = false;
        public WandererAssetFeature(ManualLogSource log) : base(log)
        {
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            if (!ResourceLoaded)
            {
                var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles/shaders/wanderershaders"));
                rainWorld.Shaders.Add("TwoColorShader", FShader.CreateShader("TwoColorShader", bundle.LoadAsset<Shader>("TwoColorShader")));

                Futile.atlasManager.LoadAtlas("atlases/wandererSprite");
          
                ResourceLoaded = true;
            }
        }
    }
}
