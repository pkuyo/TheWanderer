using BepInEx.Logging;
using HarmonyLib;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Characher
{
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
            On.Player.Destroy += Player_Destroy;
        }

        private void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            for (int i = 0; i < 2; i++)
            {
                _HandOwner.Remove((self.graphicsModule as PlayerGraphics).hands[i]);
                _HandData.Remove((self.graphicsModule as PlayerGraphics).hands[i]);
            }
           orig(self);
        }

        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            for (int i = 0; i < 2; i++)
            {
                if (!_HandOwner.ContainsKey(((PlayerGraphics)self).hands[i]))
                {
                    _HandOwner.Add(((PlayerGraphics)self).hands[i], self.owner as Player);
                    _HandData.Add(((PlayerGraphics)self).hands[i], new ClimbSlugHand((i % 2 == 0) ? new Vector2(1, 1) : new Vector2(-1, -1)));
                }
                else 
                {
                    _HandOwner[((PlayerGraphics)self).hands[i]] = self.owner as Player;
                    _HandData[((PlayerGraphics)self).hands[i]]= new ClimbSlugHand((i % 2 == 0) ? new Vector2(1, 1) : new Vector2(-1, -1));
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
            var player = _HandOwner[self];
            if (player.bodyMode == ClimbWallFeature.ClimbBackWall)
            {
                var data = _HandData[self];
                var posDir = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                var velDir = player.bodyChunks[1].vel.normalized;

                //超出手部范围
                if (!Custom.DistLess(self.pos,data.LastGrapPos,StepLength))
                {
                    
                    data.LastGrapPos = self.pos;

                    //有地面的话手接触地面
                    var soildPos = CheckHasSoild(_HandOwner[self], self.connection.pos + velDir * (StepLength - 2));
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
                    ClimbWallFeature.ClimbArgs[player].slowDownCount += 5;
                    ClimbWallFeature.ClimbArgs[player].slowDownCount = Mathf.Min(ClimbWallFeature.ClimbArgs[player].slowDownCount, 9);
                }
                return false;
            }
            return re;
            //更换手部落点 


        }

        private ManualLogSource _log;
        private readonly float StepLength = 30.0f;
        private readonly float HandWidth = 10.0f;

        private static Dictionary<SlugcatHand, Player> _HandOwner = new Dictionary<SlugcatHand, Player>();
        private static Dictionary<SlugcatHand, ClimbSlugHand> _HandData = new Dictionary<SlugcatHand, ClimbSlugHand>();
    }
    public class ClimbSlugHand
    {
        public Vector2 VerDir;
        public Vector2 LastGrapPos;
        public ClimbSlugHand(Vector2 vector)
        {
            VerDir = vector;
            LastGrapPos = Vector2.zero;
        }
    }
}
