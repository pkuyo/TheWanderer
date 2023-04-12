using BepInEx.Logging;
using Menu;
using MoreSlugcats;
using System.Collections.Generic;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class AchievementHook : HookBase
    {
        AchievementHook(ManualLogSource log) : base(log)
        {

        }

        static public AchievementHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new AchievementHook(log);
            return _instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.WinState.CycleCompleted += WinState_CycleCompleted;
            On.WinState.PassageDisplayName += WinState_PassageDisplayName;
            On.WinState.CreateAndAddTracker += WinState_CreateAndAddTracker;

            On.Menu.CustomEndGameScreen.GetDataFromSleepScreen += CustomEndGameScreen_GetDataFromSleepScreen;
            //  On.ProcessManager.CueAchievementPlatform += ProcessManager_CueAchievementPlatform;
        }

        private void CustomEndGameScreen_GetDataFromSleepScreen(On.Menu.CustomEndGameScreen.orig_GetDataFromSleepScreen orig, Menu.CustomEndGameScreen self, WinState.EndgameID endGameID)
        {
            orig(self,endGameID);
            if (endGameID == WandererModEnum.WinState.Dragonlord)
                self.scene = new InteractiveMenuScene(self, self.pages[0],  MenuScene.SceneID.Endgame_Survivor);

        }




        private string WinState_PassageDisplayName(On.WinState.orig_PassageDisplayName orig, WinState.EndgameID ID)
        {
            var re = orig(ID);
            if (ID == WandererModEnum.WinState.Dragonlord)
                return "The Dragonlord";
            return re;
        }

        private WinState.EndgameTracker WinState_CreateAndAddTracker(On.WinState.orig_CreateAndAddTracker orig, WinState.EndgameID ID, List<WinState.EndgameTracker> endgameTrackers)
        {
            var re = orig(ID, endgameTrackers);
            if (ID == WandererModEnum.WinState.Dragonlord)
            {
                re = new WinState.FloatTracker(ID, 0f, 0f, 0f, 1f);
                if (re != null && endgameTrackers != null)
                {
                    bool flag = false;
                    for (int j = 0; j < endgameTrackers.Count; j++)
                    {
                        if (endgameTrackers[j].ID == ID)
                        {
                            flag = true;
                            endgameTrackers[j] = re;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        endgameTrackers.Add(re);
                    }
                }
            }
            return re;
        }



        private void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
        {
            orig(self, game);
            if ((!ModManager.MMF || !MMF.cfgSurvivorPassageNotRequired.Value))
            {
                return;
            }
            WinState.FloatTracker floatTracker = null;
            if (game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, 0] > 0 || game.session.characterStats.name.value == WandererCharacterMod.WandererName)
                floatTracker = self.GetTracker(WandererModEnum.WinState.Dragonlord, true) as WinState.FloatTracker;
            else
                floatTracker = self.GetTracker(WandererModEnum.WinState.Dragonlord, false) as WinState.FloatTracker;
            if (floatTracker != null)
            {
                floatTracker.SetProgress(Mathf.Max(0.0f, game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, 0]));
            }

        }

        static private AchievementHook _instance;
    }
}
