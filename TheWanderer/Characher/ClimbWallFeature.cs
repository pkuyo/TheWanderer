using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using RWCustom;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Pkuyo.Wanderer.Characher
{
    class ClimbWallFeature : FeatureBase
    {
        static public readonly PlayerFeature<bool> ClimbWall = PlayerBool("wanderer/wall_climb");
        static public readonly PlayerFeature<float> ClimbWallSpeed = PlayerFloat("wanderer/wall_climb_speed");
        public ClimbWallFeature(ManualLogSource log) :base(log)
        {
            _climbSlugHandGraphics = new ClimbSlugHandGraphics(log);
            ClimbArg = new Dictionary<Player, PlayerBackClimb>();
        }


        public override void OnModsInit(RainWorld rainWorld)
        {

            On.Player.ctor += Player_ctor;
            On.Player.Update += new On.Player.hook_Update(Player_Update);
            On.Player.MovementUpdate += new On.Player.hook_MovementUpdate(Player_MovementUpdate);
            On.Player.UpdateMSC += new On.Player.hook_UpdateMSC(Player_UpdateMSC);
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;

            On.Player.Jump += new On.Player.hook_Jump(Player_Jump);
            On.Player.GrabVerticalPole += new On.Player.hook_GrabVerticalPole(Player_GrabVerticalPole);

            On.Player.ThrowObject += Player_ThrowObject;

            _climbSlugHandGraphics.OnModsInit();

            _log.LogDebug("ClimbWallFeature Init");
        }

        private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            //TODO 下投矛
            orig(self, grasp, eu);
        }



        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            foreach(var arg in ClimbArg)
            {
                if (arg.Key == null)
                    ClimbArg.Remove(arg.Key);
            }

            orig(self, abstractCreature, world);

            var CanClimb = false;
            if (!ClimbArg.ContainsKey(self) && ClimbWall.TryGet(self, out CanClimb) && CanClimb)
                ClimbArg.Add(self, new PlayerBackClimb(_log, self));
  
        }

        private void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);
            PlayerBackClimb player;
            if (ClimbArg.TryGetValue(self, out player))
                player.UpdateBodyMode();


        }

        private void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            //先于updateMSC调用
            PlayerBackClimb player;
            if (ClimbArg.TryGetValue(self, out player))
                player.UpdateGravity();
            orig(self);
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self,eu);
            PlayerBackClimb player;
            if (ClimbArg.TryGetValue(self, out player))
                player.MovementUpdate();
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            PlayerBackClimb player;
            if (!ClimbArg.TryGetValue(self, out player) || !player.IsClimb)
                orig(self);
        }


        private void Player_GrabVerticalPole(On.Player.orig_GrabVerticalPole orig, Player self)
        {
            PlayerBackClimb player;
            if (!ClimbArg.TryGetValue(self, out player) || !player.IsClimb)
                orig(self);
        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            PlayerBackClimb player;
            if (ClimbArg.TryGetValue(self, out player))
                player.UpdateInput();
        }
 
        private ClimbSlugHandGraphics _climbSlugHandGraphics;

        public static Dictionary<Player, PlayerBackClimb> ClimbArg;
    }

    public class PlayerBackClimb
    {
   
        private Player owner = null;

        private Vector2 BodyVel = new Vector2();
        private Vector2[] lastPos = new Vector2[2];

        private bool isSideWall = false;
        private bool isFall = false;

        //长按相关
        private bool pressed = false;
        private bool pressedUsed = false;

        public int SlowDownCount = 0;

        public bool IsClimb = false;

        public float MaxSpeed = 1;
        public float DefaultSpeed = 3;
        public readonly float BodyWidth = 6;

        ManualLogSource _log;

        public PlayerBackClimb(ManualLogSource log,Player self) 
        {
            _log = log;
            owner = self;
            ClimbWallFeature.ClimbWallSpeed.TryGet(owner, out MaxSpeed);
        }
        
        public void Reset()
        {
            BodyVel = owner.mainBodyChunk.vel;
            lastPos = new Vector2[2];
        }

        private int CheckCanClimb(Player self, Vector2 pos, Vector2 bodyVec, float bodyWidth = 0.0f, Vector2 addpos = new Vector2(), bool ex = true)
        {

            int[] re = new int[2];
            re[1] = 3;
            for (int k = 0; k < ((bodyWidth == 0.0f) ? 1 : 2); k++)
            {
                re[k] = 0;

                IntVector2 add = new IntVector2(0, 0);
                add += self.room.GetTilePosition(pos + addpos.normalized * 10 + Vector2.Perpendicular(bodyVec).normalized * bodyVec * ((k == 0) ? 1 : -1));
                float dis = ex ? 2 : 1;
                var titleacc = self.room.aimap.getAItile(add).acc;
                if (titleacc == AItile.Accessibility.Wall || titleacc == AItile.Accessibility.Climb)
                    re[k] |= 1;
                if (titleacc == AItile.Accessibility.Solid
                || titleacc == AItile.Accessibility.Corridor || self.room.aimap.getAItile(add).terrainProximity < dis)
                    re[k] |= 2;
            }
            var rre = re[0] & re[1];
            return rre;
        }
        private bool BlockBySoild(Player player, Vector2 pos, Vector2 addpos)
        {
            return player.room.aimap.getAItile(pos + addpos.normalized * 10).acc == AItile.Accessibility.Solid;
        }



        private void CancelWallClimb()
        {
            pressedUsed = true;
            IsClimb = false;
            owner.bodyMode = Player.BodyModeIndex.Default;
            //_log.LogDebug("Cancel climb");
        }

        private void StartWallClimb()
        {
            pressedUsed = true;
            IsClimb = true;
            Reset();
            //_log.LogDebug("Start climb");
        }


        public void UpdateBodyMode()
        {
            if (IsClimb)
            {
                //设置状态
                if (owner.bodyMode == Player.BodyModeIndex.Default
                    || owner.bodyMode == Player.BodyModeIndex.WallClimb || owner.bodyMode == Player.BodyModeIndex.ZeroG
                    || owner.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || owner.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                {
                    owner.bodyMode = WandererModEnum.PlayerBodyModeIndex.ClimbBackWall;
                    //防止卡杆子
                    owner.forceFeetToHorizontalBeamTile = 0;
                    if (owner.animation == Player.AnimationIndex.HangFromBeam)
                        owner.animation = Player.AnimationIndex.None;
                }
                else
                {
                    //爬杆钻管道等优先级高的操作 直接取消爬墙
                    _log.LogDebug("[Climb] Cancel climb cause by " + owner.bodyMode.ToString());
                    CancelWallClimb();
                    return;
                }
            }
        }
        public void UpdateGravity()
        {
            //先于updateMSC调用
            if (owner.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                owner.customPlayerGravity = 0;
            else
                owner.customPlayerGravity = 0.9f;
        }

        public void MovementUpdate()
        {
            if (SlowDownCount > 0)
                SlowDownCount--;

            var _isClimb = IsClimb;
            var vel = BodyVel;

            if (_isClimb)
            {
               
                //速度计算
                vel.y += owner.input[0].y * 4f * MaxSpeed;
                vel.x += owner.input[0].x * 6f * MaxSpeed;
                vel = (vel.magnitude > Mathf.Lerp(0, MaxSpeed * DefaultSpeed,
                   Mathf.Pow((10 - SlowDownCount) / 10.0f,0.5f))) ? vel.normalized * Mathf.Lerp(0, MaxSpeed * DefaultSpeed, Mathf.Pow((10 - SlowDownCount) / 10.0f, 0.5f)) : vel;

                //速度衰减
                vel *= owner.airFriction;

                //计算边界位置 并尝试减速
                var pos = owner.mainBodyChunk.pos;
                if (CheckCanClimb(owner, pos, Custom.DirVec(owner.bodyChunks[0].pos, owner.bodyChunks[1].pos), BodyWidth, new Vector2(vel.x, 0.0f)) == 0) vel.x *= 0.5f;
                if (CheckCanClimb(owner, pos, Custom.DirVec(owner.bodyChunks[0].pos, owner.bodyChunks[1].pos), BodyWidth, new Vector2(0.0f, vel.y)) == 0) vel.y *= 0.5f;

                owner.bodyChunks[0].vel = owner.bodyChunks[1].vel = BodyVel = vel;

                //从墙上掉落
                var climbState = CheckCanClimb(owner, pos, Custom.DirVec(owner.bodyChunks[0].pos, owner.bodyChunks[1].pos), BodyWidth) |
                    CheckCanClimb(owner, pos, Custom.DirVec(owner.bodyChunks[0].pos, owner.bodyChunks[1].pos), BodyWidth, Custom.DirVec(owner.bodyChunks[0].pos, owner.bodyChunks[1].pos));
                if (climbState == 0)
                {
                    CancelWallClimb();
                    isFall = true;
                    _log.LogDebug("[Climb] Fall from wall!");
                    return;
                }
                else if (climbState == 2)
                    isSideWall = true;
                else
                    isSideWall = false; //侧墙不掉头


                //转向速度
                if (!isSideWall && new Vector2(owner.input[0].x, owner.input[0].y) != Vector2.zero)
                {
                    var turnSpeed = (new Vector2(owner.input[0].x, owner.input[0].y).normalized - (owner.bodyChunks[0].pos - owner.bodyChunks[1].pos).normalized) * 3;
                    var slowDown = vel.magnitude / (turnSpeed.magnitude * 3 + vel.magnitude);
                    //反向移动时速度降低
                    owner.bodyChunks[1].vel *= slowDown;
                    owner.bodyChunks[0].vel *= slowDown;

                    //撞墙取消横向速度
                    if (!BlockBySoild(owner, owner.mainBodyChunk.pos, vel))
                        owner.bodyChunks[0].vel += turnSpeed;

                }
            }
        }

        public void UpdateInput()
        {
            var pos = owner.mainBodyChunk.pos;

            //落地取消抓墙保护
            if (owner.canJump > 0 && isFall)
                isFall = false;

            //抓墙保护判定
            if (isFall)
                if (!IsClimb && CheckCanClimb(owner, pos, Vector2.zero, 0.0f, Vector2.zero, false) != 0)
                {
                    _log.LogDebug("[Climb] Auto climb after fell");
                    StartWallClimb();
                    isFall = false;
                }


            //长按忽略
            if ((owner.input[0].pckp && owner.wantToJump > 0) && !pressed)
                pressed = true;
            if (!(owner.input[0].pckp && owner.wantToJump > 0))
            {
                pressed = false;
                pressedUsed = false;
            }

            //爬墙触发
            //计算人物坐标
            if (!IsClimb && pressed && !pressedUsed && owner.room.aimap.getAItile(pos).acc == AItile.Accessibility.Wall)
            {
                StartWallClimb();
            }
            else if (pressed && !pressedUsed && IsClimb)
            {
                CancelWallClimb();
            }
        }

    }
}
