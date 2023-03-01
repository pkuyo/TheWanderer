using BepInEx.Logging;
using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Characher
{
    class HudHook : FeatureBase
    {
        public HudHook(ManualLogSource log) : base(log)
        {
            _hud = new Dictionary<Player, MissionHud>();
        }

        public override void OnModsInit()
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.CreatureCommunities.InfluenceLikeOfPlayer += CreatureCommunities_InfluenceLikeOfPlayer;
            _log.LogDebug("HudHook Init");
        }

        private void CreatureCommunities_InfluenceLikeOfPlayer(On.CreatureCommunities.orig_InfluenceLikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber, float influence, float interRegionBleed, float interCommunityBleed)
        {
            orig(self, commID, region, playerNumber, influence, interRegionBleed, interCommunityBleed);

            //更新HUD
            if (self.session.characterStats.name.value == "wanderer" && commID == CreatureCommunities.CommunityID.Lizards && _hud.ContainsKey(self.session.Players[playerNumber].realizedCreature as Player))
            {
                _hud[self.session.Players[playerNumber].realizedCreature as Player].SetLikeOfPlayer(self.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, playerNumber]);
            }
        }


        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self,cam);
            
            var creature = (self.owner as Player).abstractCreature;
            if (self.owner is Player && creature.world.game.session.characterStats.name.value == "wanderer" && !_hud.ContainsKey(self.owner as Player))
            {
                var hudpart = new MissionHud(self, creature.world.game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, (self.owner as Player).playerState.playerNumber], _log);
                _hud.Add(self.owner as Player, hudpart);
                self.AddPart(hudpart);
            }
        }
        Dictionary<Player,MissionHud> _hud;
    }
    class MissionHud : HudPart
    {
        public MissionHud(HUD.HUD hud,float aacc, ManualLogSource log) : base(hud)
        {
            InitiateSprites();
            acc[1] = aacc;
        }
        public void InitiateSprites()
        {
            sprites = new FSprite[4];
            for (int i = 0; i < 4; i++)
            {
                sprites[i] = new FSprite("Futile_White", true);
                sprites[i].anchorX = sprites[i].anchorY = 0;
                sprites[i].SetPosition(pos);
                sprites[i].height = 10f;
            }

            for(int i=0;i<2;i++)
            {
                sprites[i].width = 2f;
                sprites[i].SetPosition(pos);
            }
            sprites[1].x += 210f;

            for (int i = 2; i < 4; i++)
            {
                sprites[i].x += 6f;
                sprites[i].y += 1;
                sprites[i].height = 8f;
            }
            sprites[2].width = 200f;

            sprites[3].width = 200f*acc[1];

            sprites[2].alpha = 0.2f;
            sprites[3].alpha = 0.5f;
            for(int i=0;i<4;i++)
                this.hud.fContainers[1].AddChild(sprites[i]);
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (lastTimeStacker == -1)
                lastTimeStacker = timeStacker;
            lastTimeStacker = (lastTimeStacker > timeStacker) ? lastTimeStacker -1 : lastTimeStacker;
            var deltaTime = timeStacker - lastTimeStacker;

            bool isShelter = false;
            if ((this.hud.owner as Player).room != null)
                isShelter = (this.hud.owner as Player).room.abstractRoom.shelter;
            if (isShelter)
                fade = 3.0f;

            if (ReputationChanged > 0)
            {
                fade += deltaTime / 30f;
                ReputationChanged -= deltaTime / 30f;
            }
            else if (this.hud.owner.RevealMap && fade < 6.0f)
                fade += deltaTime / 30f;
            else if (!this.hud.owner.RevealMap && ReputationChanged <= 0 && fade > 0.0f && !isShelter)
                fade -= deltaTime / 30f;
            

            fade = Mathf.Clamp(fade, 0, 6);

            var pfade = Mathf.Pow(Mathf.Clamp(fade, 0, 1), 0.2f);

            //显示隐藏部分
            sprites[0].alpha = sprites[1].alpha = pfade;
            sprites[2].alpha = 0.2f * pfade;
            sprites[3].alpha = Mathf.Lerp(0.5f, 1f, Mathf.Min(ReputationChanged, 1f)) * pfade;

            if (lastAcc > toAcc && ReputationChanged > 0)
                sprites[3].color = Color.Lerp(Color.white, new Color(237 / 255f, 86 / 255f, 88 / 255f), Mathf.Min(ReputationChanged, 1f));
            else if (lastAcc < toAcc && ReputationChanged > 0)
                sprites[3].color = Color.Lerp(Color.white, new Color(116 / 255f, 237 / 255f, 130 / 255f), Mathf.Min(ReputationChanged, 1f));
            else
                sprites[3].color = Color.white;
            sprites[0].x = Mathf.Lerp(pos.x - 20, pos.x, pfade);
            sprites[1].x = Mathf.Lerp(pos.x + 130, pos.x + 210f, pfade);

            if (ReputationChanged > 0 && pfade <1.0f)
                acc[1] = Mathf.Lerp(toAcc, lastAcc, Mathf.Pow((2f-Mathf.Clamp(ReputationChanged-1f,0,2f))/2f, 0.4f));

            for (int i = 2; i < 4; i++)
            {
                var accc = Mathf.Clamp(acc[i - 2], 0, 1);
                sprites[i].width = Mathf.Lerp(120 * accc, 200f * accc, pfade);
                sprites[i].x = Mathf.Lerp(pos.x - 20, pos.x + 6f, pfade);
            }
        }
        public void SetLikeOfPlayer(float New)
        {
            lastAcc = acc[1];
            toAcc = New;
            ReputationChanged = 10f;
        }

        Vector2 pos = new Vector2(90f, 70f);
        public FSprite[] sprites;

        float lastTimeStacker=-1f;
        float fade = 0.0f;

        float ReputationChanged = 0.0f;

        float[] acc = new float[2] { 1f, 0.0f };


        float toAcc = 0.0f;
        float lastAcc = 0.0f;

    }
}
