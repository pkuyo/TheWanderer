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

namespace MMSC.Characher
{
    class ClimbWallFeature : FeatureBase
    {
        static readonly PlayerFeature<bool> ClimbWall = PlayerBool("wanderer/wall_climb");
        static readonly PlayerFeature<float> ClimbWallSpeed = PlayerFloat("wanderer/wall_climb_speed");
        public ClimbWallFeature(ManualLogSource log) :base(log)
        {
            On.RainWorld.OnModsInit += ClimbWallFeature_OnModsInit;
            _climbSlugHandGraphics = new ClimbSlugHandGraphics(log);
        }


        public void ClimbWallFeature_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            RegisterAllEnumExtensions();

            On.Player.ctor += Player_ctor;
            On.Player.Update += new On.Player.hook_Update(Player_Update);
            On.Player.MovementUpdate += new On.Player.hook_MovementUpdate(Player_MovementUpdate);
            On.Player.Jump += new On.Player.hook_Jump(Player_Jump);
            On.Player.GrabVerticalPole += new On.Player.hook_GrabVerticalPole(Player_GrabVerticalPole);
            On.Player.UpdateMSC += new On.Player.hook_UpdateMSC(Player_UpdateMSC);
            On.Player.Destroy += Player_Destroy;
      
            _log.LogDebug("ClimbWallFeature Init");
        }

        private void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            ClimbArgs.Remove(self);
            orig(self);
        }

        //注册enum
        public static void RegisterAllEnumExtensions()
        {
            ClimbWallFeature.ClimbBackWall = new Player.BodyModeIndex("ClimbBackWall", true);
        }


        private int CheckCanClimb(Player self, Vector2 pos,Vector2 bodyVec,float bodyWidth=0.0f ,Vector2 addpos =new Vector2(),bool ex=true)
        {
       

            int[] re = new int[2];
            re[1] = 3;
            for (int k = 0; k < ((bodyWidth == 0.0f) ? 1 : 2); k++)
            {
                re[k] = 0;

                IntVector2 add = new IntVector2(0, 0);
                add += self.room.GetTilePosition(pos + addpos.normalized * 10 +Vector2.Perpendicular(bodyVec).normalized*bodyVec * ((k==0)?1:-1));
                if (ex)
                {
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                        {
                            //判断预计点周边9个点 宽判
                            IntVector2 tmpadd = add + new IntVector2(i, j);
                            var titleacc = self.room.aimap.getAItile(tmpadd).acc;
                            if (titleacc == AItile.Accessibility.Wall || self.room.aimap.getAItile(add).terrainProximity < 2)
                                re[k] |= 1;
                            if (titleacc == AItile.Accessibility.Solid
                            || titleacc == AItile.Accessibility.Corridor)
                                re[k] |= 2;
                        }
                }
                else
                {
                    //单点 严判
                    var titleacc = self.room.aimap.getAItile(add).acc;
                    if (titleacc == AItile.Accessibility.Wall || self.room.aimap.getAItile(add).terrainProximity < 2)
                        re[k] |= 1;
                    if (titleacc == AItile.Accessibility.Solid
                    || titleacc == AItile.Accessibility.Corridor)
                        re[k] |= 2;
                }
            }
            var rre = re[0] & re[1];
            return rre;
        }
        private bool BlockBySoild(Player player,Vector2 pos, Vector2 addpos)
        {
            return player.room.aimap.getAItile(pos + addpos.normalized * 10).acc == AItile.Accessibility.Solid;
        }



        private void CancelWallClimb(Player self)
        {
            ClimbArgs[self].isClimb = false;
            ClimbArgs[self].pressedUsed = true;
            self.bodyMode = Player.BodyModeIndex.Default;
            _log.LogError("Cancel climb");
        }

        private void StartWallClimb(Player self)
        {
            ClimbArgs[self].isClimb = true;
            ClimbArgs[self].Reset(self.mainBodyChunk.vel);

            ClimbArgs[self].pressedUsed = true;
            _log.LogError("Start climb");
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!ClimbArgs.ContainsKey(self))
                ClimbArgs.Add(self, new BackWallClimb());
            else
                ClimbArgs[self] = new BackWallClimb();
        }

        private void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            //先于updateMSC调用
            if (self.bodyMode == ClimbBackWall)
                self.customPlayerGravity = 0;
            else
                self.customPlayerGravity = 0.9f;
            orig(self);
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self,eu);

            if (ClimbArgs[self].slowDownCount > 0)
                ClimbArgs[self].slowDownCount--;

            var _isClimb = ClimbArgs[self].isClimb;
            var vel = ClimbArgs[self].BodyVel;
            var slowDownCount = ClimbArgs[self].slowDownCount;

            if (_isClimb)
            {
                ClimbWallSpeed.TryGet(self,out MaxSpeed);
                //设置状态
                if ((self.bodyMode == Player.BodyModeIndex.Default
                    || self.bodyMode == Player.BodyModeIndex.WallClimb
                    || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam) && !self.room.GetTile(self.mainBodyChunk.pos).horizontalBeam)
                {
                    self.bodyMode = ClimbBackWall;
                }
                else
                {
                    //爬杆钻管道等优先级高的操作 直接取消爬墙
                    CancelWallClimb(self);
                    _log.LogDebug("Cancel climb cause by " + self.bodyMode.ToString());
                    return;
                }

                //速度计算
                vel.y = self.input[0].y*4f;
                vel.x = self.input[0].x*6f;
                vel = (vel.magnitude > Mathf.Lerp(0, MaxSpeed,
                    (10 - slowDownCount) / 10.0f)) ? vel.normalized * Mathf.Lerp(0, MaxSpeed, (10 - slowDownCount) / 10.0f) : vel;

                //速度衰减
                vel *= self.airFriction;

                //计算边界位置 并尝试减速
                var pos = self.mainBodyChunk.pos;
                if (CheckCanClimb(self, pos ,Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos), BodyWidth , new Vector2(vel.x, 0.0f)) == 0) vel.x *= 0.3f;
                if (CheckCanClimb(self, pos, Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos),BodyWidth  ,new Vector2(0.0f, vel.y)) == 0) vel.y *= 0.3f;

                self.bodyChunks[0].vel = self.bodyChunks[1].vel = ClimbArgs[self].BodyVel = vel;

                //从墙上掉落
                var climbState = CheckCanClimb(self, pos, Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos),BodyWidth ) | 
                    CheckCanClimb(self, pos , Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos), BodyWidth  ,Custom.DirVec(self.bodyChunks[0].pos , self.bodyChunks[1].pos));
                if (climbState == 0)
                {
                    CancelWallClimb(self);
                    ClimbArgs[self].isFall = true;
                    _log.LogDebug("Fall from wall!");
                    return;
                }
                else if (climbState == 2)
                    ClimbArgs[self].isSideWall = true;
                else
                    ClimbArgs[self].isSideWall = false; //侧墙不掉头


                //转向速度
                if (!ClimbArgs[self].isSideWall && new Vector2(self.input[0].x, self.input[0].y) != Vector2.zero)
                {
                    var turnSpeed = (new Vector2(self.input[0].x, self.input[0].y).normalized - (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized) * 3;
                    var slowDown = vel.magnitude / (turnSpeed.magnitude * 3 + vel.magnitude);
                    //反向移动时速度降低
                    self.bodyChunks[1].vel *= slowDown;
                    self.bodyChunks[0].vel *= slowDown;

                    //撞墙取消横向速度
                    if (!BlockBySoild(self, self.mainBodyChunk.pos, vel)) 
                        self.bodyChunks[0].vel += turnSpeed;

                }


             

                
            }
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            if (self.bodyMode != ClimbBackWall)
                orig(self);
        }


        private void Player_GrabVerticalPole(On.Player.orig_GrabVerticalPole orig, Player self)
        {
            if (!ClimbArgs[self].isClimb) //防止中途身体状态更改
                orig(self);
        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            
            //获取技能参数
            bool canClimb;
            ClimbWall.TryGet(self, out canClimb);
            if (!canClimb)
                return;

            var pos = self.mainBodyChunk.pos;

            //落地取消抓墙保护
            if (self.canJump > 0 && ClimbArgs[self].isFall)
                ClimbArgs[self].isFall = false;

            //抓墙保护判定
            if (ClimbArgs[self].isFall)
                if (!ClimbArgs[self].isClimb && CheckCanClimb(self, pos,Vector2.zero, 0.0f, Vector2.zero, false)!=0)
                {
                    _log.LogDebug("Auto climb after fell");
                    StartWallClimb(self);
                    ClimbArgs[self].isFall = false;
                }


            //长按忽略
            //TODO : 多人支持
            if ((self.input[0].pckp && self.input[0].jmp) && !ClimbArgs[self].pressed)
                ClimbArgs[self].pressed = true;
            if (!(self.input[0].pckp && self.input[0].jmp))
            {
                ClimbArgs[self].pressed = false;
                ClimbArgs[self].pressedUsed = false;
            }

            //爬墙触发
            //计算人物坐标
            if (!ClimbArgs[self].isClimb && ClimbArgs[self].pressed && !ClimbArgs[self].pressedUsed && self.room.aimap.getAItile(pos).acc == AItile.Accessibility.Wall)
            {
                StartWallClimb(self);
            }
            else if (ClimbArgs[self].pressed && !ClimbArgs[self].pressedUsed && ClimbArgs[self].isClimb)
            {
                CancelWallClimb(self);
            }
        }

        private bool WallCheck(Player self,Vector2 pos)
        {
            if (true)
                return self.room.aimap.getAItile(pos).acc == AItile.Accessibility.Wall;
            else
                return self.room.GetTile(pos).wallbehind;
        }
 
        private ClimbSlugHandGraphics _climbSlugHandGraphics;
        public static Player.BodyModeIndex ClimbBackWall;

        public float MaxSpeed = 7;
        public readonly float BodyWidth = 6;
        


        public class BackWallClimb
        {
            public bool isClimb = false;

            public Vector2 BodyVel = new Vector2();
            public Vector2[] lastPos = new Vector2[2];

            public bool isSideWall = false;
            public bool isFall = false;

            //长按相关
            public bool pressed = false;
            public bool pressedUsed = false;

            public int slowDownCount = 0;

            public void Reset(Vector2 vel)
            {
                BodyVel = vel;
                lastPos = new Vector2[2];
            }
        }
        static public Dictionary<Player, BackWallClimb> ClimbArgs = new Dictionary<Player, BackWallClimb>();
        

    }
}
