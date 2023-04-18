using BepInEx.Logging;
using HarmonyLib;
using Nutils.hook;
using Pkuyo.Wanderer.Options;
using RWCustom;
using SlugBase.Features;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace Pkuyo.Wanderer.Feature
{
    class LoungeFeature : HookBase
    {
        static public readonly PlayerFeature<bool> CanLounge = PlayerBool("wanderer/lounge");
        LoungeFeature(ManualLogSource log) : base(log)
        {
            LoungeData = new ConditionalWeakTable<Player, PlayerLounge>();

        }



        static public LoungeFeature Instance(ManualLogSource log = null)
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
            On.Player.Die += Player_Die;
          

            On.Player.Jump += Player_Jump;
            _log.LogDebug("Lounge Feature Init");
        }

 

        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if (LoungeData.TryGetValue(self, out var lounge))
                lounge.Die();
            orig(self);
        }



        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (LoungeData.TryGetValue(self, out var lounge))
                lounge.Jump();
        }

        private void Mushroom_BitByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if ((grasp.grabber as Player).slugcatStats.name.value == WandererMod.WandererName)
                (grasp.grabber as Player).mushroomCounter -= 320;
        }

        private void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
        {
            Creature creature = (self.followAbstractCreature != null) ? self.followAbstractCreature.realizedCreature : null;
            if (creature != null && creature is Player)
            {
                if (LoungeData.TryGetValue(creature as Player, out var lounge))
                    lounge.SetMushroomEffect(self);
            }
            orig(self);
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (LoungeData.TryGetValue(self, out var lounge))
                lounge.Update();
            orig(self, eu);
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (!CanLounge.TryGet(self, out var canLounge) || !canLounge)
                return;

            if (!LoungeData.TryGetValue(self, out _))
                LoungeData.Add(self, new PlayerLounge(self));
        }



        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (LoungeData.TryGetValue(self, out var lounge))
                lounge.MovementUpdate();
        }

        public ConditionalWeakTable<Player, PlayerLounge> LoungeData;

        static private LoungeFeature _Instance;

        public class PlayerLounge
        {
            public PlayerLounge(Player player)
            {
                PlayerRef = new WeakReference<Player>(player);
                keyCode = WandererMod.WandererOptions.LoungeKeys[player.playerState.playerNumber].Value;

                runspeedFac = player.slugcatStats.runspeedFac;
                poleClimbSpeedFac = player.slugcatStats.poleClimbSpeedFac;
                corridorClimbSpeedFac = player.slugcatStats.corridorClimbSpeedFac;
                loudnessFac = player.slugcatStats.loudnessFac;

                PlayerBackClimb climb;
                if (ClimbWallFeature.Instance().ClimbFeatures.TryGetValue(player, out climb))
                    climbSpeed = climb.MaxSpeed;

                PlayerBaseAbility baseAbility;
                if (PlayerBaseHook.BaseAbilityData.TryGetValue(player, out baseAbility))
                {
                    rollSpeed = baseAbility.rollSpeed;
                    slideSpeed = baseAbility.slideSpeed;
                    jumpBoost = baseAbility.jumpBoost;
                }
            }

            public void Die()
            {
                Player self;
                if (!PlayerRef.TryGetTarget(out self))
                    return;
                StopLounge(self);
                IntroCount = 0;
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
                    //移动的火星
                    self.room.AddObject(new Spark(self.bodyChunks[1].pos + a * 5f, (a - self.firstChunk.vel).normalized * self.firstChunk.vel.magnitude * 1.3f, Color.white, null, 6, 12));
                }
            }

            public void Update()
            {
                Player self;
                if (!PlayerRef.TryGetTarget(out self))
                    return;

                var post = WandererAssetManager.Instance().PostEffect;
                if (IsLounge && post.LoungeCounter<=15)
                    post.LoungeCounter +=2;
                else if(IsLounge)
                     post.LoungeCounter=15;

                if (IsLounge)
                {
                    if (++FoodCount == 400 && !self.room.game.IsArenaSession)
                    {
                            self.AddFood(-1);

                        FoodCount -= 400;
                        if (self.playerState.foodInStomach == 0)
                        {
                            StartLounge(self);
                            IntroCount = 15;
                        }
                    }

                    //进入特效
                    if (IntroCount >= 0)
                    {
                        if (!WandererMod.WandererOptions.DisableDash.Value)
                        {
                            self.mushroomEffect = Custom.LerpAndTick(self.mushroomEffect, 3f, 0.15f, 0.025f);
                            Traverse.Create(self.adrenalineEffect).Field("intensity").SetValue(3 * (Traverse.Create(self.adrenalineEffect).Field("intensity").GetValue<float>()));
                        }
                        else
                        {
                            self.mushroomEffect = Custom.LerpAndTick(self.mushroomEffect, 0.2f, 0.15f, 0.025f);
                            
                        }

 
                        IntroCount--;
                        reset = true;
                        return;
                    }
                    //进入特效后半程
                    else if (reset && self.mushroomEffect > 0)
                    {
                        if (!WandererMod.WandererOptions.DisableDash.Value)
                            self.mushroomEffect = Custom.LerpAndTick(self.mushroomEffect, 0f, 0.15f, 0.025f);

                        if (self.mushroomEffect <= 0.0)
                            reset = false;
                        return;
                    }
                    //进行时特效
                    else
                    {

                        self.mushroomEffect = self.mushroomEffect >= 0.0f ? self.mushroomEffect : 0.0f;

                        if (Traverse.Create(self.adrenalineEffect).Field("intensity").GetValue<float>() < 0.5)
                            Traverse.Create(self.adrenalineEffect).Field("intensity").SetValue(0.5f);
                    }
                }
                //蘑菇效果会自己停

                //长按判断
                if (Input.GetKey(keyCode))
                {
                    keyDown = true;
                }
                else if (!Input.GetKey(keyCode))
                {
                    keyUse = false;
                    keyDown = false;
                }

                //切换状态
                if ((self.playerState.foodInStomach != 0 || self.room.game.IsArenaSession) && keyDown && !keyUse && !IsLounge && self.mushroomCounter == 0)
                {
                    StartLounge(self);
                    keyUse = true;
                    return;
                }
                else if (keyDown && !keyUse && IsLounge)
                {
                    StopLounge(self);
                    keyUse = true;
                    return;
                }

                if (IntroCount >= 0) IntroCount--;

            }

            public void Jump()
            {
                Player self;
                if (!PlayerRef.TryGetTarget(out self))
                    return;
                if (self.jumpBoost != 0 && IsLounge)
                    self.jumpBoost *= 1.3f;
            }

            private void StartLounge(Player self)
            {
                self.slugcatStats.runspeedFac = runspeedFac * 1.5f;
                self.slugcatStats.poleClimbSpeedFac = poleClimbSpeedFac * 1.7f;
                self.slugcatStats.corridorClimbSpeedFac = corridorClimbSpeedFac * 1.7f;
                self.slugcatStats.loudnessFac = loudnessFac * 1.5f;
                self.slugcatStats.throwingSkill = 2;

                PlayerBackClimb climb;
                if (ClimbWallFeature.Instance().ClimbFeatures.TryGetValue(self, out climb))
                    climb.MaxSpeed = climbSpeed * 1.7f;

                PlayerBaseAbility baseAbility;
                if (PlayerBaseHook.BaseAbilityData.TryGetValue(self, out baseAbility))
                {
                    baseAbility.rollSpeed = rollSpeed * 1.5f;
                    baseAbility.slideSpeed = slideSpeed * 1.5f;
                    baseAbility.jumpBoost = jumpBoost * 1.1f;
                }

                IntroCount = 15;
                //修改shader
                WandererGraphics graphics;
                if (self.graphicsModule != null && WandererGraphicsFeature.Instance().WandererGraphics.TryGetValue((self.graphicsModule as PlayerGraphics), out graphics))
                    graphics.IsLounge = true;

                IsLounge = true;
            }


            private void StopLounge(Player self)
            {
                self.slugcatStats.runspeedFac = runspeedFac;
                self.slugcatStats.poleClimbSpeedFac = poleClimbSpeedFac;
                self.slugcatStats.corridorClimbSpeedFac = corridorClimbSpeedFac;
                self.slugcatStats.loudnessFac = loudnessFac;
                self.slugcatStats.throwingSkill = 1;

                PlayerBackClimb climb;
                if (ClimbWallFeature.Instance().ClimbFeatures.TryGetValue(self, out climb))
                    climb.MaxSpeed = climbSpeed;

                PlayerBaseAbility baseAbility;
                if (PlayerBaseHook.BaseAbilityData.TryGetValue(self, out baseAbility))
                {
                    baseAbility.rollSpeed = rollSpeed;
                    baseAbility.slideSpeed = slideSpeed;
                    baseAbility.jumpBoost = jumpBoost;
                }

                IntroCount = 15;
                //修改shader
                WandererGraphics graphics;
                if (self.graphicsModule != null && WandererGraphicsFeature.Instance().WandererGraphics.TryGetValue((self.graphicsModule as PlayerGraphics), out graphics))
                    graphics.IsLounge = false;


                IsLounge = false;
            }

            public void SetMushroomEffect(RoomCamera rCam)
            {
                if (IsLounge && IntroCount == -1)
                    rCam.mushroomMode = 3;
                else if(IsLounge && IntroCount >=0 && WandererMod.WandererOptions.DisableDash.Value)
                    rCam.mushroomMode = Mathf.Lerp(3, 0, Mathf.InverseLerp(0, 15, IntroCount));
                else if (!IsLounge && IntroCount >= 0)
                    rCam.mushroomMode = Mathf.Lerp(0, 3, Mathf.InverseLerp(0, 15, IntroCount));

            }
            public bool IsLounge = false;

            bool keyDown = false;
            bool keyUse = false;


            int IntroCount = -1;
            bool reset = false;
            readonly KeyCode keyCode = KeyCode.None;


            int FoodCount = 0;
            readonly float runspeedFac;
            readonly float poleClimbSpeedFac;
            readonly float corridorClimbSpeedFac;
            readonly float loudnessFac;
            readonly float climbSpeed;
            readonly float slideSpeed;
            readonly float jumpBoost;
            readonly float rollSpeed;
            readonly WeakReference<Player> PlayerRef;


        }


    }
}
