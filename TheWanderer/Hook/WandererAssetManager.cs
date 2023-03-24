using BepInEx.Logging;
using Pkuyo.Wanderer.Post;
using System.Collections.Generic;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class WandererAssetManager : HookBase
    {
        bool ResourceLoaded = false;
        WandererAssetManager(ManualLogSource log) : base(log)
        {
            PostShaders = new Dictionary<string, Shader>();
        }

        static public WandererAssetManager Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new WandererAssetManager(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            if (!ResourceLoaded)
            {
                var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles/shaders/wanderershaders"));
                rainWorld.Shaders.Add("TwoColorShader", FShader.CreateShader("TwoColorShader", bundle.LoadAsset<Shader>("TwoColorShader")));
                rainWorld.Shaders.Add("ShowWall", FShader.CreateShader("ShowWall", bundle.LoadAsset<Shader>("ShowWall")));
                rainWorld.Shaders.Add("ToolHoloGird", FShader.CreateShader("ToolHoloGird", bundle.LoadAsset<Shader>("ToolHoloGird")));


                PostShaders.Add("LoungePost", bundle.LoadAsset<Shader>("LoungePost"));

                Futile.atlasManager.LoadAtlas("atlases/wandererSprite");
                Futile.atlasManager.LoadImage("illustrations/fade");
                Camera cam = GameObject.FindObjectOfType<Camera>();
                PostEffect = cam.gameObject.AddComponent<PostEffect>();


                ResourceLoaded = true;
            }
        }

        public Dictionary<string, Shader> PostShaders;


        public PostEffect PostEffect;

        static private WandererAssetManager _Instance;
    }
}
