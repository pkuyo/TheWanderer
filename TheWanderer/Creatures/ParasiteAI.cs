using Noise;
using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.Creatures
{   
    class ParasiteAI : ArtificialIntelligence, IUseARelationshipTracker, IAINoiseReaction
    {

        public ParasiteAI(AbstractCreature creature, World world) : base(creature, world)
        {
            parasite = creature.realizedCreature as Parasite;
            parasite.AI = this;
            AddModule(new StandardPather(this, world, creature));
            pathFinder.stepsPerFrame = 15;
            pathFinder.accessibilityStepsPerFrame = 15;


            AddModule(new Tracker(this, 10, 10, 1500, 0.5f, 5, 5, 10));
            AddModule(new NoiseTracker(this, tracker));
            AddModule(new ThreatTracker(this, 3));
            AddModule(new PreyTracker(this, 5, 2f, 10f, 70f, 0.5f));
            AddModule(new RainTracker(this));
            AddModule(new DenFinder(this, creature));
            AddModule(new StuckTracker(this, true, false));
            AddModule(new NoiseTracker(this, tracker));
            AddModule(new UtilityComparer(this));
            AddModule(new RelationshipTracker(this, tracker));
            FloatTweener.FloatTween smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.5f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.005f));
            utilityComparer.AddComparedModule(threatTracker, smoother, 1f, 1.1f);
            smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.2f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.01f));
            utilityComparer.AddComparedModule(preyTracker, smoother, 0.65f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 0.4f, 1.1f);
            utilityComparer.AddComparedModule(noiseTracker, null, 0.3f, 1.2f);
            behavior = Behavior.Idle;
            noiseTracker.hearingSkill = 0.5f;
            AddModule(hiveMindAI = new HiveModule(this));
        }

        public override bool TrackerToDiscardDeadCreature(AbstractCreature crit)
        {
            return true;
        }
        public override void Update()
        {
            base.Update();
            /////这是debug用的!!!
            if (relationshipTracker.visualize && (!creature.state.alive || creature.Room == null))
                relationshipTracker.viz.ClearAll();
          
            if (creature.Room == null)
                return;

            if(parasite.sitting)
                noiseTracker.hearingSkill = 1.0f;
            else
                noiseTracker.hearingSkill = 0.5f;

            if (ModManager.MSC && parasite.LickedByPlayer != null)
                tracker.SeeCreature(parasite.LickedByPlayer.abstractCreature);

            if (parasite.dead && hiveMindAI != null)
            {
                hiveMindAI.Die();
                modules.Remove(hiveMindAI);
                hiveMindAI = null;
            }

            if (preyTracker.MostAttractivePrey != null)
                utilityComparer.GetUtilityTracker(preyTracker).weight = Custom.LerpMap(creature.pos.Tile.FloatDist(preyTracker.MostAttractivePrey.BestGuessForPosition().Tile), 26f, 36f, 1f, 0.1f);

            AIModule aimodule = utilityComparer.HighestUtilityModule();

            var currentUtility = utilityComparer.HighestUtility();
            if (aimodule != null)
            {
                if (aimodule is ThreatTracker)
                    behavior = Behavior.Flee;
                else if (aimodule is PreyTracker)
                    behavior = Behavior.Hunt;
                else if (aimodule is RainTracker)
                    behavior = Behavior.EscapeRain;
                else if (aimodule is StuckTracker)
                    behavior = Behavior.GetUnstuck;
                else if (aimodule is NoiseTracker)
                    behavior = Behavior.InvestigateSound;
            }

            if (currentUtility < 0.05f)
                behavior = Behavior.Idle;
            if (behavior != Behavior.Flee && parasite.grasps[0] != null)
            {
                if (parasite.isFemale)
                {
                    var bodyDir = Custom.DirVec(parasite.bodyChunks[0].pos, parasite.bodyChunks[1].pos);
                    if (parasite.infectingCounter > 0 ||
                        (Mathf.Abs(Custom.VecToDeg(bodyDir) - Custom.VecToDeg(Custom.DirVec(parasite.bodyChunks[1].pos, parasite.bodyChunks[2].pos))) < 40
                        && parasite.room.aimap.getAItile(parasite.abstractCreature.pos).acc == AItile.Accessibility.Floor))
                    {
                        //到了地面，平行于地面，身体没有折叠的情况下
                        behavior = Behavior.Infect;
                        SetDestination(parasite.abstractCreature.pos);
                    }
                    else
                    {
                        behavior = Behavior.Idle;
                        if (Custom.DistLess(idlePos, parasite.abstractCreature.pos, 4))
                            idlePosCounter = 0;
                    }


                }
                else
                {

                    if (parasite.room.aimap.getAItile(parasite.abstractCreature.pos).acc == AItile.Accessibility.Floor)
                    {
                        SetDestination(parasite.abstractCreature.pos);
                        behavior = Behavior.Eat;
                        //如果到了地面则直接吃
                    }
                    else
                    {
                        behavior = Behavior.Idle;
                        if (Custom.DistLess(idlePos, parasite.abstractCreature.pos, 4))
                            idlePosCounter = 0;
                    }
                }
            }
            if (behavior == Behavior.Hunt &&
            (preyTracker.MostAttractivePrey == null || (preyTracker.MostAttractivePrey.dynamicRelationship.state as ParasiteTrackState).parasited))
                behavior = Behavior.Idle;

            if (behavior != Behavior.Idle)
            {
                tempIdlePos = creature.pos;
            }
            if (behavior == Behavior.GetUnstuck)
            {
                parasite.runSpeed = 1f;
                if (Random.value < 0.006666667f)
                    creature.abstractAI.SetDestination(parasite.room.GetWorldCoordinate(parasite.mainBodyChunk.pos + Custom.RNV() * 100f));
            }
            else if (behavior == Behavior.Idle)
            {
                if (idleTowardsPosition != null)
                {
                    creature.abstractAI.SetDestination(idleTowardsPosition.Value);
                    if (Random.value < 0.002f || Custom.ManhattanDistance(creature.pos, idleTowardsPosition.Value) < 4)
                    {
                       idleTowardsPosition = null;
                    }
                }
                else if (!creature.abstractAI.WantToMigrate)
                {
                    int attemptTimes = 0;
                    if(parasite.isFemale && parasite.room.aimap.getAItile(pathFinder.GetDestination).acc != AItile.Accessibility.Floor)
                    {
                        WorldCoordinate coord2 = new WorldCoordinate(parasite.room.abstractRoom.index, Random.Range(0, parasite.room.TileWidth), Random.Range(0, parasite.room.TileHeight), -1);
                        while (attemptTimes < 10)
                        {
                            attemptTimes++;
                            if (parasite.room.aimap.getAItile(coord2).acc == AItile.Accessibility.Floor)
                            {
                                idlePos = coord2;
                                break;
                            }
                        }
                       
                    }
                    WorldCoordinate coord = new WorldCoordinate(parasite.room.abstractRoom.index, Random.Range(0, parasite.room.TileWidth), Random.Range(0, parasite.room.TileHeight), -1);
                    if (IdleScore(coord) < IdleScore(tempIdlePos))
                    {
                        tempIdlePos = coord;
                    }
                    SetDestination(idlePos);
                    if (IdleScore(tempIdlePos) <IdleScore(pathFinder.GetDestination) + Custom.LerpMap(idlePosCounter, 0f, 300f, 100f, -300f))
                    {
    
                        idlePos = tempIdlePos;
                        idlePosCounter = Random.Range(600, 2000);
                        tempIdlePos = new WorldCoordinate(parasite.room.abstractRoom.index, Random.Range(0, parasite.room.TileWidth), Random.Range(0, parasite.room.TileHeight), -1);
                    }
                    idlePosCounter--;
                }
                else if (pathFinder.GetDestination != creature.abstractAI.MigrationDestination)
                {
                    creature.abstractAI.SetDestination(creature.abstractAI.MigrationDestination);
                }

            }
            else if (behavior == Behavior.Hunt)
            {
                parasite.runSpeed = Custom.LerpAndTick(parasite.runSpeed, 1f, 0.01f, 0.1f);
                creature.abstractAI.SetDestination(preyTracker.MostAttractivePrey.BestGuessForPosition());
                
                //male攻击
                if (parasite.isMale && parasite.grasps[0] == null && !parasite.jumping && parasite.attemptBite == 0f && parasite.charging == 0f && parasite.Footing && preyTracker.MostAttractivePrey.representedCreature.realizedCreature.room == parasite.room && parasite.CheckCanGrab(preyTracker.MostAttractivePrey.representedCreature.realizedCreature))
				{
					BodyChunk bodyChunk = preyTracker.MostAttractivePrey.representedCreature.realizedCreature.bodyChunks[Random.Range(0, preyTracker.MostAttractivePrey.representedCreature.realizedCreature.bodyChunks.Length)];
					if (Custom.DistLess(parasite.mainBodyChunk.pos, bodyChunk.pos, 120f) && (parasite.room.aimap.TileAccessibleToCreature(parasite.room.GetTilePosition(parasite.bodyChunks[1].pos - Custom.DirVec(parasite.bodyChunks[1].pos, bodyChunk.pos) * 30f), parasite.Template) || parasite.room.GetTile(parasite.bodyChunks[1].pos - Custom.DirVec(parasite.bodyChunks[1].pos, bodyChunk.pos) * 30f).Solid) && parasite.room.VisualContact(parasite.mainBodyChunk.pos, bodyChunk.pos))
					{
						if (Vector2.Dot((parasite.mainBodyChunk.pos - bodyChunk.pos).normalized, (parasite.bodyChunks[1].pos - parasite.mainBodyChunk.pos).normalized) > 0.2f)
						{
							parasite.InitiateJump(bodyChunk);
						}
						else
						{
							parasite.mainBodyChunk.vel += Custom.DirVec(parasite.mainBodyChunk.pos, bodyChunk.pos) * 2f;
							parasite.bodyChunks[1].vel -= Custom.DirVec(parasite.mainBodyChunk.pos, bodyChunk.pos) * 2f;
						}
					}
				}
            }
            else if (behavior == Behavior.Flee)
            {
                parasite.runSpeed = Custom.LerpAndTick(parasite.runSpeed, 1f, 0.01f, 0.1f);
                creature.abstractAI.SetDestination(threatTracker.FleeTo(creature.pos, 6, 30, true));
            }
            else if (behavior == Behavior.EscapeRain)
            {
                parasite.runSpeed = Custom.LerpAndTick(parasite.runSpeed, 1f, 0.01f, 0.1f);
                if (denFinder.GetDenPosition() != null)
                    creature.abstractAI.SetDestination(denFinder.GetDenPosition().Value);
            }else if (behavior == Behavior.InvestigateSound)
            {
                parasite.abstractCreature.abstractAI.SetDestination(noiseTracker.ExaminePos);
                parasite.runSpeed = Mathf.Lerp(parasite.runSpeed, 0.6f, 0.2f);
            }
        }

        public void CollideWithKin(Parasite otherBug)
        {
            //if (Custom.ManhattanDistance(creature.pos, otherBug.AI.pathFinder.GetDestination) < 4)
            //{
            //    return;
            //}
            //if ((otherBug.abstractCreature.personality.dominance > parasite.abstractCreature.personality.dominance && !otherBug.sitting) || parasite.sitting || otherBug.AI.pathFinder.GetDestination.room != otherBug.room.abstractRoom.index)
            //{
            //    idleTowardsPosition = new WorldCoordinate?(otherBug.AI.pathFinder.GetDestination);
            //}
        }
        public override PathCost TravelPreference(MovementConnection connection, PathCost cost)
        {
            cost.resistance += Mathf.Max(0f, threatTracker.ThreatOfTile(connection.destinationCoord, true) - threatTracker.ThreatOfTile(creature.pos, true)) * 40f;
            cost = hiveMindAI.TravelPreference(connection,cost);
            return cost;
        }

        public float IdleScore(WorldCoordinate coord)
        {
            if (coord.room != creature.pos.room || !pathFinder.CoordinateReachableAndGetbackable(coord))
                return float.MaxValue;
            
            float num = 1f;
            if (parasite.room.aimap.getAItile(coord).narrowSpace)
                num += 300f;
            
            if (parasite.room.GetTile(coord).AnyWater)
                return float.MaxValue;

            if (parasite.room.aimap.getAItile(coord.Tile).acc == AItile.Accessibility.Ceiling)
                num += 300f;
            if (parasite.room.aimap.getAItile(coord.Tile).acc == AItile.Accessibility.Climb)
                num += 400f;
            if (parasite.room.aimap.getAItile(coord.Tile).acc == AItile.Accessibility.Floor)
                num -= 1000f;
            
            num += Mathf.Max(0f, creature.pos.Tile.FloatDist(coord.Tile) - 80f) / 2f;
            num += parasite.room.aimap.getAItile(coord).visibility / 800f;
            num -= Mathf.Max(parasite.room.aimap.getAItile(coord).smoothedFloorAltitude, 16f) * 2f;

            return num;
        }

        public override Tracker.CreatureRepresentation CreateTrackerRepresentationForCreature(AbstractCreature otherCreature)
        {
            Tracker.CreatureRepresentation result;
            if (otherCreature.creatureTemplate.smallCreature)
            {
                result = new Tracker.SimpleCreatureRepresentation(tracker, otherCreature, 0f, false);
            }
            else
            {
                result = new Tracker.ElaborateCreatureRepresentation(tracker, otherCreature, 1f, 3);
            }
            return result;
        }

        public AIModule ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
                return threatTracker;
            else if (relationship.type == CreatureTemplate.Relationship.Type.Eats || relationship.type == CreatureTemplate.Relationship.Type.Attacks)
                return preyTracker;
            return null;
        }

        

        public CreatureTemplate.Relationship UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            dRelation.state.alive = dRelation.trackerRep.representedCreature.state.alive;
            if (dRelation.trackerRep.VisualContact)
            {
                if (dRelation.trackerRep.representedCreature.realizedCreature != null)
                {
                    if (ParasiteHook.Instance().parasiteData.TryGetValue(dRelation.trackerRep.representedCreature, out var data))
                        (dRelation.state as ParasiteTrackState).parasited = data.isParasite;

                    if ((dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat || dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger))
                    {
                        for (int i = 0; i < dRelation.trackerRep.representedCreature.realizedCreature.grasps.Length; i++)
                        {
                            if (dRelation.trackerRep.representedCreature.realizedCreature.grasps[i] != null && dRelation.trackerRep.representedCreature.realizedCreature.grasps[i].grabbed is Spear)
                            {
                                (dRelation.state as ParasiteTrackState).armed = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!dRelation.state.alive)
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.5f);

            if ((dRelation.state as ParasiteTrackState).parasited && parasite.isFemale)
            {
                if (dRelation.trackerRep.representedCreature.creatureTemplate.CreatureRelationship(parasite.Template).type == CreatureTemplate.Relationship.Type.Eats)
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.3f);
                else
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Uncomfortable, 0.5f);
            }

            CreatureTemplate.Relationship relationship = StaticRelationship(dRelation.trackerRep.representedCreature);
            if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
                if (!dRelation.state.alive)
                    relationship.intensity = 0f;

            if (parasite.isFemale && relationship.type == CreatureTemplate.Relationship.Type.Eats)
            {
                relationship.type = CreatureTemplate.Relationship.Type.Attacks;
                relationship.intensity *= 1.5f;
            }
            else if (parasite.isChild)
            {
                if (relationship.type == CreatureTemplate.Relationship.Type.Eats && (relationship.intensity < 0.4 || (dRelation.state as ParasiteTrackState).armed))
                    relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f);
            }
            relationship.intensity *= dRelation.trackerRep.VisualContact ? 1f : 0.3f;
            return relationship;

        }

        public RelationshipTracker.TrackedCreatureState CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            return new ParasiteTrackState();
        }

        public void ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
        {
            //TODO : 这是动作相关
        }

        static public bool CheckLeader(AbstractCreature origin, AbstractCreature member)
        {
            if (GetTypeIndex(origin.creatureTemplate.type) == GetTypeIndex(member.creatureTemplate.type))
            {
                return origin.personality.dominance > member.personality.dominance;
            }
            return GetTypeIndex(origin.creatureTemplate.type) > GetTypeIndex(member.creatureTemplate.type);

        }

        static int GetTypeIndex(CreatureTemplate.Type type)
        {
            if (type == WandererEnum.Creatures.MaleParasite)
                return 3;
            else if (type == WandererEnum.Creatures.FemaleParasite)
                return 2;
            else if (type == WandererEnum.Creatures.ChildParasite)
                return 1;
            return 0;
        }

        public Behavior behavior;

        public Parasite parasite;

        int idlePosCounter;

        WorldCoordinate tempIdlePos;

        WorldCoordinate idlePos;

        WorldCoordinate? idleTowardsPosition;

        public class Behavior : ExtEnum<Behavior>
        {
            public Behavior(string value, bool register = false) : base(value, register)
            {
            }

            public static readonly Behavior Idle = new Behavior("Idle", true);

            public static readonly Behavior Flee = new Behavior("Flee", true);

            public static readonly Behavior Hunt = new Behavior("Hunt", true);

            public static readonly Behavior Infect = new Behavior("Infect", true);

            public static readonly Behavior Eat = new Behavior("Eat", true);

            public static readonly Behavior InvestigateSound = new Behavior("InvestigateSound", true);

            public static readonly Behavior EscapeRain = new Behavior("EscapeRain", true);

            public static readonly Behavior GetUnstuck = new Behavior("GetUnstuck", true);

        }

        public class ParasiteTrackState : RelationshipTracker.TrackedCreatureState
        {
            public bool parasited;

            public bool armed;
        }

        public HiveModule hiveMindAI;

    }

    class HiveModule : AIModule
    {
        public HiveModule(ArtificialIntelligence AI) : base(AI)
        {
            queue = new MindQueue(this.AI.creature);
        }

        public void Die()
        {
            queue.Clear();
            queue = null;
        }

        public PathCost TravelPreference(MovementConnection connection, PathCost cost)
        {
            if (parasite == null || parasite.slatedForDeletetion || parasite.room == null || parasite.AI == null)
            {
                return cost;
            }
            if (parasite.AI.behavior == ParasiteAI.Behavior.Hunt)
            {
                foreach (var member in queue.MemberInRoom)
                {
                    Vector2 a = parasite.room.MiddleOfTile(connection.destinationCoord);
                    if (member == null || member.slatedForDeletion)
                    {
                        return cost;
                    }
                    if (member != parasite.abstractCreature && member.realizedCreature != null && !ParasiteAI.CheckLeader(parasite.abstractCreature,member))
                    {
                        cost.resistance += Mathf.InverseLerp(300f, 0f, Vector2.Distance(a, member.realizedCreature.mainBodyChunk.pos)) * 200f / (queue.MemberInRoom.Count - 1);
                        if (connection.destinationCoord.Tile == member.pos.Tile)
                        {
                            cost.resistance += 300f / (queue.MemberInRoom.Count - 1);
                        }
                    }
                }
            }
            return cost;
        }

        public override void Update()
        {
            base.Update();
            queue.Update();
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
            if (room == null)
                return;
            WandererMod.Log("HiveModule : Enter new room");
            if (!AllMinds.TryGetValue(room.abstractRoom, out var mind) || mind == null)
                AllMinds.Add(room.abstractRoom,mind = new RoomMind(room.abstractRoom));
            queue.PushNewMind(mind);
        }

        MindQueue queue;

        public Parasite parasite;


        public int leaveGroupCounter;

        static ConditionalWeakTable<AbstractRoom, RoomMind> AllMinds { get; } = new ConditionalWeakTable<AbstractRoom, RoomMind>();

        class MindQueue
        {
            List<RoomMind> minds;
            List<int> mindCountRemain;
            AbstractCreature creature;
            public MindQueue(AbstractCreature creature)
            {
                minds = new List<RoomMind>();
                mindCountRemain = new List<int>();
                this.creature = creature;
            }

            public void Clear()
            {
                foreach(var mind in minds)
                    mind.RemoveMember(creature);
                minds.Clear();
                mindCountRemain.Clear();
                creature = null;
            }
            public void PushNewMind(RoomMind newMind)
            {
                if (minds.Contains(newMind))
                    mindCountRemain.RemoveAt(minds.FindIndex(i => i == newMind));

                newMind.AddNewMember(creature);
                minds.Add(newMind);
                mindCountRemain.Add(600);
            }
            
            public void Update()
            {
                //忘记之前的房间群组
                for (int i = 0; i < mindCountRemain.Count - 1; i++)
                {
                    if (--mindCountRemain[i] == 0)
                    {
                        WandererMod.Log("HiveModule : " + creature.ID + " remove old hive " + minds[i].ToString());
                        minds[i].RemoveMember(creature);
                        minds.RemoveAt(i);
                        mindCountRemain.RemoveAt(i);
                        i--;
                        
                    }
                }

                HashSet<Parasite> set = new HashSet<Parasite>();
                foreach (var mind in  minds)
                {
                    foreach(var creature in mind.list)
                    {
                        if (creature.realizedCreature != null)
                            set.Add(creature.realizedCreature as Parasite);
                    }
                }
                foreach (var creature in set)
                {
                    foreach (var rep in creature.AI.tracker.creatures)
                    {
                        OtherParasiteSeeCreature(creature, rep);
                    }
                }

            }

            public List<AbstractCreature> MemberInRoom => minds[minds.Count - 1].list;

            void OtherParasiteSeeCreature(Parasite parasite, Tracker.CreatureRepresentation rep)
            {
                if (parasite.abstractCreature == creature || rep.representedCreature.realizedCreature == null || !rep.VisualContact  || !parasite.CheckCanGrab(rep.representedCreature.realizedCreature))
                    return;

                Tracker.CreatureRepresentation creatureRepresentation = creature.abstractAI.RealAI.tracker.RepresentationForObject(rep.representedCreature.realizedCreature, false);

                if (creatureRepresentation == null)
                {
                    creature.abstractAI.RealAI.tracker.SeeCreature(rep.representedCreature);
                }
                else if (creatureRepresentation.VisualContact)
                {
                    return;
                }
                else
                {
                    for (int i = 0; i < rep.representedCreature.realizedCreature.bodyChunks.Length; i++)
                    {
                        if (parasite.AI.VisualContact(rep.representedCreature.realizedCreature.bodyChunks[i]))
                        {
                            creature.abstractAI.RealAI.tracker.SeeCreature(rep.representedCreature);
                            return;
                        }
                    }
                }
                
            }

        }

        class RoomMind
        {
            AbstractRoom room;
            public List<AbstractCreature> list;

            public void AddNewMember(AbstractCreature creature)
            {
                if(!list.Contains(creature))
                    list.Add(creature);
            }
            public bool RemoveMember(AbstractCreature creature)
            {
                if (list.Contains(creature))
                    list.Remove(creature);

                if (list.Count == 0)
                    return true;
                return false;
            }

            public override string ToString()
            {
                return room.name + " [" + list.Count + "]";
            }

            public RoomMind(AbstractRoom room)
            {
                this.room = room;
                list = new List<AbstractCreature>();
                WandererMod.Log("Creature new Room Mind in" + room.name);
            }
        }
    }


}
