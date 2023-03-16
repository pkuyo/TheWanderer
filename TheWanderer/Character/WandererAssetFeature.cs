﻿using BepInEx.Logging;
using Pkuyo.Wanderer.Post;
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
        WandererAssetFeature(ManualLogSource log) : base(log)
        {
            PostShaders = new Dictionary<string, Shader>();
        }

        static public WandererAssetFeature Instance(ManualLogSource log)
        {
            if (_Instance == null)
                _Instance = new WandererAssetFeature(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            if (!ResourceLoaded)
            {
                var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles/shaders/wanderershaders"));
                rainWorld.Shaders.Add("TwoColorShader", FShader.CreateShader("TwoColorShader", bundle.LoadAsset<Shader>("TwoColorShader")));
                rainWorld.Shaders.Add("ShowWall", FShader.CreateShader("ShowWall", bundle.LoadAsset<Shader>("ShowWall")));
                PostShaders.Add("LoungePost",bundle.LoadAsset<Shader>("LoungePost"));
                Futile.atlasManager.LoadAtlas("atlases/wandererSprite");

                Camera cam = GameObject.FindObjectOfType<Camera>();
                PostEffect = cam.gameObject.AddComponent<PostEffect>();


                ResourceLoaded = true;
            }
        }

        public Dictionary<string, Shader> PostShaders;


        public PostEffect PostEffect;

        static private WandererAssetFeature _Instance;
    }
}
