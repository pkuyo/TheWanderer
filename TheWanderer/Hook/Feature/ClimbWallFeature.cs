using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase.Features;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace Pkuyo.Wanderer.Feature
{
    class ClimbWallFeature : HookBase
    {
        static public readonly PlayerFeature<float> ClimbWallSpeed = PlayerFloat("wanderer/wall_climb_speed");
        ClimbWallFeature(ManualLogSource log) :base(log)
        {
            _climbSlugHandGraphics = new ClimbSlugHandGraphics(log);
            ClimbFeatures = new ConditionalWeakTable<Player, PlayerBackClimb>();
        }

        static public ClimbWallFeature Instance(ManualLogSource log = null)
        {
            if (_Instance==null)
                _Instance = new ClimbWallFeature(log);
            return _Instance;
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

            IL.Player.ThrowObject += Player_ThrowObjectIL;

            

            _climbSlugHandGraphics.OnModsInit();

            _log.LogDebug("ClimbWallFeature Init");
        }

        private void Player_ThrowObjectIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if(c.TryGotoNext(MoveType.After,
                i => i.MatchCallOrCallvirt<Player>("get_ThrowDirection"),
                i => i.OpCode == OpCodes.Ldc_I4_0,
                i => i.MatchCall<IntVector2>(".ctor"),
                i => i.OpCode==OpCodes.Ldarg_0,
                i => i.MatchCallOrCallvirt<PhysicalObject>("get_firstChunk")))
            {
                c.GotoPrev(MoveType.After, i => i.OpCode == OpCodes.Ldarg_0);
                var label = c.DefineLabel();
                c.MarkLabel(label);

                //插入状态判断
                c.GotoPrev(MoveType.Before, i => i.OpCode == OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Player, bool>>((player) =>
                 {
                     if (player.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall && (player.input[0].x != 0 || player.input[0].y != 0))
                     {
                         return true;
                     }
                     return false;
                 });
                c.Emit(OpCodes.Brfalse,label);

                //如果爬墙状态则修改投矛方向
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Player, IntVector2>>((player) => new IntVector2(player.input[0].x, player.input[0].y));
                c.Emit(OpCodes.Stloc_S, (byte)0);
            }

        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {

            orig(self, abstractCreature, world);

            var speed = 0.0f;

            PlayerBackClimb tmp = null;
            if (!ClimbFeatures.TryGetValue(self,out tmp) && ClimbWallSpeed.TryGet(self, out speed))
                ClimbFeatures.Add(self, new PlayerBackClimb(_log, self));
  
        }

        private void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);
            PlayerBackClimb player;
            if (ClimbFeatures.TryGetValue(self, out player))
                player.UpdateBodyMode();


        }

        private void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            //先于updateMSC调用
            PlayerBackClimb player;
            if (ClimbFeatures.TryGetValue(self, out player))
                player.UpdateGravity();
            orig(self);
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self,eu);
            PlayerBackClimb player;
            if (ClimbFeatures.TryGetValue(self, out player))
                player.MovementUpdate();
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            PlayerBackClimb player;
            if (!ClimbFeatures.TryGetValue(self, out player) || !player.IsClimb)
                orig(self);
        }


        private void Player_GrabVerticalPole(On.Player.orig_GrabVerticalPole orig, Player self)
        {
            PlayerBackClimb player;
            if (!ClimbFeatures.TryGetValue(self, out player) || !player.IsClimb)
                orig(self);
        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            PlayerBackClimb player;
            if (ClimbFeatures.TryGetValue(self, out player))
                player.UpdateInput();
        }
 
        private ClimbSlugHandGraphics _climbSlugHandGraphics;

        public ConditionalWeakTable<Player, PlayerBackClimb> ClimbFeatures;

        static ClimbWallFeature _Instance;
    }

    public class PlayerBackClimb
    {
   
        private Player owner
        {
            get
            {
                Player a;
                if (ownerRef.TryGetTarget(out a))
                    return a;
                return null;
            }
        }
        private WeakReference<Player> ownerRef;

        private Vector2 BodyVel = new Vector2();

        private bool isSideWall = false;
        private bool isFall = false;

        //长按相关
        private bool pressed = false;
        private bool pressedUsed = false;

        public int SlowDownCount = 0;

        public bool IsClimb = false;

        private float maxSpeed = 1;

        public float MaxSpeed
        {
            get
            {
                var player = owner;
                if (player == null)
                    return maxSpeed;
                return maxSpeed * (EnergyCheck(player) ? 2 : 1);
            }
            set
            {
                maxSpeed = value;
            }
        }

        public float DefaultSpeed = 3;
        public readonly float BodyWidth = 6;

        ManualLogSource _log;

        public PlayerBackClimb(ManualLogSource log,Player self) 
        {
            _log = log;
            ownerRef = new WeakReference<Player>(self);
            ClimbWallFeature.ClimbWallSpeed.TryGet(owner, out maxSpeed);
        }
        
        public void Reset()
        {
            BodyVel = owner.mainBodyChunk.vel;
        }

        private int CheckCanClimb(Player self, Vector2 pos, Vector2 bodyVec, float bodyWidth = 0.0f, Vector2 addpos = new Vector2(), bool ex = true)
        {
            if (EnergyCheck(owner))
                 return 3;

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
            var player = owner;
            if (player == null)
                return;

            if (IsClimb)
            {
                //设置状态
                if (player.bodyMode == Player.BodyModeIndex.Default
                    || player.bodyMode == Player.BodyModeIndex.WallClimb || player.bodyMode == Player.BodyModeIndex.ZeroG
                    || player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || player.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                {
                    player.bodyMode = WandererModEnum.PlayerBodyModeIndex.ClimbBackWall;
                    //防止卡杆子
                    player.forceFeetToHorizontalBeamTile = 0;
                    if (player.animation == Player.AnimationIndex.HangFromBeam)
                        player.animation = Player.AnimationIndex.None;
                }
                else
                {
                    //爬杆钻管道等优先级高的操作 直接取消爬墙
                    _log.LogDebug("[Climb] Cancel climb cause by " + player.bodyMode.ToString());
                    CancelWallClimb();
                    return;
                }
            }
        }
        public void UpdateGravity()
        {
            var player = owner;
            if (player == null)
                return;

            //先于updateMSC调用
            if (player.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                player.customPlayerGravity = 0;
            else
                player.customPlayerGravity = 0.9f;
        }

        public void MovementUpdate()
        {
            if (SlowDownCount > 0)
                SlowDownCount--;

            var _isClimb = IsClimb;
            var vel = BodyVel;

            var player = owner;
            if (player == null)
                return;

            if (_isClimb)
            {

                //速度计算
                vel.y += player.input[0].y * 5f * MaxSpeed;
                vel.x += player.input[0].x * 5f * MaxSpeed;
                vel *= (0.8f / MaxSpeed);
                vel = (vel.magnitude > Mathf.Lerp(0, MaxSpeed * DefaultSpeed,
                Mathf.Pow((10 - SlowDownCount) / 10.0f,0.5f))) ? vel.normalized * Mathf.Lerp(0, MaxSpeed * DefaultSpeed, Mathf.Pow((10 - SlowDownCount) / 10.0f, 0.5f)) : vel;
                //vel = (vel.magnitude > MaxSpeed * DefaultSpeed )? vel.normalized * MaxSpeed * DefaultSpeed : vel;

                //速度衰减
               

                //计算边界位置 并尝试减速
                var pos = player.mainBodyChunk.pos;
                if (CheckCanClimb(player, pos, Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos), BodyWidth, new Vector2(vel.x, 0.0f)) == 0) vel.x *= 0.5f;
                if (CheckCanClimb(player, pos, Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos), BodyWidth, new Vector2(0.0f, vel.y)) == 0) vel.y *= 0.5f;

                player.bodyChunks[0].vel = player.bodyChunks[1].vel = BodyVel = vel;

                //从墙上掉落
                var climbState = CheckCanClimb(player, pos, Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos), BodyWidth) |
                    CheckCanClimb(player, pos, Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos), BodyWidth, Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos));
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
                if (!isSideWall && new Vector2(player.input[0].x, player.input[0].y) != Vector2.zero)
                {
                    var turnSpeed = (new Vector2(player.input[0].x, player.input[0].y).normalized - (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized) * 3;
                    var slowDown = vel.magnitude / (turnSpeed.magnitude * 3 + vel.magnitude);
                    //反向移动时速度降低
                    player.bodyChunks[1].vel *= slowDown;
                    player.bodyChunks[0].vel *= slowDown;

                    //撞墙取消横向速度
                    if (!BlockBySoild(player, player.mainBodyChunk.pos, vel))
                        player.bodyChunks[0].vel += turnSpeed;

                }
            }
        }

        private bool EnergyCheck(Player owner)
        {

            return (owner.grasps[0]!=null && owner.grasps[0].grabbed != null && owner.grasps[0].grabbed is CoolObject && (owner.grasps[0].grabbed as CoolObject).IsOpen);
          
        }
        public void UpdateInput()
        {
            var player = owner;
            var pos = player.mainBodyChunk.pos;
            if (player == null)
                return;

            //落地取消抓墙保护
            if (player.canJump > 0 && isFall)
                isFall = false;

            //抓墙保护判定
            if (isFall)
                if (!IsClimb && CheckCanClimb(player, pos, Vector2.zero, 0.0f, Vector2.zero, false) != 0)
                {
                    _log.LogDebug("[Climb] Auto climb after fell");
                    StartWallClimb();
                    isFall = false;
                }


            //长按忽略
            if ((player.input[0].pckp && player.wantToJump > 0) && !pressed)
                pressed = true;
            if (!(player.input[0].pckp && player.wantToJump > 0))
            {
                pressed = false;
                pressedUsed = false;
            }

            //爬墙触发
            //计算人物坐标

            if (!IsClimb && pressed && !pressedUsed && 
                (player.room.aimap.getAItile(pos).acc == AItile.Accessibility.Wall ||
                EnergyCheck(player)) )
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
