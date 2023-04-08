using BepInEx.Logging;
using RWCustom;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace Pkuyo.Wanderer.Feature
{
    class ScareLizardFeature : HookBase
    {
        static readonly PlayerFeature<bool> ScareLizard = PlayerBool("wanderer/scare_lizard");
        ScareLizardFeature(ManualLogSource log) : base(log)
        {
            _ScareLizardData = new ConditionalWeakTable<Player, PlayerScareLizard>();
        }

        public static ScareLizardFeature Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new ScareLizardFeature(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.Player.checkInput += Player_checkInput;
            On.Player.ctor += Player_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            _log.LogDebug("ScareLizardFeature Init");
        }

        private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            PlayerScareLizard tmp = null;
            if (_ScareLizardData.TryGetValue(self.owner as Player, out tmp))
                tmp.GraphicsUpdate();
        }

        //为了修改输入
        private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);

            PlayerScareLizard tmp = null;
            if (_ScareLizardData.TryGetValue(self, out tmp))
                tmp.CheckInput();


        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            bool isEnable;

            if (!ScareLizard.TryGet(self, out isEnable))
                return;

            PlayerScareLizard tmp = null;
            if (!_ScareLizardData.TryGetValue(self, out tmp) && isEnable)
                _ScareLizardData.Add(self, new PlayerScareLizard(self));


        }

        readonly ConditionalWeakTable<Player, PlayerScareLizard> _ScareLizardData;

        static private ScareLizardFeature _Instance;

        class PlayerScareLizard
        {
            public PlayerScareLizard(Player player)
            {
                PlayerRef = new WeakReference<Player>(player);
            }
            public void CheckInput()
            {
                Player self = null;
                if (!PlayerRef.TryGetTarget(out self))
                    return;


                if (self.input[0].pckp && self.input[0].thrw && !self.lungsExhausted && ScareLizardCD >= 20 && self.dangerGrasp == null)
                {
                    self.input[0].thrw = false;

                    ScareLizardCD = 0;



                    //劳累状态
                    self.lungsExhausted = true;
                    self.airInLungs = 0.05f;
                    self.Stun(5);

                    //惊吓物体
                    var scareObject = new WandererScareObject(self.mainBodyChunk.pos);
                    self.room.AddObject(new ShockWave(self.mainBodyChunk.pos, 175f, 0.035f, 15, false));
                    self.room.AddObject(scareObject);

                    //一个粒子效果
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 a = Custom.RNV();
                        self.room.AddObject(new Spark(self.mainBodyChunk.pos + a * 10f, a.normalized * 10, Color.white, null, 8, 24));
                    }

                    self.room.PlaySound(SoundID.Firecracker_Disintegrate, self.mainBodyChunk.pos);

                }
                else if (self.input[0].pckp && self.input[0].thrw)
                {
                    if (!self.lungsExhausted)
                        ScareLizardCD++;

                    self.input[0].thrw = false;
                    self.input[0].pckp = false;
                }
                else if (!(self.input[0].pckp && self.input[0].thrw) && ScareLizardCD > 0)
                    ScareLizardCD--;
            }

            public void GraphicsUpdate()
            {
                Player self = null;
                if (!PlayerRef.TryGetTarget(out self))
                    return;

                if (ScareLizardCD > 0 && self.graphicsModule != null)
                {
                    (self.graphicsModule as PlayerGraphics).blink = 5;
                }
            }

            readonly WeakReference<Player> PlayerRef;

            int ScareLizardCD = 0;

        }


        class WandererScareObject : UpdatableAndDeletable
        {
            public WandererScareObject(Vector2 pos)
            {
                this.pos = pos;
                this.threatPoints = new List<ThreatTracker.ThreatPoint>();
                this.fearRange = 1500f;
                this.fearScavs = true;
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                this.lifeTime++;
                WorldCoordinate worldCoordinate = this.room.GetWorldCoordinate(this.pos);
                if (!this.init)
                {
                    this.init = true;
                    for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
                    {
                        if (this.room.abstractRoom.creatures[i].realizedCreature != null && !this.room.abstractRoom.creatures[i].realizedCreature.dead && (this.fearScavs || this.room.abstractRoom.creatures[i].creatureTemplate.type != CreatureTemplate.Type.Scavenger) && Custom.DistLess(this.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, this.pos, this.fearRange))
                        {
                            if (this.room.abstractRoom.creatures[i].abstractAI != null && this.room.abstractRoom.creatures[i].abstractAI.RealAI != null && this.room.abstractRoom.creatures[i].abstractAI.RealAI.threatTracker != null)
                            {
                                if ((room.abstractRoom.creatures[i].abstractAI.RealAI is LizardAI) && (room.abstractRoom.creatures[i].abstractAI.RealAI as LizardAI).friendTracker.friend != null)
                                    continue;
                                this.threatPoints.Add(this.room.abstractRoom.creatures[i].abstractAI.RealAI.threatTracker.AddThreatPoint(null, worldCoordinate, 1f));
                                this.MakeCreatureLeaveRoom(this.room.abstractRoom.creatures[i].abstractAI.RealAI);
                            }
                        }
                    }
                }
                for (int j = 0; j < this.threatPoints.Count; j++)
                {
                    this.threatPoints[j].severity = Mathf.InverseLerp(700f, 500f, lifeTime);
                    this.threatPoints[j].pos = worldCoordinate;
                }
                if (this.lifeTime > 400)
                {
                    this.Destroy();
                }
            }

            private void MakeCreatureLeaveRoom(ArtificialIntelligence AI)
            {
                if (AI.creature.abstractAI.destination.room != this.room.abstractRoom.index)
                {
                    return;
                }
                int num = AI.threatTracker.FindMostAttractiveExit();
                if (num > -1 && num < this.room.abstractRoom.nodes.Length && this.room.abstractRoom.nodes[num].type == AbstractRoomNode.Type.Exit)
                {
                    int num2 = this.room.world.GetAbstractRoom(this.room.abstractRoom.connections[num]).ExitIndex(this.room.abstractRoom.index);
                    if (num2 > -1)
                    {
                        AI.creature.abstractAI.MigrateTo(new WorldCoordinate(this.room.abstractRoom.connections[num], -1, -1, num2));
                    }
                }
            }
            public override void Destroy()
            {
                for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
                {
                    if (this.room.abstractRoom.creatures[i].realizedCreature != null && !this.room.abstractRoom.creatures[i].realizedCreature.dead && this.room.abstractRoom.creatures[i].abstractAI != null && this.room.abstractRoom.creatures[i].abstractAI.RealAI != null && this.room.abstractRoom.creatures[i].abstractAI.RealAI.threatTracker != null)
                    {
                        for (int j = 0; j < this.threatPoints.Count; j++)
                        {
                            this.room.abstractRoom.creatures[i].abstractAI.RealAI.threatTracker.RemoveThreatPoint(this.threatPoints[j]);
                        }
                    }
                }
                base.Destroy();
            }
            public int lifeTime;
            public Vector2 pos;
            public List<ThreatTracker.ThreatPoint> threatPoints;
            private bool init;
            public bool fearScavs;
            public float fearRange;
        }



    }
}
