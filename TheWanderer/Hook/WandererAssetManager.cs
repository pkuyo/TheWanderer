using BepInEx.Logging;
using Pkuyo.Wanderer.Post;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class WandererAssetManager : HookBase
    {

        WandererAssetManager(ManualLogSource log) : base(log)
        {
            PostShaders = new Dictionary<string, Shader>();
            PlayerPos = new List<Vector2>();
        }

        static public WandererAssetManager Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new WandererAssetManager(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles/shaders/wanderershaders"));
            rainWorld.Shaders.Add("TwoColorShader", FShader.CreateShader("TwoColorShader", bundle.LoadAsset<Shader>("TwoColorShader")));
            rainWorld.Shaders.Add("ToolHoloGird", FShader.CreateShader("ToolHoloGird", bundle.LoadAsset<Shader>("ToolHoloGird")));
            rainWorld.Shaders.Add("VignetteMask", FShader.CreateShader("VignetteMask", bundle.LoadAsset<Shader>("VignetteMask")));

            PostShaders.Add("LoungePost", bundle.LoadAsset<Shader>("LoungePost"));

            Futile.atlasManager.LoadAtlas("atlases/wandererSprite");
            Futile.atlasManager.LoadImage("illustrations/fade");


            Camera cam = GameObject.FindObjectOfType<Camera>();
            PostEffect = cam.gameObject.AddComponent<PostEffect>();


            On.RainWorldGame.Update += RainWorldGame_Update;




        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            PlayerPos.Clear();
            foreach (var ab in self.AlivePlayers)
            {
                if (ab.realizedCreature != null)
                    PlayerPos.Add((ab.realizedCreature.mainBodyChunk.pos - self.cameras[0].pos)/Custom.rainWorld.screenSize);
            }
        }





        public Dictionary<string, Shader> PostShaders;

        public List<Vector2> PlayerPos;

        public PostEffect PostEffect;

        static private WandererAssetManager _Instance;
    }
}
