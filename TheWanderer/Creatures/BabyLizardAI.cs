//using Noise;
//using RWCustom;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Random = UnityEngine.Random;

//namespace Pkuyo.Wanderer.Creatures
//{
//    public class BabyLizardAI : ArtificialIntelligence,
//        IUseARelationshipTracker, IAINoiseReaction, IReactToSocialEvents, FriendTracker.IHaveFriendTracker
//    {

//        public class BehaviorType : ExtEnum<BehaviorType>
//        {
//            public BehaviorType(string value, bool register = false) : base(value, register)
//            {
//            }

//            public static readonly BehaviorType Idle = new BehaviorType("Idle", true);

//            public static readonly BehaviorType Fleeing = new BehaviorType("Fleeing", true);

//            public static readonly BehaviorType Following = new BehaviorType("Following", true);

//            public static readonly BehaviorType Hunting = new BehaviorType("Hunting", true);

//            public static readonly BehaviorType Attacking = new BehaviorType("Attacking", true);

//            public static readonly BehaviorType BeingHeld = new BehaviorType("BeingHeld", true);

//            public static readonly BehaviorType Seeking = new BehaviorType("Seeking", true);

//            public static readonly BehaviorType Playing = new BehaviorType("Playing", true);

//            public static readonly BehaviorType ZeroG = new BehaviorType("ZeroG", true);

//        }

//        public class
//        public BabyLizardAI(AbstractCreature creature, World world) : base(creature, world)
//        {
//            AddModule(new Tracker(this, 10, 10, -1, 0.35f, 5, 5, 10));
//            AddModule(new NoiseTracker(this, tracker));
//            AddModule(new RainTracker(this));
//            AddModule(new PreyTracker(this, 3, 2, 3, 50, 0.5f));
//            AddModule(new ThreatTracker(this, 5));
//            AddModule(new AgressionTracker(this, 0.001f, 0.001f));
//            //AddModule(new MissionTracker(this));
//            AddModule(new RelationshipTracker(this, tracker));

//            AddModule(new StuckTracker(this, true, true)); //?
//            stuckTracker.totalTrackedLastPositions = 20;
//            stuckTracker.checkPastPositionsFrom = 7;
//            stuckTracker.pastPosStuckDistance = 2;
//            stuckTracker.pastStuckPositionsCloseToIncrementStuckCounter = 4;
//            stuckTracker.AddSubModule(new StuckTracker.MoveBacklog(stuckTracker));

//            AddModule(new FriendTracker(this));
//            friendTracker.tamingDifficlty = 0.5f;
//            friendTracker.desiredCloseness = Mathf.Lerp(2f, 8f, (1f - this.creature.personality.nervous) * 0.5f + this.creature.personality.dominance * 0.5f);

//            //CommuicateTracker

//            AddModule(new UtilityComparer(this));

//            FloatTweener.FloatTween smoother = new FloatTweener.FloatTweenUpAndDown(new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.5f), new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.0025f));
//            utilityComparer.AddComparedModule(threatTracker, smoother, 1f, 1f);
//            smoother = new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.15f);
//            utilityComparer.AddComparedModule(preyTracker, smoother, 0.6f, 1f);
//            utilityComparer.AddComparedModule(rainTracker, null, 0.9f, 1f);
//            //TODO : Injury Tracker
//            utilityComparer.AddComparedModule(agressionTracker, null, 0.5f, 1.2f);
//            utilityComparer.AddComparedModule(missionTracker, null, 0.8f, 1f);
//            utilityComparer.AddComparedModule(noiseTracker, null, 0.2f, 1.2f);
//            utilityComparer.AddComparedModule(friendTracker, null, 0.9f, 1.2f);
//        }

//        public override Tracker.CreatureRepresentation CreateTrackerRepresentationForCreature(AbstractCreature otherCreature)
//        {
//            Tracker.CreatureRepresentation result;
//            if (otherCreature.creatureTemplate.smallCreature /*|| TODO:获取同种蜥蜴 */)
//            {
//                result = new Tracker.SimpleCreatureRepresentation(base.tracker, otherCreature, 0f, false);
//            }
//            else
//            {
//                result = new Tracker.ElaborateCreatureRepresentation(base.tracker, otherCreature, 1f, 5);
//            }
//            return result;
//        }

//        public override void CreatureSpotted(bool firstSpot, Tracker.CreatureRepresentation otherCreature)
//        {
//            //TODO : 和画面和生物同步
//            base.CreatureSpotted(firstSpot, otherCreature);
//        }

//        public override float CurrentPlayerAggression(AbstractCreature player)
//        {
//            //TODO : 修改机制
//            Tracker.CreatureRepresentation creatureRepresentation = base.tracker.RepresentationForCreature(player, false);
//            if (creatureRepresentation == null || creatureRepresentation.dynamicRelationship == null)
//            {
//                return 1f;
//            }
//            return Mathf.InverseLerp(0.5f, 0f, LikeOfPlayer(creatureRepresentation));
//        }

//        public float LikeOfPlayer(Tracker.CreatureRepresentation player)
//        {
//            if (player == null)
//            {
//                return 0f;
//            }
//            float like = creature.world.game.session.creatureCommunities.LikeOfPlayer(creature.creatureTemplate.communityID, creature.world.RegionNumber, (player.representedCreature.state as PlayerState).playerNumber);
//            like = Mathf.Lerp(like, -creature.personality.aggression, Mathf.Abs(creature.personality.aggression);

//            float tempLike = creature.state.socialMemory.GetTempLike(player.representedCreature.ID);
//            like = Mathf.Lerp(like, tempLike, Mathf.Abs(tempLike));
//            like += Mathf.Lerp(-0.1f, 0.1f, this.creature.personality.sympathy);

//            for (int i = 0; i < player.representedCreature.realizedCreature.grasps.Length; i++)
//            {
//                if (player.representedCreature.realizedCreature.grasps[i] != null)
//                {
//                    if (like < -0.2f && player.representedCreature.realizedCreature.grasps[i].grabbed is Spear)
//                    {
//                        like -= 0.1f;
//                    }
//                }
//            }


//            if (friendTracker.giftOfferedToMe != null && friendTracker.giftOfferedToMe.owner == player.representedCreature.realizedCreature)
//            {
//                like = Custom.LerpMap(like, -0.5f, 1f, 0f, 1f, 0.8f);
//            }
//            return like;
//        }

//        public override void HeardNoise(InGameNoise noise)
//        {
//            base.HeardNoise(noise);
//        }

//        public override void NewArea(bool strandedFromExits)
//        {
//            base.NewArea(strandedFromExits);
//        }

//        public override void NewRoom(Room room)
//        {
//            base.NewRoom(room);
//        }

//        public override bool TrackerToDiscardDeadCreature(AbstractCreature crit)
//        {
//            return base.TrackerToDiscardDeadCreature(crit);
//        }

//        public override PathCost TravelPreference(MovementConnection connection, PathCost cost)
//        {
//            if (obstacleTracker != null)
//            {
//                int num = obstacleTracker.ObstacleWarning(connection);
//                cost += new PathCost(Mathf.Pow((float)num, 3f) * 5f, PathCost.Legality.Allowed);
//            }
//            if (threatTracker != null && threatTracker.Utility() > 0f)
//            {
//                float f = threatTracker.ThreatOfTile(connection.destinationCoord, true);
//                cost += new PathCost(Mathf.Pow(f, 3f) * 2f * Mathf.Pow(this.fear, 0.5f), PathCost.Legality.Allowed);
//            }
//            //?
//            if (fear > 0.2f && (connection.type == MovementConnection.MovementType.DropToClimb || connection.type == MovementConnection.MovementType.DropToFloor) && base.threatTracker.ThreatOfArea(connection.destinationCoord, true) > base.threatTracker.ThreatOfArea(connection.startCoord, true))
//            {
//                cost += new PathCost(100f, PathCost.Legality.Allowed);
//            }
//            return cost;
//        }

//        public override void Update()
//        {
//            if (panic > 0)
//            {
//                panic--;
//            }
//            timeInRoom++;
//            creature.state.socialMemory.EvenOutAllTemps(0.0002f);

//            pathFinder.walkPastPointOfNoReturn = (stranded || denFinder.GetDenPosition() == null || !pathFinder.CoordinatePossibleToGetBackFrom(denFinder.GetDenPosition().Value));

//            //TODO : 看看飞天蜈蚣
//            fear = Mathf.Pow(utilityComparer.GetSmoothedNonWeightedUtility(threatTracker), Mathf.Lerp(0.2f, 1.8f, creature.personality.bravery));

//            hunger = Custom.LerpMap(Lizard.foodInStomach, 0, Lizard.MaxFood, 0.2f - creature.personality.sympathy*0.3f , 1) * utilityComparer.GetSmoothedNonWeightedUtility(preyTracker);
//            hunger += creature.personality.energy * 0.2f;

//            rainFear = utilityComparer.GetSmoothedNonWeightedUtility(rainTracker);

//            excitement = Mathf.Lerp(excitement, Mathf.Max(hunger, CombinedFear), 0.1f);

//            if (fear > 0.8f)
//            {
//                lastDistressLength = Custom.IntClamp(lastDistressLength + 1, 0, 500);
//            }
//            else if (fear < 0.2f)
//            {
//                lastDistressLength = 0;
//            }
//            ((utilityComparer.GetUtilityTracker(threatTracker).smoother as FloatTweener.FloatTweenUpAndDown).down as FloatTweener.FloatTweenBasic).speed = 1f / ((lastDistressLength + 20) * 3f);
//            utilityComparer.GetUtilityTracker(agressionTracker).weight = (creature.world.game.IsStorySession ? 0.25f : 0.125f) /*乘以血量*/;
//            utilityComparer.GetUtilityTracker(rainTracker).weight = (friendTracker.CareAboutRain() ? 0.9f : 0.1f);

//            behavior = DetermineBehavior();
//        }

//        private BehaviorType DetermineBehavior()
//        {
//            BehaviorType behavior = BehaviorType.Idle;
//            AIModule aimodule = utilityComparer.HighestUtilityModule();
//            if (aimodule != null)
//            {
//                if(aimodule is ThreatTracker)
//                {
//                    behavior = BehaviorType.Fleeing;
//                }
//                else if(aimodule is RainTracker) 
//                {
//                    behavior = BehaviorType.Fleeing;
//                }
//                else if(aimodule is PreyTracker)
//                {
//                    behavior = BehaviorType.Hunting;
//                }
//                else if(aimodule is FriendTracker)
//                {
//                    behavior = BehaviorType.Following;
//                }
//            }
//            throw new NotImplementedException();
//        }
//        public override float VisualScore(Vector2 lookAtPoint, float bonus)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool WantToStayInDenUntilEndOfCycle()
//        {
//            throw new NotImplementedException();
//        }

//        RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
//        {
//            throw new NotImplementedException();
//        }

//        void FriendTracker.IHaveFriendTracker.GiftRecieved(SocialEventRecognizer.OwnedItemOnGround gift)
//        {
//            throw new NotImplementedException();
//        }

//        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
//        {
//            throw new NotImplementedException();
//        }

//        void IAINoiseReaction.ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
//        {
//            throw new NotImplementedException();
//        }

//        void IReactToSocialEvents.SocialEvent(SocialEventRecognizer.EventID ID, global::Creature subjectCrit, global::Creature objectCrit, PhysicalObject involvedItem)
//        {
//            throw new NotImplementedException();
//        }

//        CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
//        {
//            throw new NotImplementedException();
//        }


//        public float CombinedFear
//        {
//            get
//            {
//                return Mathf.Max(fear, Mathf.Pow(rainFear, 3f));
//            }
//        }

//        public BabyLizard Lizard
//        {
//            get =>creature.realizedCreature as BabyLizard;
//        }
//        int timeInRoom;
//        int panic;

//        float fear;
//        float hunger;
//        float rainFear;
//        float excitement;

//        int lastDistressLength;

//        public BehaviorType behavior;

//    }
//}
