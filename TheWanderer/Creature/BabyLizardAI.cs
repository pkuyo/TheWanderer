using Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Creatures
{
    public class BabyLizardAI : ArtificialIntelligence,
        IUseARelationshipTracker, IAINoiseReaction, IReactToSocialEvents, FriendTracker.IHaveFriendTracker
    {
        public BabyLizardAI(AbstractCreature creature, World world) : base(creature, world)
        {
        }

        public override Tracker.CreatureRepresentation CreateTrackerRepresentationForCreature(AbstractCreature otherCreature)
        {
            return base.CreateTrackerRepresentationForCreature(otherCreature);
        }

        public override void CreatureSpotted(bool firstSpot, Tracker.CreatureRepresentation otherCreature)
        {
            base.CreatureSpotted(firstSpot, otherCreature);
        }

        public override float CurrentPlayerAggression(AbstractCreature player)
        {
            return base.CurrentPlayerAggression(player);
        }

        public override void HeardNoise(InGameNoise noise)
        {
            base.HeardNoise(noise);
        }

        public override void NewArea(bool strandedFromExits)
        {
            base.NewArea(strandedFromExits);
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
        }

        public override bool TrackerToDiscardDeadCreature(AbstractCreature crit)
        {
            return base.TrackerToDiscardDeadCreature(crit);
        }

        public override PathCost TravelPreference(MovementConnection coord, PathCost cost)
        {
            return base.TravelPreference(coord, cost);
        }

        public override void Update()
        {
            base.Update();
        }

        public override float VisualScore(Vector2 lookAtPoint, float bonus)
        {
            return base.VisualScore(lookAtPoint, bonus);
        }

        public override bool WantToStayInDenUntilEndOfCycle()
        {
            return base.WantToStayInDenUntilEndOfCycle();
        }

        RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            throw new NotImplementedException();
        }

        void FriendTracker.IHaveFriendTracker.GiftRecieved(SocialEventRecognizer.OwnedItemOnGround gift)
        {
            throw new NotImplementedException();
        }

        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            throw new NotImplementedException();
        }

        void IAINoiseReaction.ReactToNoise(NoiseTracker.TheorizedSource source, InGameNoise noise)
        {
            throw new NotImplementedException();
        }

        void IReactToSocialEvents.SocialEvent(SocialEventRecognizer.EventID ID, global::Creature subjectCrit, global::Creature objectCrit, PhysicalObject involvedItem)
        {
            throw new NotImplementedException();
        }

        CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            throw new NotImplementedException();
        }
    }

    public class BabyLizardPather : PathFinder
    {
        public BabyLizardPather(ArtificialIntelligence AI, World world, AbstractCreature creature) : base(AI, world, creature)
        {

        }

        public override PathCost CheckConnectionCost(PathFinder.PathingCell start, PathFinder.PathingCell goal, MovementConnection connection, bool followingPath)
        {
            return base.CheckConnectionCost(start, goal, connection, followingPath);
        }


    }
}
