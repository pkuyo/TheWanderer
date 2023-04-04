using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SlugBase.Features;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace Pkuyo.Wanderer.Feature
{
    class PlayerBaseFeature : HookBase
    {
        static public readonly PlayerFeature<float> slideSpeed = PlayerFloat("wanderer/slide_speed");
        static public readonly PlayerFeature<float> rollSpeed = PlayerFloat("wanderer/roll_speed");
        static public readonly PlayerFeature<float> jumpBoost = PlayerFloat("wanderer/jump_boost");
        static public readonly PlayerFeature<float> poleJump = PlayerFloat("wanderer/pole_jump");
        static public readonly PlayerFeature<float> wallJump = PlayerFloat("wanderer/wall_jump");
        static public readonly PlayerFeature<float> slideJumpCD = PlayerFloat("wanderer/slide_jump_cd");
        PlayerBaseFeature(ManualLogSource log) : base(log)
        {
            BaseAbilityData = new ConditionalWeakTable<Player, PlayerBaseAbility>();

        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.Player.ctor += Player_ctor;
            On.Player.Jump += Player_Jump;
            On.Player.UpdateAnimation += Player_UpdateAnimation;
            On.Player.WallJump += Player_WallJump;

            IL.Player.UpdateAnimation += Player_UpdateAnimationIL;
            _log.LogDebug("PlayerBaseFeature Init");
        }

        private void Player_UpdateAnimationIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchStfld<Player>("whiplashJump"),
                i => i.MatchLdcI4(12),
                i => i.MatchStloc(26)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Player, int>>((self) =>
                {
                    PlayerBaseAbility ability = null;
                    if (Instance().BaseAbilityData.TryGetValue(self, out ability) && ability.slideJumpCD >= 0)
                        return (int)(6 * ability.slideJumpCD);
                    return 12;
                });
                c.Emit(OpCodes.Stloc_S, (byte)26);
            }
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(34),
                i => i.MatchStloc(26)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Player, int>>((self) =>
                {
                    PlayerBaseAbility ability = null;
                    if (Instance().BaseAbilityData.TryGetValue(self, out ability) && ability.slideJumpCD >= 0)
                        return (int)(20 * ability.slideJumpCD);
                    return 34;
                });
                c.Emit(OpCodes.Stloc_S, (byte)27);
            }

        }



        private void Player_WallJump(On.Player.orig_WallJump orig, Player self, int direction)
        {
            PlayerBaseAbility ability = null;
            if (BaseAbilityData.TryGetValue(self, out ability))
            {
                ability.lastWallJumpCheck = self.IsTileSolid(1, 0, -1) || self.IsTileSolid(0, 0, -1) ||
                    (self.input[0].x != 0 && self.bodyChunks[0].ContactPoint.x == self.input[0].x && self.IsTileSolid(0, self.input[0].x, 0) && !self.IsTileSolid(0, self.input[0].x, 1));
                orig(self, direction);
                ability.WallJump(direction);
            }
            else
                orig(self, direction);
        }


        private void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
        {
            PlayerBaseAbility ability = null;
            if (BaseAbilityData.TryGetValue(self, out ability))
            {
                ability.BeforeUpdateAnimation();
                orig(self);
                ability.UpdateAnimation();
            }
            else
                orig(self);

        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            float slide = -1;
            float roll = -1;
            float boost = -1;
            float jumpCD = -1;
            float pole = -1;
            float wall = -1;
            PlayerBaseAbility ability = null;
            bool hasModify = false;

            //短路问题!!!
            hasModify = slideSpeed.TryGet(self, out slide) || hasModify;
            hasModify = rollSpeed.TryGet(self, out roll) || hasModify;
            hasModify = jumpBoost.TryGet(self, out boost) || hasModify;
            hasModify = slideJumpCD.TryGet(self, out jumpCD) || hasModify;
            hasModify = poleJump.TryGet(self, out pole) || hasModify;
            hasModify = wallJump.TryGet(self,out wall) || hasModify;
            if (hasModify && !BaseAbilityData.TryGetValue(self, out ability))
                    BaseAbilityData.Add(self, new PlayerBaseAbility(self, slide, roll, boost, jumpCD, pole, wall));
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {

            PlayerBaseAbility ability = null;
            if (BaseAbilityData.TryGetValue(self, out ability))
            {
                ability.BeforeJump();
                orig(self);
                ability.Jump();
            }
            else
                orig(self);
        }

        static public PlayerBaseFeature Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new PlayerBaseFeature(log);
            return _instance;
        }

        static PlayerBaseFeature _instance;

        public ConditionalWeakTable<Player, PlayerBaseAbility> BaseAbilityData;

    }

    class PlayerBaseAbility
    {
        public PlayerBaseAbility(Player owner, float slide, float roll, float boost, float jumpCD, float pole,float wall)
        {
            ownerRef = new WeakReference<Player>(owner);
            slideSpeed = slide;
            rollSpeed = roll;
            jumpBoost = boost;
            slideJumpCD = jumpCD;
            poleJump = pole;
            wallJump = wall;
        }

        public void WallJump(int direction)
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self) || wallJump <= 0)
                return;

            float num = Mathf.Lerp(1f, 1.15f, self.Adrenaline);
            if (self.exhausted)
                num *= 1f - 0.5f * self.aerobicLevel;
            if (lastWallJumpCheck || self.bodyChunks[1].submersion > 0.1f)
            {
                if (self.bodyChunks[1].ContactPoint.y > -1 && self.bodyChunks[0].ContactPoint.y > -1 && self.Submersion == 0f)
                    num *= 0.7f;
                self.bodyChunks[0].vel.y = ((9f - 8f) * wallJump + 8) * num;
                self.bodyChunks[1].vel.y = ((8f - 7f) * wallJump + 8f) * num;
            }
            else
            {
                self.bodyChunks[0].vel.y = ((10f - 8f) * wallJump + 8) * num;
                self.bodyChunks[1].vel.y = ((9f - 7f) * wallJump + 7) * num;
                self.bodyChunks[0].vel.x = ((9f - 6f) * wallJump + 6) * num * (float)direction;
                self.bodyChunks[1].vel.x = ((7f - 5f) * wallJump + 5) * num * (float)direction;
            }
            self.jumpBoost = 4 * wallJump;
        }
        public void BeforeJump()
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self))
                return;

            lastRollDirection = self.rollDirection;
            lastSuperLaunchJump = self.superLaunchJump;
            lastAnim = self.animation;
            lastSlideCounter = self.slideCounter;
            lastWhiplashJump = self.whiplashJump;
            lastFlipDirection = self.flipDirection;
            lastBodyMode = self.bodyMode;
        }

       
        public void Jump()
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self))
                return;

            var slideY = (slideSpeed - 1) * 0.3f + 1;

            float num = Mathf.Lerp(1f, 1.15f, self.Adrenaline);
  
            
            if (!(lastBodyMode == Player.BodyModeIndex.CorridorClimb))
            {
                if (lastAnim == Player.AnimationIndex.ClimbOnBeam && poleJump >= 0)
                {
                    if (self.input[0].x != 0)
                    {

                        self.bodyChunks[0].vel.y = 9f * num * poleJump;
                        self.bodyChunks[1].vel.y = 8f * num* poleJump;
                        self.bodyChunks[0].vel.x = 9f * (float)lastFlipDirection * num* poleJump;
                        self.bodyChunks[1].vel.x = 7f * (float)lastFlipDirection * num* poleJump;
                        return;
                    }
                }
                else if (lastAnim == Player.AnimationIndex.Roll && rollSpeed >= 0)
                {
                    self.bodyChunks[0].vel.x *= rollSpeed;
                    self.bodyChunks[1].vel.x *= rollSpeed;
                }
                else if (lastAnim == Player.AnimationIndex.BellySlide && slideSpeed >= 0)
                {
                    var num2 = 18 * slideSpeed;

                    if (!lastWhiplashJump && self.input[0].x != -lastRollDirection)
                    {
                        var y = 10f * slideY;
                        self.bodyChunks[1].vel = new Vector2(lastRollDirection * num2, y) * num * (self.longBellySlide ? 1.2f : 1f);
                        self.bodyChunks[0].vel = new Vector2(lastRollDirection * num2, y) * num * (self.longBellySlide ? 1.2f : 1f);
                        return;
                    }
                    self.bodyChunks[0].vel = new Vector2(lastRollDirection * -11f * slideSpeed, 12f * slideY);
                    self.bodyChunks[1].vel = new Vector2(lastRollDirection * -11f * slideSpeed, 13f * slideY);
                }
                else if (!(lastAnim == Player.AnimationIndex.ZeroGSwim) && !(lastAnim == Player.AnimationIndex.ZeroGPoleGrab) && slideSpeed >= 0)
                {
                    if (!(lastAnim == Player.AnimationIndex.DownOnFours && self.bodyChunks[1].ContactPoint.y < 0 && self.input[0].downDiagonal == lastFlipDirection))
                    {
                        if (self.standing)
                        {
                            if (lastSlideCounter > 0 && lastSlideCounter < 10)
                            {
                                if (!self.PainJumps)
                                {
                                    self.bodyChunks[0].vel.y = 12f * num * slideY;
                                    self.bodyChunks[1].vel.y = 10f * num * slideY;
                                }
                            }
                            else
                            {
                                self.bodyChunks[0].vel.y = 6f * num * slideY;
                                self.bodyChunks[1].vel.y = 5f * num * slideY;
                            }
                        }
                        else
                        {
                            if (lastSuperLaunchJump >= 20)
                            {
                                float num6 = (12f * slideSpeed) - 9f;//9是已经加过的值
                                float num5 = ((self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1);
                                if (num5 != 0 && self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x == num5 > 0)
                                {
                                    self.bodyChunks[0].vel.x += (float)num5 * num6 * num;
                                    self.bodyChunks[1].vel.x += (float)num5 * num6 * num;
                                }
                            }
                        }
                    }
                }
            }
            else if (jumpBoost >= 0)
            {
                self.jumpBoost = 14f * jumpBoost;
            }
            return;

        }

        public void BeforeUpdateAnimation()
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self))
                return;
            lastRollCounter = self.rollCounter;
            lastRollDirection = self.rollDirection;
            lastSuperLaunchJump = self.superLaunchJump;
            lastAnim = self.animation;
            lastSlideCounter = self.slideCounter;
            lastWhiplashJump = self.whiplashJump;
            lastFlipDirection = self.flipDirection;
            lastBodyMode = self.bodyMode;
        }
        public void UpdateAnimation()
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self))
                return;
            if (lastAnim == Player.AnimationIndex.BellySlide && slideSpeed >= 0)
            {
                if (lastRollCounter < 6)//这里其实可以加个参数控制
                {
                    self.bodyChunks[1].vel.y -= 2.7f;
                    self.bodyChunks[1].vel.x += 9.1f * lastRollDirection;
                }
                float num7 = (20f * slideSpeed) - 14f;//原有速度
                float num8 = (25f * slideSpeed) - 18.1f;
                self.bodyChunks[0].vel.x += self.longBellySlide ? num7 : num8 * lastRollDirection * Mathf.Sin(lastRollCounter / (self.longBellySlide ? 39f : 15f) * 3.1415927f);
            }
        }

        readonly WeakReference<Player> ownerRef;
        public float slideSpeed;
        public float jumpBoost;
        public float rollSpeed;
        public float slideJumpCD;
        public float poleJump;
        public float wallJump;

        int lastRollDirection;
        int lastSuperLaunchJump;
        Player.AnimationIndex lastAnim;
        int lastSlideCounter;
        bool lastWhiplashJump;
        int lastFlipDirection;

        int lastRollCounter;
        Player.BodyModeIndex lastBodyMode;

        public bool lastWallJumpCheck = false;

    }
}
