using BepInEx.Logging;
using MonoMod.Cil;
using Pkuyo.Wanderer.Cosmetic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pkuyo.Wanderer.Feature
{
    class WandererGraphicsFeature : HookBase
    {
        WandererGraphicsFeature(ManualLogSource log) : base(log)
        {
            WandererGraphics = new ConditionalWeakTable<PlayerGraphics, WandererGraphics>();
        }

        static public WandererGraphicsFeature Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new WandererGraphicsFeature(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            
            _log.LogDebug("WandererGraphics Init");

        }


        private void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            WandererGraphics graphics;
            if (WandererGraphics.TryGetValue(self, out graphics))
                graphics.AddToContainer(sLeaser, rCam, newContatiner);

        }


        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);

            if ((self.owner as Player).slugcatStats.name.value != WandererCharacterMod.WandererName)
                return;

            WandererGraphics graphics;
            if (!WandererGraphics.TryGetValue(self, out graphics))
                WandererGraphics.Add(self, new WandererGraphics(self, _log));

        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            WandererGraphics graphics;
            if (WandererGraphics.TryGetValue(self, out graphics))
                graphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);

            WandererGraphics graphics;
            if (WandererGraphics.TryGetValue(self, out graphics))
                graphics.InitiateSprites(sLeaser, rCam);

        }

        private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);

            WandererGraphics graphics;
            if (WandererGraphics.TryGetValue(self, out graphics))
                graphics.ApplyPalette(sLeaser, rCam, palette);

        }

        private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);

            WandererGraphics graphics;
            if (WandererGraphics.TryGetValue(self, out graphics))
                graphics.Update();
        }



        public ConditionalWeakTable<PlayerGraphics, WandererGraphics> WandererGraphics;

        static WandererGraphicsFeature _Instance;
    }

    class WandererGraphics
    {

        public WandererGraphics(PlayerGraphics self, ManualLogSource log)
        {
            Cosmetics = new List<CosmeticBase>();
            BeforeCosmetics = new List<CosmeticBase>();
            selfRef = new WeakReference<PlayerGraphics>(self);
            _log = log;
            OriginSprites = EndSprites = -1;

            AddCosmetic(new WandererTailEffect(self, _log), true);
            AddCosmetic(new WandererHead(self, _log), true);
            //AddCosmetic(new WandererClimbShow(self, _log));
            AddCosmetic(new WandererBodyFront(self, _log));
            AddCosmetic(new WandererTailFin(self, _log));
            AddCosmetic(new WandererSpineSpikes(self, _log));
            AddCosmetic(new WandererLongHair(self, _log));

            //AddCosmetic(new WandererShaderTest(self, _log));
        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (OriginSprites == -1 || EndSprites > sLeaser.sprites.Length)
                return;
            foreach (var cosmetic in Cosmetics)
            {
                cosmetic.AddToContainer(sLeaser, rCam,newContatiner);
            }
        }
        private void AddCosmetic(CosmeticBase cosmetic, bool isBefore = false)
        {
            if (isBefore)
            {
                if (cosmetic.numberOfSprites != 0)
                    throw new Exception("Can't not insert cosmetic before playerGraphics.");
                BeforeCosmetics.Add(cosmetic);
            }
            else
                Cosmetics.Add(cosmetic);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            PlayerGraphics self;
            if (!selfRef.TryGetTarget(out self))
                return;

            //位置更新
            foreach (var cosmetic in BeforeCosmetics)
                cosmetic.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            foreach (var cosmetic in Cosmetics)
                cosmetic.DrawSprites(sLeaser, rCam, timeStacker, camPos);



        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics self;
            if (!selfRef.TryGetTarget(out self))
                return;

            foreach (var cosmetic in BeforeCosmetics)
                cosmetic.InitiateSprites(sLeaser, rCam);


            OriginSprites = sLeaser.sprites.Length;
            var startLength = sLeaser.sprites.Length;

            foreach (var cosmetic in Cosmetics)
            {
                cosmetic.startSprite = startLength;
                startLength += cosmetic.numberOfSprites;
            }
            Array.Resize(ref sLeaser.sprites, startLength);
            EndSprites = startLength;

            foreach (var cosmetic in Cosmetics)
            {
                cosmetic.InitiateSprites(sLeaser, rCam);
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics self;
            if (!selfRef.TryGetTarget(out self))
                return;


            //设置颜色
            foreach (var cosmetic in BeforeCosmetics)
                cosmetic.ApplyPalette(sLeaser, rCam, palette);
            foreach (var cosmetic in Cosmetics)
                cosmetic.ApplyPalette(sLeaser, rCam, palette);
        }

        public void Update()
        {

            PlayerGraphics self;
            if (!selfRef.TryGetTarget(out self))
                return;

            foreach (var cosmetic in BeforeCosmetics)
                cosmetic.Update();
            foreach (var cosmetic in Cosmetics)
                cosmetic.Update();
        }

        readonly WeakReference<PlayerGraphics> selfRef;
        readonly ManualLogSource _log;
        readonly List<CosmeticBase> BeforeCosmetics;
        readonly List<CosmeticBase> Cosmetics;

        public bool IsLounge
        {
            get
            {
                return _IsLounge;
            }
            set
            {
                if (IsLounge != value)
                {
                    foreach (var cosmetic in BeforeCosmetics)
                        cosmetic.IsLounge = value;
                    foreach (var cosmetic in Cosmetics)
                        cosmetic.IsLounge = value;
                    _IsLounge = value;
                }
            }
        }
        private bool _IsLounge;
        public int OriginSprites = -1;
        public int EndSprites;
    }

}
