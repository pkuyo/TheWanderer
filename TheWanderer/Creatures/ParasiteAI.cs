using JetBrains.Annotations;
using Noise;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.Creatures
{
    class ParasiteAI : ArtificialIntelligence, IUseARelationshipTracker, IAINoiseReaction
    {

        public ParasiteAI(AbstractCreature creature, World world) : base(creature, world)
        {
            parasite = creature.realizedCreature as Parasite;

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
            behavior = Behavior.Idle;
        }

        public override void Update()
        {
            base.Update();
            if (creature.Room == null)
                return;

            if(ModManager.MSC && parasite.LickedByPlayer != null)
                tracker.SeeCreature(parasite.abstractCreature);

            utilityComparer.GetUtilityTracker(preyTracker).weight = Custom.LerpMap(creature.pos.Tile.FloatDist(preyTracker.MostAttractivePrey.BestGuessForPosition().Tile), 26f, 36f, 1f, 0.1f);

            AIModule aimodule = utilityComparer.HighestUtilityModule();

            var currentUtility = utilityComparer.HighestUtility();
            if (aimodule != null)
            {
                if (aimodule is ThreatTracker)
                    behavior = Behavior.Flee;
                else if (aimodule is PreyTracker)
                    behavior =Behavior.Hunt;
                else if (aimodule is RainTracker)
                    behavior = Behavior.EscapeRain;
                else if (aimodule is StuckTracker)
                    behavior = Behavior.GetUnstuck;
            }
            if (currentUtility < 0.05f)
               behavior = Behavior.Idle;

            if (behavior == Behavior.GetUnstuck)
            {
                parasite.runSpeed = 1f;
                if (Random.value < 0.006666667f)
                    creature.abstractAI.SetDestination(parasite.room.GetWorldCoordinate(parasite.mainBodyChunk.pos + Custom.RNV() * 100f));
            }
            else if (behavior == Behavior.Idle)
            {

            }
            else if (behavior == Behavior.Hunt)
            {
                parasite.runSpeed = Custom.LerpAndTick(parasite.runSpeed, 1f, 0.01f, 0.1f);
            }
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
            if (dRelation.trackerRep.VisualContact)
            {
                CreatureTemplate.Relationship relationship = StaticRelationship(dRelation.trackerRep.representedCreature);
                if ((dRelation.state as ParasiteTrackState).parasited)
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Uncomfortable, 0.5f);

                if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
                    if (!dRelation.state.alive)
                        relationship.intensity = 0f;

                if (!parasite.isMale || relationship.type == CreatureTemplate.Relationship.Type.Eats)
                {
                    relationship.type = CreatureTemplate.Relationship.Type.Attacks;
                    relationship.intensity *= 1.5f;
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

        public class Behavior : ExtEnum<Behavior>
        {
            public Behavior(string value, bool register = false) : base(value, register)
            {
            }

            public static readonly Behavior Idle = new Behavior("Idle", true);

            public static readonly Behavior Flee = new Behavior("Flee", true);

            public static readonly Behavior Hunt = new Behavior("Hunt", true);

            public static readonly Behavior EscapeRain = new Behavior("EscapeRain", true);

            public static readonly Behavior GetUnstuck = new Behavior("GetUnstuck", true);

        }

        public class ParasiteTrackState : RelationshipTracker.TrackedCreatureState
        {
            public bool parasited;
        }

    }


}
