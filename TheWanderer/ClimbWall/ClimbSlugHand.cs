using BepInEx.Logging;
using HarmonyLib;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Characher
{
    //全场最烂 千万不要看
    class ClimbSlugHandGraphics
    {
        public ClimbSlugHandGraphics(ManualLogSource log)
        {
            _log = log;
           
        }
        public void OnModsInit()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor; ;
            On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
        }




        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            var player = self.owner as Player;
            PlayerBackClimb climb;
            if ( !ClimbWallFeature.Instance(_log).ClimbArg.TryGetValue(player, out climb))
                return;

            for (int i = 0; i < 2; i++)
            {
                if (self.hands[i]!=null && !_HandOwner.TryGetValue(self.hands[i], out player))
                {
                    player = self.owner as Player;
                    PlayerBackClimb tmp;
                    if (!ClimbWallFeature.Instance(_log).ClimbArg.TryGetValue(player, out tmp))
                        return;
                    _HandOwner.Add(self.hands[i], player);
                    _HandData.Add(self.hands[i], new ClimbSlugHand((i % 2 == 0) ? new Vector2(1, 1) : new Vector2(-1, -1), tmp));
                }
            }
        }
        private int CheckHasSoild(Player self, Vector2 pos)
        {
            IntVector2 add = new IntVector2(0, 0);
            add += self.room.GetTilePosition(pos);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    //判断预计点周边9个点
                    IntVector2 tmpadd = add + new IntVector2(i, j);
                    var titleacc = self.room.aimap.getAItile(tmpadd).acc;
                    if (titleacc == AItile.Accessibility.Solid
                        || titleacc == AItile.Accessibility.Floor)
                        return i;
                }
            return -2;
        }
        private bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand self)
        {

            var re = orig(self);
            Player player;
            if (_HandOwner.TryGetValue(self, out player) && player != null)
            {
                float maxSpeed;
                ClimbWallFeature.ClimbWallSpeed.TryGet(player, out maxSpeed);
                if (player.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                {
                    ClimbSlugHand data;
                    if(!_HandData.TryGetValue(self,out data))
                        return re;
                    var posDir = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                    var velDir = player.bodyChunks[1].vel.normalized;

                    //超出手部范围
                    if (!Custom.DistLess(self.pos, data.LastGrapPos, StepLength))
                    {

                        data.LastGrapPos = self.pos;

                        //有地面的话手接触地面
                        var soildPos = CheckHasSoild(player, self.connection.pos + velDir * (StepLength - 2));
                        if (soildPos != -2)
                        {
                            self.absoluteHuntPos = self.connection.pos + velDir * (StepLength - 2) + Vector2.Perpendicular(posDir).normalized * HandWidth * -soildPos;
                            self.FindGrip(player.room, self.absoluteHuntPos, self.absoluteHuntPos, 24f, self.absoluteHuntPos, 2, 2, false);
                        }
                        else
                        {
                            self.absoluteHuntPos = self.connection.pos + velDir * (StepLength - 2) + Vector2.Perpendicular(posDir).normalized * HandWidth * data.VerDir;
                            self.FindGrip(player.room, self.absoluteHuntPos, self.absoluteHuntPos, 24f, self.absoluteHuntPos, 2, 2, true);
                        }

                        //换点 减速
                        PlayerBackClimb backwall = null;
                        if (data.playerBackClimbRef.TryGetTarget(out backwall))
                        {
                            backwall.SlowDownCount += (int)(5 / maxSpeed);
                            backwall.SlowDownCount = Mathf.Min(backwall.SlowDownCount, 9);
                        }
                        else
                            throw new Exception();
                    }
                    return false;
                }
            }
            return re;
            //更换手部落点 


        }

        private ManualLogSource _log;
        private readonly float StepLength = 30.0f;
        private readonly float HandWidth = 10.0f;

        

        private static ConditionalWeakTable<SlugcatHand, Player> _HandOwner = new ConditionalWeakTable<SlugcatHand, Player>();
        private static ConditionalWeakTable<SlugcatHand, ClimbSlugHand> _HandData = new ConditionalWeakTable<SlugcatHand, ClimbSlugHand>();
    }
    public class ClimbSlugHand
    {
        public Vector2 VerDir;
        public Vector2 LastGrapPos;

        public WeakReference<PlayerBackClimb> playerBackClimbRef;

        public ClimbSlugHand(Vector2 vector, PlayerBackClimb player)
        {
            VerDir = vector;
            LastGrapPos = Vector2.zero;
            playerBackClimbRef = new WeakReference<PlayerBackClimb>(player);
        }
    }
}
