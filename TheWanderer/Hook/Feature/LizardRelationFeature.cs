using BepInEx.Logging;
using RWCustom;
using UnityEngine;

namespace Pkuyo.Wanderer.Feature
{
    class LizardRelationFeature : HookBase
    {
        LizardRelationFeature(ManualLogSource log) : base(log)
        {
        }

        static public LizardRelationFeature Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new LizardRelationFeature(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.CreatureCommunities.InfluenceLikeOfPlayer += CreatureCommunities_InfluenceLikeOfPlayer;

            On.LizardAI.ctor += LizardAI_ctor;
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship;

            On.SharedPhysics.TraceProjectileAgainstBodyChunks += SharedPhysics_TraceProjectileAgainstBodyChunks;

            _log.LogDebug("LizardRelationFeature Init");
        }

        private SharedPhysics.CollisionResult SharedPhysics_TraceProjectileAgainstBodyChunks(On.SharedPhysics.orig_TraceProjectileAgainstBodyChunks orig, SharedPhysics.IProjectileTracer projTracer, Room room, Vector2 lastPos, ref Vector2 pos, float rad, int collisionLayer, PhysicalObject exemptObject, bool hitAppendages)
        {
            //友伤免疫 朋友蜥蜴和好感度为1的地区全部蜥蜴
            var re = orig(projTracer, room, lastPos, ref pos, rad, collisionLayer, exemptObject, hitAppendages);
            if (ChangeLizardFriendFire)
                if (re.obj is Lizard && exemptObject is Player && ((re.obj as Lizard).AI.friendTracker.friend != null || (re.obj as Lizard).abstractCreature.world.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Lizards, (re.obj as Lizard).abstractCreature.world.RegionNumber, (exemptObject as Player).playerState.playerNumber) == 1)
                      && (exemptObject as Player).slugcatStats.name.value == WandererCharacterMod.WandererName)
                    re.obj = null;
            return re;
        }


        private CreatureTemplate.Relationship LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var relationship = orig(self, dRelation);

            if (WandererCharacterMod.WandererOptions.DisableDash.Value)
                return relationship;

            //漫游者猫猫养的
            if (self.friendTracker.friend != null && (self.friendTracker.friend is Player) && (self.friendTracker.friend as Player).slugcatStats.name.value == WandererCharacterMod.WandererName)
            {
                //如果角色为蛞蝓猫
                if (ChangeLizardAIOption && dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                {
                    //忽略秃鹫面具惊吓
                    if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
                        relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                }

                //忽略活的拾荒
                if (ChangeLizardAIOption && relationship.type == CreatureTemplate.Relationship.Type.Eats && dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
                {
                    if (self.friendTracker.friend != null && !dRelation.trackerRep.representedCreature.state.dead)
                        relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                }

                //忽略同为一家的蜥蜴
                if (ChangeLizardAIOption && relationship.type == CreatureTemplate.Relationship.Type.Attacks && (dRelation.trackerRep.representedCreature.realizedCreature is Lizard) && (dRelation.trackerRep.representedCreature.realizedCreature as Lizard).AI.friendTracker.friend != null)
                {
                    if (self.friendTracker.friend != null)
                        relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                }
            }
            //没养猫猫
            else if (self.friendTracker.friend == null || !(self.friendTracker.friend is Player))
            {
                if (self.creature.Room != null || self.friendTracker.friend != null)
                    return relationship;

                Player toPlayer = null;
                float max = -10f;
                var room = self.creature.Room.realizedRoom;

                //总体好感度高自动保护
                foreach (var player in room.PlayersInRoom)
                {
                    if (player.slugcatStats.name.value == WandererCharacterMod.WandererName && room.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Lizards, room.game.world.RegionNumber, player.playerState.playerNumber) == 1.0f &&
                             dRelation.trackerRep.representedCreature.creatureTemplate.dangerousToPlayer > 0f && dRelation.trackerRep.representedCreature.state.alive && dRelation.trackerRep.representedCreature.realizedCreature != null &&
                             !(dRelation.trackerRep.representedCreature.realizedCreature is Lizard) && dRelation.trackerRep.representedCreature.abstractAI != null && dRelation.trackerRep.representedCreature.abstractAI.RealAI != null)
                    {
                        float num2 = 0.5f * Mathf.Pow(dRelation.trackerRep.representedCreature.creatureTemplate.dangerousToPlayer * dRelation.trackerRep.representedCreature.abstractAI.RealAI.CurrentPlayerAggression(player.abstractCreature), 0.5f);
                        num2 *= Mathf.InverseLerp(30f, 7f, Custom.WorldCoordFloatDist(room.GetWorldCoordinate(player.mainBodyChunk.pos), dRelation.trackerRep.BestGuessForPosition()));
                        if (!Custom.DistLess(room.GetWorldCoordinate(player.mainBodyChunk.pos), dRelation.trackerRep.BestGuessForPosition(), Custom.WorldCoordFloatDist(self.creature.pos, room.GetWorldCoordinate(player.mainBodyChunk.pos))))
                        {
                            num2 *= 0.5f;
                        }
                        if (num2 > 0f && (self.StaticRelationship(dRelation.trackerRep.representedCreature).type != CreatureTemplate.Relationship.Type.Eats || self.StaticRelationship(dRelation.trackerRep.representedCreature).intensity < num2))
                        {
                            if (num2 > max)
                            {
                                toPlayer = player;
                                max = num2;
                            }
                        }
                    }
                }
                if (toPlayer != null)
                {
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Attacks, max);
                }
            }
            return relationship;
        }

        ///战役相关///

        private void LizardAI_ctor(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
        {
            orig(self, creature, world);
            if (creature.world.game.session.characterStats.name.value == WandererCharacterMod.WandererName)
                //降低驯服难度
                self.friendTracker.tamingDifficlty = Mathf.Clamp(self.friendTracker.tamingDifficlty * 0.3f, 0.5f, 2f);
        }

        private void CreatureCommunities_InfluenceLikeOfPlayer(On.CreatureCommunities.orig_InfluenceLikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber, float influence, float interRegionBleed, float interCommunityBleed)
        {

            if (self.session.characterStats.name.value == WandererCharacterMod.WandererName)
                if (commID == CreatureCommunities.CommunityID.Lizards)
                {
                    //区域影响全局乘2
                    interRegionBleed *= WandererCharacterMod.WandererOptions.MessionReputationBonus.Value;

                    //在好感度为正时负影响倍率为乘2
                    if (influence < 0 && self.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, playerNumber] > 0)
                        influence *= 2f;
                }
            orig(self, commID, region, playerNumber, influence, interRegionBleed, interCommunityBleed);
        }

        ///////////////////////////////////////////////////
        //TODO: ui界面
        ///////////////////////////////////////////////////
        readonly bool ChangeLizardAIOption = true;
        readonly bool ChangeLizardFriendFire = true;

        static private LizardRelationFeature _Instance;

    }
}
