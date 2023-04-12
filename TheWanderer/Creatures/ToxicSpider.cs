using BepInEx.Logging;
using DevInterface;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Properties;
using Fisobs.Sandbox;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Pkuyo.Wanderer.Object;
using RWCustom;
using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.Creatures
{
    //TODO : 速度修改
    class ToxicSpiderHook : HookBase
    {
        ToxicSpiderHook(ManualLogSource log) : base(log)
        {
        }

        static public ToxicSpiderHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new ToxicSpiderHook(log);
            return _instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            //TODO : 一些速度修改
            On.BigSpider.ctor += BigSpider_ctor;
            On.BigSpider.Spit += BigSpider_Spit;
            

            On.BigSpiderAI.TrackerToDiscardDeadCreature += BigSpiderAI_TrackerToDiscardDeadCreature;
    
            On.BigSpiderAI.SpiderSpitModule.SpitPosScore += SpiderSpitModule_SpitPosScore;
            On.BigSpiderAI.SpiderSpitModule.Update += SpiderSpitModule_Update;
            On.BigSpiderAI.SpiderSpitModule.SpiderHasSpit += SpiderSpitModule_SpiderHasSpit;

            IL.BigSpider.Update += BigSpider_UpdateIL;
            IL.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += BigSpiderAI_IUseARelationshipTracker_UpdateDynamicRelationshipIL;
        }

        private void BigSpider_ctor(On.BigSpider.orig_ctor orig, BigSpider self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider)
            {

                self.bodyChunks[0] = new BodyChunk(self, 0, new Vector2(0f, 0f), 5f, 0.33333334f);
                self.bodyChunks[1] = new BodyChunk(self, 1, new Vector2(0f, 0f), 9f, 0.6666667f);
                self.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(self.bodyChunks[0], self.bodyChunks[1], 20f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f);
                self.spitter = true;
                self.yellowCol = Color.Lerp(new Color(0.1f, 0.1f, 1f), Custom.HSL2RGB(Random.value, Random.value, Random.value), Random.value * 0.2f);
            }

        }

        private void BigSpider_Spit(On.BigSpider.orig_Spit orig, BigSpider self)
        {
            if (self.abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider)
            {
                Vector2 vector = self.AI.spitModule.aimDir;
                if (self.safariControlled)
                {
                    if (self.inputWithDiagonals != null && self.inputWithDiagonals.Value.AnyDirectionalInput)
                    {
                        vector = new Vector2(self.inputWithDiagonals.Value.x, self.inputWithDiagonals.Value.y).normalized;
                    }
                    else
                    {
                        vector = self.travelDir.normalized;
                    }
                    Creature creature = null;
                    float num = float.MaxValue;
                    float current = Custom.VecToDeg(vector);

                    //锁定倒霉蛋
                    for (int i = 0; i < self.abstractCreature.Room.creatures.Count; i++)
                    {
                        if (self.abstractCreature != self.abstractCreature.Room.creatures[i] && self.abstractCreature.Room.creatures[i].realizedCreature != null)
                        {
                            float target = Custom.AimFromOneVectorToAnother(self.mainBodyChunk.pos, self.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                            float num2 = Custom.Dist(self.mainBodyChunk.pos, self.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                            if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 22.5f && num2 < num)
                            {
                                num = num2;
                                creature = self.abstractCreature.Room.creatures[i].realizedCreature;
                            }
                        }
                    }
                    if (creature != null)
                    {
                        vector = Custom.DirVec(self.mainBodyChunk.pos, creature.mainBodyChunk.pos);
                    }
                }
                self.charging = 0f;
                self.mainBodyChunk.pos += vector * 12f;
                self.mainBodyChunk.vel += vector * 2f;

                AbstractPhysicalObject abstractPhysicalObject = new AbstractPoisonNeedle(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID());

                abstractPhysicalObject.RealizeInRoom();
                (abstractPhysicalObject.realizedObject as PoisonNeedle).Shoot(self.mainBodyChunk.pos, vector, self);
                self.room.PlaySound(SoundID.Big_Spider_Spit, self.mainBodyChunk);
                self.AI.spitModule.SpiderHasSpit();
            }
            else
                orig(self);
        }

        private void SpiderSpitModule_SpiderHasSpit(On.BigSpiderAI.SpiderSpitModule.orig_SpiderHasSpit orig, BigSpiderAI.SpiderSpitModule self)
        {
            orig(self);
            if (self.bug.abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider)
            {
                if (self.ammo < 1)
                    self.bugAI.stayAway = false;
            }
        }




        private bool BigSpiderAI_TrackerToDiscardDeadCreature(On.BigSpiderAI.orig_TrackerToDiscardDeadCreature orig, BigSpiderAI self, AbstractCreature crit)
        {
            return orig(self, crit) && !(crit.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider);
        }


        private float SpiderSpitModule_SpitPosScore(On.BigSpiderAI.SpiderSpitModule.orig_SpitPosScore orig, BigSpiderAI.SpiderSpitModule self, WorldCoordinate test)
        {
            var re = orig(self, test);
            for (int i = 0; i < self.AI.tracker.CreaturesCount; i++)
            {
                if (self.AI.tracker.GetRep(i).representedCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider && self.AI.tracker.GetRep(i).representedCreature.personality.dominance > self.AI.creature.personality.dominance && self.AI.tracker.GetRep(i).representedCreature.abstractAI.RealAI != null)
                {
                    re -= Mathf.Min(20f, test.Tile.FloatDist((self.AI.tracker.GetRep(i).representedCreature.abstractAI.RealAI as BigSpiderAI).spitModule.spitPos.Tile)) * 5f;
                }
            }
            return re;
        }


        private void SpiderSpitModule_Update(On.BigSpiderAI.SpiderSpitModule.orig_Update orig, BigSpiderAI.SpiderSpitModule self)
        {
            orig(self);
            if (self.bug.abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider)
            {
                //增加毒针数量
                if (self.ammo < 6)
                {
                     //加快速度
                     self.ammoRegen += 1f / (self.fastAmmoRegen ? 20f : 300f);

                     if(self.ammo < 4)
                        self.ammoRegen -= 1f / (self.fastAmmoRegen ? 60f : 1200f);

                    if (self.ammoRegen > 1f)
                    {
                        self.ammo++;
                        self.ammoRegen -= 1f;
                    }
                }
                //减少cd
                if (self.randomCritSpitDelay == 139)
                    self.randomCritSpitDelay = 50;
            }

        }




        private void BigSpider_UpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After,i => i.MatchCallOrCallvirt<Creature>("get_Consious")))
            {
                c.Emit(OpCodes.Ldloc_S, (byte)20);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, IntVector2, BigSpider, bool>>((re, vec, self) =>
                {
                    if (self.abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider
                        && self.grabChunks[vec.x, vec.y] != null && (self.grabChunks[vec.x, vec.y].owner is Creature) && (self.grabChunks[vec.x, vec.y].owner as Creature).blind > 10)
                    {
                        re = false;
                    }
                    return re;
                });
            }
        }


        private void BigSpiderAI_IUseARelationshipTracker_UpdateDynamicRelationshipIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Creature>("get_Consious()")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<bool, BigSpiderAI, RelationshipTracker.DynamicRelationship, bool>>((re, self, dRelation) =>
                {
                    if (self.bug.abstractCreature.creatureTemplate.type == WandererModEnum.Creatures.ToxicSpider && dRelation.trackerRep.representedCreature.realizedCreature.blind > 5)
                    {
                        re = false;
                    }
                    return re;
                });
            }
        }
        static private ToxicSpiderHook _instance;
    }

    sealed class ToxicSpiderCritob : Critob
    {
        public ToxicSpiderCritob() : base(WandererModEnum.Creatures.ToxicSpider)
        {
            Icon = new SimpleIcon("Kill_BigSpider", new Color(0.1f, 0.1f, 1f));
            CreatureName = "Toxic Spider";
            LoadedPerformanceCost = 50f;
            SandboxPerformanceCost = new SandboxPerformanceCost(0.5f, 0.5f);
            RegisterUnlock(KillScore.Configurable(5), WandererModEnum.Sandbox.ToxicSpider, MultiplayerUnlocks.SandboxUnlockID.SpitterSpider, 0);
        }
        public override int ExpeditionScore()
        {
            return 5;
        }
        public override CreatureState CreateState(AbstractCreature acrit)
        {
            return new HealthState(acrit);
        }
        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new BigSpiderAI(acrit,acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new BigSpider(acrit,acrit.world);
        }

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate t = new CreatureFormula(this)
            {
                DefaultRelationship = new(CreatureTemplate.Relationship.Type.Ignores, 0.0f),
                HasAI = true,
                InstantDeathDamage = 1,
                Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.BigSpider),
                TileResistances = new()
                {
                    OffScreen = new(1, PathCost.Legality.Allowed),
                    Floor = new(1, PathCost.Legality.Allowed),
                    Corridor = new(1, PathCost.Legality.Allowed),
                    Climb = new(1.5f, PathCost.Legality.Allowed),
                    Wall = new(3, PathCost.Legality.Allowed),
                    Ceiling = new(3, PathCost.Legality.Allowed),
                },
                ConnectionResistances = new()
                {
                    Standard = new(1, PathCost.Legality.Allowed),
                    OpenDiagonal = new(3, PathCost.Legality.Allowed),
                    ReachOverGap = new(3, PathCost.Legality.Allowed),
                    ReachUp = new(3, PathCost.Legality.Allowed),
                    SemiDiagonalReach = new(2f, PathCost.Legality.Allowed),
                    DropToFloor = new(10f, PathCost.Legality.Allowed),
                    DropToWater = new(10f, PathCost.Legality.Unwanted),
                    DropToClimb = new(10f, PathCost.Legality.Unwanted),
                    ShortCut = new(1.5f, PathCost.Legality.Allowed),
                    NPCTransportation = new(3f, PathCost.Legality.Allowed),
                    OffScreenMovement = new(1, PathCost.Legality.Allowed),
                    BetweenRooms = new(5f, PathCost.Legality.Allowed),
                    Slope = new(1.5f, PathCost.Legality.Allowed),
                    CeilingSlope = new(1.5f, PathCost.Legality.Allowed),
                },
                DamageResistances = new()
                {
                    Base = 1.1f,
                },
                StunResistances = new()
                {
                    Base = 0.6f,
                }
            }.IntoTemplate();
            t.instantDeathDamageLimit = 1f;
            t.offScreenSpeed = 0.3f;
            t.abstractedLaziness = 50;
            t.AI = true;
            t.requireAImap = true;
            t.bodySize = 0.9f;
            t.stowFoodInDen = true;
            t.shortcutSegments = 2;
            t.grasps = 1;
            t.visualRadius = 1700f;
            t.throughSurfaceVision = 0.95f;
            t.communityInfluence = 0.1f;
            t.dangerousToPlayer = 0.45f;
            t.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;
            t.waterPathingResistance = 5f;
            t.canSwim = true;
            t.meatPoints = 3;
            t.interestInOtherAncestorsCatches = 0f;
            t.interestInOtherCreaturesCatches = 2f;
            
            t.jumpAction = "Spit";
            t.pickupAction = "Grab";
            t.throwAction = "Release";
            return t;
        }
        public override string DevtoolsMapName(AbstractCreature acrit)
        {
            return "PS";
        }
        public override Color DevtoolsMapColor(AbstractCreature acrit)
        {
            return new Color(.1f, .1f, 1f);
        }

        public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction()
        {
            return new RoomAttractivenessPanel.Category[]
            {
                RoomAttractivenessPanel.Category.LikesInside
            };
        }

        public override CreatureTemplate.Type ArenaFallback()
        {
            return CreatureTemplate.Type.SpitterSpider;
        }
        public override void EstablishRelationships()
        {
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.SpitterSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.BigSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.LizardTemplate, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.3f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.BlueLizard, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.GreenLizard, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.RedLizard, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.Vulture, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.LanternMouse, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.8f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.Centipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.11f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.SmallCentipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.2f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.VultureGrub, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.1f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.Scavenger, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.45f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.TentaclePlant, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.8f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.PoleMimic, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.3f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.Centiwing, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.CicadaA, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.3f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.CicadaB, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.3f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.BigEel, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.BigNeedleWorm, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.SmallNeedleWorm, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.3f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.DropBug, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.25f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.MirosBird, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.9f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, CreatureTemplate.Type.RedCentipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.8f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, MoreSlugcatsEnums.CreatureTemplateType.SpitLizard, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.4f));
            StaticWorld.EstablishRelationship(WandererModEnum.Creatures.ToxicSpider, MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));

            StaticWorld.EstablishRelationship(CreatureTemplate.Type.SpitterSpider, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BigSpider, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.LizardTemplate, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.35f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.GreenLizard, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BlueLizard, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.LanternMouse, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.6f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Vulture, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.CicadaA, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Leech, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.3f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Spider, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Slugcat, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.MirosBird, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Scavenger, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.45f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.SpitLizard, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.6f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, WandererModEnum.Creatures.ToxicSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));

        }
        public override IEnumerable<string> WorldFileAliases()
        {
            return new string[]
            {
                "toxicspider",
                "toxic spider",
                "ToxicSpider"
            };
        }

        public override ItemProperties Properties(Creature crit)
        {
            BigSpider spider = crit as BigSpider;
            ItemProperties result = null;
            if (spider != null)
            {
                result = new ToxicSpiderProperties(spider);
            }
            return result;
        }
    }



    internal sealed class ToxicSpiderProperties : ItemProperties
    {

        public ToxicSpiderProperties(BigSpider spider)
        {
            this.spider = spider;
        }

        private readonly BigSpider spider;
    }


}
