using BepInEx.Logging;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Characher
{
    class LizardRelationFeature : FeatureBase
    {
        public LizardRelationFeature(ManualLogSource log) : base(log)
        {
        }

        public override void OnModsInit()
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
            var re = orig(projTracer, room, lastPos,ref pos, rad, collisionLayer, exemptObject, hitAppendages);
            if (ChangeLizardFriendFire)
                if (re.obj is Lizard && exemptObject is Player && ((re.obj as Lizard).AI.friendTracker.friend != null || (re.obj as Lizard).abstractCreature.world.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Lizards, (re.obj as Lizard).abstractCreature.world.RegionNumber, (exemptObject as Player).playerState.playerNumber)==1)
                      && (exemptObject as Player).slugcatStats.name.value == "wanderer")
                    re.obj = null;
            return re;
        }


        private CreatureTemplate.Relationship LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var relationship = orig(self, dRelation);

            ///////////////////////////////////////////////////
            //TODO: ui界面
            ///////////////////////////////////////////////////
            //漫游者猫猫养的
            if (self.friendTracker.friend != null && (self.friendTracker.friend is Player) && (self.friendTracker.friend as Player).slugcatStats.name.value == "wanderer")
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
            }
            return relationship;
        }

        ///战役相关///
        
        private void LizardAI_ctor(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
        {
            orig(self, creature, world);
            if (self.creature.world.game.session.characterStats.name.value == "wanderer")
                //降低驯服难度
                self.friendTracker.tamingDifficlty *= 0.3f;
        }

        private void CreatureCommunities_InfluenceLikeOfPlayer(On.CreatureCommunities.orig_InfluenceLikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber, float influence, float interRegionBleed, float interCommunityBleed)
        {

            if (self.session.characterStats.name.value == "wanderer")
                if (commID == CreatureCommunities.CommunityID.Lizards)
                {
                    //区域影响全局乘2
                    interRegionBleed *= 2f;

                    //在好感度为正时负影响倍率为乘2
                    if (influence <0 && self.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, playerNumber] > 0)
                        influence *= 2f;
                }
            orig(self,commID,region,playerNumber,influence, interRegionBleed, interCommunityBleed);
        }

        ///////////////////////////////////////////////////
        //TODO: ui界面
        ///////////////////////////////////////////////////
        bool ChangeLizardAIOption = true;
        bool ChangeLizardFriendFire = true;

    }
}
