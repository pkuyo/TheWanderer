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
    //全部更改均为战役专属
    class LizardRelationFeature : FeatureBase
    {
        public LizardRelationFeature(ManualLogSource log) : base(log)
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            On.CreatureCommunities.InfluenceLikeOfPlayer += CreatureCommunities_InfluenceLikeOfPlayer;

            On.LizardAI.ctor += LizardAI_ctor;
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship;

            On.Lizard.SpearStick += Lizard_SpearStick;
            On.Lizard.Violence += Lizard_Violence;
            On.SocialEventRecognizer.WeaponAttack += SocialEventRecognizer_WeaponAttack;
            _log.LogDebug("LizardRelationFeature(session only) Init");
        }

        private void SocialEventRecognizer_WeaponAttack(On.SocialEventRecognizer.orig_WeaponAttack orig, SocialEventRecognizer self, PhysicalObject weapon, Creature thrower, Creature victim, bool hit)
        {
            if (ChangeLizardFriendFire && victim.abstractCreature.world.game.session.characterStats.name.value == "wanderer")
                if (victim is Lizard && (victim as Lizard).AI.friendTracker.friend == thrower)
                    return;
            
            orig(self, weapon, thrower, victim, hit);
        }

        private void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            //去除石头和矛的友伤
            if (ChangeLizardFriendFire && self.abstractCreature.world.game.session.characterStats.name.value == "wanderer") 
                if(self.AI.friendTracker.friend != null)
                    return;

            orig(self,source,directionAndMomentum,hitChunk,onAppendagePos,type,damage,stunBonus);
        }

        private bool Lizard_SpearStick(On.Lizard.orig_SpearStick orig, Lizard self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos onAppendagePos, Vector2 direction)
        {
            //去除石头和矛的友伤
            if (ChangeLizardFriendFire && self.abstractCreature.world.game.session.characterStats.name.value == "wanderer") 
                if(self.AI.friendTracker.friend != null)
                    return false;

            return orig(self, source, dmg, chunk, onAppendagePos, direction);
        }

        private CreatureTemplate.Relationship LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var relationship = orig(self, dRelation);
            //非角色战役不更改
            if (self.creature.world.game.session.characterStats.name.value != "wanderer")
                return relationship;

            ///////////////////////////////////////////////////
            //TODO: ui界面
            ///////////////////////////////////////////////////
            ///
            //如果角色为蛞蝓猫
            if (ChangeLizardAIOption && dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
            {
                //忽略秃鹫面具惊吓
                if (relationship.type == CreatureTemplate.Relationship.Type.Afraid && self.friendTracker.friend != null)
                    relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            }

            //忽略活的拾荒
            if (ChangeLizardAIOption && self.friendTracker.friend !=null && relationship.type == CreatureTemplate.Relationship.Type.Eats && dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
            {
                if(self.friendTracker.friend != null && !dRelation.trackerRep.representedCreature.state.dead)
                    relationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            }
            return relationship;
        }

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
                    if (self.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, playerNumber] > 0)
                        influence *= 2f;
                }
            orig(self,commID,region,playerNumber,influence,interCommunityBleed,interCommunityBleed);
        }

        ///////////////////////////////////////////////////
        //TODO: ui界面
        ///////////////////////////////////////////////////
        bool ChangeLizardAIOption = true;
        bool ChangeLizardFriendFire = true;

    }
}
