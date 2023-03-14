using BepInEx.Logging;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using Random = UnityEngine.Random;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;

namespace Pkuyo.Wanderer.Characher
{
    class LoungeFeature : FeatureBase
    {
        static public readonly PlayerFeature<bool> CanLounge = PlayerBool("wanderer/lounge");
        LoungeFeature(ManualLogSource log) : base(log)
        {
            LoungeData = new ConditionalWeakTable<Player, PlayerLounge>();
        }



        static public LoungeFeature Instance(ManualLogSource log)
        {
            if (_Instance == null)
                _Instance = new LoungeFeature(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.Player.ctor += Player_ctor;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Update += Player_Update;

            On.RoomCamera.ApplyFade += RoomCamera_ApplyFade;

            On.Mushroom.BitByPlayer += Mushroom_BitByPlayer;
        }

        private void Mushroom_BitByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if ((grasp.grabber as Player).slugcatStats.name.value == "wanderer")
                (grasp.grabber as Player).mushroomCounter -= 320;
        }

        private void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
        {
            PlayerLounge lounge;
            Creature creature = (self.followAbstractCreature != null) ? self.followAbstractCreature.realizedCreature : null;
            if (creature != null && creature is Player)
            {
                if (LoungeData.TryGetValue(creature as Player, out lounge))
                    lounge.SetMushroomEffect(self);
            }
            orig(self);
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            PlayerLounge lounge;
            if (LoungeData.TryGetValue(self, out lounge))
                lounge.Update();
            orig(self, eu);
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            bool canLounge = false;
            if (!CanLounge.TryGet(self, out canLounge) || !canLounge)
                return;

            PlayerLounge lounge;
            if (!LoungeData.TryGetValue(self, out lounge))
                LoungeData.Add(self, new PlayerLounge(self));
        }

  

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            PlayerLounge lounge;
            if (LoungeData.TryGetValue(self, out lounge))
                lounge.MovementUpdate();
        }

        public ConditionalWeakTable<Player, PlayerLounge> LoungeData;

        static private LoungeFeature _Instance;

        public class PlayerLounge
        {
            public PlayerLounge(Player player)
            {
                PlayerRef = new WeakReference<Player>(player);
                WaveRef = new WeakReference<ShockWave>(null);
                keyCode = WandererCharacterMod.WandererOptions.LoungeKeys[player.playerState.playerNumber].Value;
            }
            public void MovementUpdate()
            {
                Player self;
                if (!PlayerRef.TryGetTarget(out self))
                    return;

                if (IsLounge && self.input[0].AnyDirectionalInput && self.canJump > 0 && self.firstChunk.vel != Vector2.zero &&
                        (self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.Stand))
                {
                    Vector2 a = Custom.RNV();
                    self.room.AddObject(new Spark(self.bodyChunks[1].pos + a * 5f, (a - self.firstChunk.vel).normalized * self.firstChunk.vel.magnitude * 3, Color.white, null, 4, 8));
                }
            }

            public void Update()
            {
                Player self;
                if (!PlayerRef.TryGetTarget(out self))
                    return;

                if (IsLounge)
                    if (++FoodCount == 400)
                    {
                        self.AddFood(-1);
                        FoodCount -= 400;
                        if (self.playerState.foodInStomach == 0)
                        {
                            IsLounge = false;
                            IntroCount = 15;
                        }
                    }

                if (IsLounge && IntroCount >= 0)
                {
                    self.mushroomEffect = Custom.LerpAndTick(self.mushroomEffect, 3f, 0.15f, 0.025f);
                    Traverse.Create(self.adrenalineEffect).Field("intensity").SetValue(3 * (Traverse.Create(self.adrenalineEffect).Field("intensity").GetValue<float>()) );
                    IntroCount--;
                    reset = true;
                    return;
                }else if(reset && self.mushroomEffect>0)
                {
                    self.mushroomEffect = Custom.LerpAndTick(self.mushroomEffect, 0f, 0.15f, 0.025f);
                    if(self.mushroomEffect<=0)
                        reset = false;
                    return;
                }

                if (Input.GetKey(keyCode))
                {
                    keyDown = true;
                }
                else if (!Input.GetKey(keyCode))
                {
                    keyUse = false;
                    keyDown = false;
                }
                if(self.playerState.foodInStomach != 0 && keyDown &&!keyUse && ! IsLounge && self.mushroomCounter==0)
                {
                    self.slugcatStats.runspeedFac *= 1.7f;
                    self.slugcatStats.poleClimbSpeedFac *= 2f;
                    self.slugcatStats.lungsFac *= 2f;
                    self.slugcatStats.corridorClimbSpeedFac *= 1.7f;
                    self.slugcatStats.loudnessFac *= 1.5f;
                    self.slugcatStats.throwingSkill = 2;
                    IntroCount = 15;
                    PlayerBackClimb climb;
                    if (ClimbWallFeature.Instance(null).ClimbArg.TryGetValue(self, out climb))
                        climb.MaxSpeed *= 1.7f;

                    //var wave = new ShockWave(self.mainBodyChunk.pos, 300f, 0.015f, 30, false);
                    //self.room.AddObject(wave);
                    //WaveRef = new WeakReference<ShockWave>(wave);

                    WandererGraphics graphics;
                    if (self.graphicsModule != null && WandererGraphicsFeature.Instance(null).WandererGraphics.TryGetValue((self.graphicsModule as PlayerGraphics), out graphics))
                        graphics.IsLounge = true;

                    IsLounge = true;
                    keyUse = true;
                    return;
                }
                else if(keyDown && !keyUse && IsLounge)
                {
                    self.slugcatStats.runspeedFac /= 1.7f;
                    self.slugcatStats.poleClimbSpeedFac /= 2f;
                    self.slugcatStats.lungsFac /= 2f;
                    self.slugcatStats.corridorClimbSpeedFac /= 1.7f;
                    self.slugcatStats.loudnessFac /= 1.5f;
                    self.slugcatStats.throwingSkill = 1;

                    IntroCount = 15;
                    PlayerBackClimb climb;
                    if (ClimbWallFeature.Instance(null).ClimbArg.TryGetValue(self, out climb))
                        climb.MaxSpeed /= 1.7f;

                    WandererGraphics graphics;
                    if (self.graphicsModule != null && WandererGraphicsFeature.Instance(null).WandererGraphics.TryGetValue((self.graphicsModule as PlayerGraphics), out graphics))
                        graphics.IsLounge = false;

                    IsLounge = false;
                    keyUse = true;
                    return;
                }

                if(IntroCount>=0)
                    IntroCount--;

            }

            public void SetMushroomEffect(RoomCamera rCam)
            {
                if (IsLounge && IntroCount == -1)
                    rCam.mushroomMode = 3;
                else if(!IsLounge && IntroCount >= 0)
                    rCam.mushroomMode = Mathf.Lerp(0, 3, Mathf.InverseLerp(0, 15,IntroCount));
                
            }
            public bool IsLounge = false;

            bool keyDown = false;
            bool keyUse = false;

            int IntroCount = -1;
            bool reset = false;

            KeyCode keyCode = KeyCode.None;

            int FoodCount = 0;

            WeakReference<Player> PlayerRef;
            WeakReference<ShockWave> WaveRef;
        }
    }
}
