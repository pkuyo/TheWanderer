using BepInEx.Logging;
using Pkuyo.Wanderer.Post;
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
            postDic = new ConditionalWeakTable<PlayerGraphics, PostData>();
            postData = new List<PostData>();
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

            PostShaders.Add("LoungePost", bundle.LoadAsset<Shader>("LoungePost"));

            Futile.atlasManager.LoadAtlas("atlases/wandererSprite");
            Futile.atlasManager.LoadImage("illustrations/fade");


            Camera cam = GameObject.FindObjectOfType<Camera>();
            PostEffect = cam.gameObject.AddComponent<PostEffect>();

            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.Player.NewRoom += Player_NewRoom;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;




        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (postDic.TryGetValue(self, out var data))
                data.DrawSprite(sLeaser);
        }

        private void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {

            orig(self, newRoom);
            if (self.graphicsModule != null && postDic.TryGetValue(self.graphicsModule as PlayerGraphics, out var data))
                data.NewRoom();
        }


        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            postData.RemoveAll(i => !i.IsVaild);
            orig(self,ow);
            if (!postDic.TryGetValue(self, out _))
            {
                var data = new PostData(self);
                postDic.Add(self, data);
                postData.Add(data);
            }
        }

        public Dictionary<string, Shader> PostShaders;

        ConditionalWeakTable<PlayerGraphics, PostData> postDic;

        public List<PostData> postData;

        public PostEffect PostEffect;

        static private WandererAssetManager _Instance;
    }
}
