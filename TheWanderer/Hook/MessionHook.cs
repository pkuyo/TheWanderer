using BepInEx.Logging;
using HUD;
using RWCustom;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class MessionHook : HookBase
    {
        MessionHook(ManualLogSource log) : base(log)
        {

        }

        static public MessionHook Instance(ManualLogSource log = null)
        {
            if (_Instance == null)
                _Instance = new MessionHook(log);
            return _Instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.HUD.HUD.Update += HUD_Update;
            On.CreatureCommunities.InfluenceLikeOfPlayer += CreatureCommunities_InfluenceLikeOfPlayer;
            On.SaveState.ctor += SaveState_ctor;
            _log.LogDebug("WanderMessionFeature Init");
        }

        private void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (saveStateNumber.value == WandererCharacterMod.WandererName)
            {
                self.miscWorldSaveData.SLOracleState.neuronsLeft=7;
            }
        }

        private void HUD_Update(On.HUD.HUD.orig_Update orig, HUD.HUD self)
        {
            orig(self);
            if (wandererHud != null)
                wandererHud.Update();
        }



        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);

            //判断是否为the wanderer战役
            if (self.owner is Player && (self.owner as Player).abstractCreature.world.game.session.characterStats.name.value == WandererCharacterMod.WandererName)
            {
                if (wandererHud != null)
                    wandererHud.Destroy();

                wandererHud = new WandererMessionHud(self);
            }
        }

        private void CreatureCommunities_InfluenceLikeOfPlayer(On.CreatureCommunities.orig_InfluenceLikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber, float influence, float interRegionBleed, float interCommunityBleed)
        {
            orig(self, commID, region, playerNumber, influence, interRegionBleed, interCommunityBleed);

            //更新HUD
            if (wandererHud != null && commID == CreatureCommunities.CommunityID.Lizards)
            {
                wandererHud.SetLikeOfPlayer(self.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, playerNumber]);
            }
        }

        static private MessionHook _Instance;

        WandererMessionHud wandererHud;
    }

    class WandererMessionHud
    {
        public WandererMessionHud(HUD.HUD owner)
        {
            this.owner = owner;

            var creature = (owner.owner as Player).abstractCreature;
            var hudpart = new MissionHud(owner, creature.world.game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, 0], null);
            _hud = hudpart;
            owner.AddPart(hudpart);
        }

        public void Update()
        {
            if (owner == null)
                return;
            if (!(owner.owner is Player))
            {
                Destroy();
                return;
            }

            var room = (owner.owner as Player).room;
            if (room == null)
                return;

            //爬墙教程
            if (!ClimbWallTurtorial && room.roomSettings.name == "SB_INTROROOM1" && (room.GetWorldCoordinate((owner.owner as Player).firstChunk.pos).x >= 53))
            {
                ClimbWallTurtorial = true;
                room.AddObject(new WandererClimbTurtorial(room));
            }
            //冲刺教程
            else if (room.roomSettings.name == "SB_GOR02VAN"&&! LoungeTurtorial)
            {
                room.AddObject(new WandererLoungeTurtorial(room));
                LoungeTurtorial = true;
            }
            //惊吓蜥蜴教程
            else if (!ScareTurtorial && room.roomSettings.name == "SB_H03")
            {
                bool hasLizard = false;
                foreach (var creature in room.abstractRoom.creatures)
                {
                    if (creature.realizedCreature is Lizard)
                    {
                        hasLizard = true;
                        break;
                    }
                }
                if (hasLizard)
                {
                    room.AddObject(new WandererScareTurtorial(room));
                    ScareTurtorial = true;
                }
            }
            //工具教程
            else if (room.roomSettings.name == "SS_L01" && room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 3)
            {
                room.AddObject(new WandererToolTurtorial(room));
                room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
            }
            //DemoEnding
            else if (room.roomSettings.name == "IW_DemoEnding" && !DemoEnding)
            {
                DemoEnding = true;
                owner.InitDialogBox();
                owner.dialogBox.NewMessage(Custom.rainWorld.inGameTranslator.Translate("Thank you for playing this mod!"), 50);
                owner.dialogBox.NewMessage(Custom.rainWorld.inGameTranslator.Translate("The real ending and new region are in the works, so stay tuned!"), 300);
            }
        }

        public void SetLikeOfPlayer(float like)
        {
            _hud.SetLikeOfPlayer(like);
        }

        public void Destroy()
        {
            if (_hud != null)
                _hud.slatedForDeletion = true;
            owner = null;
            _hud = null;
        }

        bool ClimbWallTurtorial = false;
        bool ScareTurtorial = false;
        bool LoungeTurtorial = false;
        bool DemoEnding = false;
        MissionHud _hud;
        HUD.HUD owner;
    }

    class MissionHud : HudPart
    {
        public MissionHud(HUD.HUD hud, float aacc, ManualLogSource log) : base(hud)
        {
            acc[1] = aacc;
            InitiateSprites();
            _log = log;
        }
        public void InitiateSprites()
        {
            //设置起始位置
            var tmppos = pos + this.hud.rainWorld.options.SafeScreenOffset;

            sprites = new FSprite[4];
            for (int i = 0; i < 4; i++)
            {
                sprites[i] = new FSprite("Futile_White", true);
                sprites[i].anchorX = sprites[i].anchorY = 0;
                sprites[i].SetPosition(tmppos);
                sprites[i].height = 10f;
            }

            for (int i = 0; i < 2; i++)
            {
                sprites[i].width = 2f;
                sprites[i].SetPosition(tmppos);
            }
            sprites[1].x += 210f;

            for (int i = 2; i < 4; i++)
            {
                sprites[i].x += 6f;
                sprites[i].y += 1;
                sprites[i].height = 8f;
            }
            sprites[2].width = 200f;

            sprites[3].width = 200f * acc[1];

            sprites[2].alpha = 0.2f;
            sprites[3].alpha = 0.5f;
            for (int i = 0; i < 4; i++)
                this.hud.fContainers[1].AddChild(sprites[i]);
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            //设置起始位置
            var tmppos = pos + this.hud.rainWorld.options.SafeScreenOffset;

            //是否是避难所内，是则强制显示
            bool isShelter = false;
            if ((this.hud.owner as Player).room != null)
                isShelter = (this.hud.owner as Player).room.abstractRoom.shelter;
            if (isShelter)
                fade = 3.0f;

            //声望变动时显示
            if (ReputationChanged > 0)
            {
                fade += 1 / 30f;
                ReputationChanged -= 1 / 30f;
            }
            else if (this.hud.owner.RevealMap && fade < 6.0f)
                fade += 1 / 30f;
            else if (!this.hud.owner.RevealMap && ReputationChanged <= 0 && fade > 0.0f && !isShelter)
                fade -= 1 / 30f;


            fade = Mathf.Clamp(fade, 0, 6);

            var pfade = Mathf.Pow(Mathf.Clamp(fade, 0, 1), 0.2f);

            //显示隐藏部分
            sprites[0].alpha = sprites[1].alpha = pfade;
            sprites[2].alpha = 0.2f * pfade;
            sprites[3].alpha = Mathf.Lerp(0.5f, 1f, Mathf.Min(ReputationChanged, 1f)) * pfade;

            //声望变动导致颜色变化
            if (lastAcc > toAcc && ReputationChanged > 0)
                sprites[3].color = Color.Lerp(Color.white, new Color(237 / 255f, 86 / 255f, 88 / 255f), Mathf.Min(ReputationChanged, 1f));
            else if (lastAcc < toAcc && ReputationChanged > 0)
                sprites[3].color = Color.Lerp(Color.white, new Color(116 / 255f, 237 / 255f, 130 / 255f), Mathf.Min(ReputationChanged, 1f));
            else
                sprites[3].color = Color.white;

            sprites[0].x = Mathf.Lerp(tmppos.x - 20, tmppos.x, pfade);
            sprites[1].x = Mathf.Lerp(tmppos.x + 130, tmppos.x + 210f, pfade);

            if (ReputationChanged > 0 && pfade < 1.0f)
                acc[1] = Mathf.Lerp(toAcc, lastAcc, Mathf.Pow((2f - Mathf.Clamp(ReputationChanged - 1f, 0, 2f)) / 2f, 0.4f));

            for (int i = 2; i < 4; i++)
            {
                var accc = Mathf.Clamp(acc[i - 2], 0, 1);
                sprites[i].width = Mathf.Lerp(120 * accc, 200f * accc, pfade);
                sprites[i].x = Mathf.Lerp(tmppos.x - 20, tmppos.x + 6f, pfade);
            }
        }
        public void SetLikeOfPlayer(float New)
        {
            if (New == toAcc)
                return;

            lastAcc = acc[1];
            toAcc = New;
            ReputationChanged = 10f;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            foreach (var a in sprites)
                a.RemoveFromContainer();
        }

        Vector2 pos = new Vector2(95f, 70f);
        public FSprite[] sprites;

        float fade = 0.0f;

        float ReputationChanged = 0.0f;
        readonly float[] acc = new float[2] { 1f, 0.0f };
        float toAcc = 0.0f;
        float lastAcc = 0.0f;
        readonly ManualLogSource _log;

    }
}
