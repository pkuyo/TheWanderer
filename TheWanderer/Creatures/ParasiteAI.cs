using Noise;
using RWCustom;
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
            //relationshipTracker.visualize = true;
            FloatTweener.FloatTween smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.5f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.005f));
            utilityComparer.AddComparedModule(threatTracker, smoother, 1f, 1.1f);
            smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.2f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.01f));
            utilityComparer.AddComparedModule(preyTracker, smoother, 0.65f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 0.4f, 1.1f);
            behavior = Behavior.Idle;
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

       

            if (ModManager.MSC && parasite.LickedByPlayer != null)
                tracker.SeeCreature(parasite.LickedByPlayer.abstractCreature);

          
            if(preyTracker.MostAttractivePrey != null)
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
            }

            if (currentUtility < 0.05f)
                behavior = Behavior.Idle;

            if (behavior != Behavior.Flee && parasite.grasps[0] != null)
            {
                if (parasite.isFemale)
                    behavior = Behavior.Infect;

                if (parasite.room.aimap.getAItile(parasite.abstractCreature.pos).acc == AItile.Accessibility.Floor && !parasite.isFemale)
                {
                    SetDestination(parasite.abstractCreature.pos);
                    behavior = Behavior.Eat;
                    //如果到了地面则直接吃
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
                                SetDestination(coord2);
                                break;
                            }
                        }
                       
                    }
                    WorldCoordinate coord = new WorldCoordinate(parasite.room.abstractRoom.index, Random.Range(0, parasite.room.TileWidth), Random.Range(0, parasite.room.TileHeight), -1);
                    if (IdleScore(coord) < IdleScore(tempIdlePos))
                    {
                        tempIdlePos = coord;
                    }
                    if (IdleScore(tempIdlePos) <IdleScore(pathFinder.GetDestination) + Custom.LerpMap(idlePosCounter, 0f, 300f, 100f, -300f))
                    {
                        if (parasite.isMale || (parasite.isFemale && parasite.room.aimap.getAItile(tempIdlePos).acc != AItile.Accessibility.Floor))
                        {
                            SetDestination(tempIdlePos);
                        }
                        idlePosCounter = Random.Range(800, 3200);
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
                if (parasite.isMale && parasite.grasps[0] == null && !parasite.jumping && parasite.attemptBite == 0f && parasite.charging == 0f && parasite.Footing && preyTracker.MostAttractivePrey.representedCreature.realizedCreature.room == parasite.room)
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
            }

        }

        public void CollideWithKin(Parasite otherBug)
        {
            if (Custom.ManhattanDistance(creature.pos, otherBug.AI.pathFinder.GetDestination) < 4)
            {
                return;
            }
            if ((otherBug.abstractCreature.personality.dominance > parasite.abstractCreature.personality.dominance && !otherBug.sitting) || parasite.sitting || otherBug.AI.pathFinder.GetDestination.room != otherBug.room.abstractRoom.index)
            {
                idleTowardsPosition = new WorldCoordinate?(otherBug.AI.pathFinder.GetDestination);
            }
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
                    if (ParasiteHook.Instance().parasiteData.TryGetValue(dRelation.trackerRep.representedCreature.realizedCreature, out var data))
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



                if (!dRelation.state.alive)
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.5f);

                if ((dRelation.state as ParasiteTrackState).parasited)
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Uncomfortable, 0.5f);

                CreatureTemplate.Relationship relationship = StaticRelationship(dRelation.trackerRep.representedCreature);
                if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
                    if (!dRelation.state.alive)
                        relationship.intensity = 0f;

                if (parasite.isFemale && relationship.type == CreatureTemplate.Relationship.Type.Eats)
                {
                    relationship.type = CreatureTemplate.Relationship.Type.Attacks;
                    relationship.intensity *= 1.5f;
                }
                else if(parasite.isChild)
                {
                    if (relationship.type == CreatureTemplate.Relationship.Type.Eats && (relationship.intensity < 0.4 || (dRelation.state as ParasiteTrackState).armed))
                        relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f);
                }

                return relationship;
            }
            return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.0f);
        }

        public RelationshipTracker.TrackedCreatureState CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            return new ParasiteTrackState();
        }

        public void ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
        {
            //TODO : 这是动作相关
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

            public static readonly Behavior EscapeRain = new Behavior("EscapeRain", true);

            public static readonly Behavior GetUnstuck = new Behavior("GetUnstuck", true);

        }

        public class ParasiteTrackState : RelationshipTracker.TrackedCreatureState
        {
            public bool parasited;

            public bool armed;
        }

    }


}
