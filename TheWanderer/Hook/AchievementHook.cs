using BepInEx.Logging;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            On.WinState.PassageAchievementID += WinState_PassageAchievementID;

          //  On.ProcessManager.CueAchievementPlatform += ProcessManager_CueAchievementPlatform;
        }
        private RainWorld.AchievementID WinState_PassageAchievementID(On.WinState.orig_PassageAchievementID orig, WinState.EndgameID ID)
        {
            var id = orig(ID);
            if (ID == WandererModEnum.WandererWinState.Dragonlord)
                return RainWorld.AchievementID.ArtificerEnding;
            return id;
        }


        private string WinState_PassageDisplayName(On.WinState.orig_PassageDisplayName orig, WinState.EndgameID ID)
        {
           var re = orig(ID);
            if (ID == WandererModEnum.WandererWinState.Dragonlord)
                return "The Dragonlord";
            return re;
        }

        private WinState.EndgameTracker WinState_CreateAndAddTracker(On.WinState.orig_CreateAndAddTracker orig, WinState.EndgameID ID, List<WinState.EndgameTracker> endgameTrackers)
        {
            var re = orig(ID, endgameTrackers);
            if (ID == WandererModEnum.WandererWinState.Dragonlord)
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
            if (game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, 0]>0 || game.session.characterStats.name.value == WandererCharacterMod.WandererName)
                floatTracker = self.GetTracker(WandererModEnum.WandererWinState.Dragonlord, true) as WinState.FloatTracker;
            else
                floatTracker = self.GetTracker(WandererModEnum.WandererWinState.Dragonlord, false) as WinState.FloatTracker;
            if (floatTracker != null)
            {
                floatTracker.SetProgress(Mathf.Max(0.0f,game.session.creatureCommunities.playerOpinions[CreatureCommunities.CommunityID.Lizards.Index - 1, 0, 0]));
            }

        }

        static private AchievementHook _instance;
    }
}
