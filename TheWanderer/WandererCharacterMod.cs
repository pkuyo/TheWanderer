using BepInEx;
using Fisobs.Core;
using IL.JollyCoop;
using Newtonsoft.Json.Serialization;
using Nutils.Hook;
using Pkuyo.Wanderer.Creatures;
using Pkuyo.Wanderer.Feature;
using Pkuyo.Wanderer.Options;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace Pkuyo.Wanderer
{
    [BepInPlugin("pkuyo.thevanguard", "The Vanguard", "1.0.0")]
    public class WandererCharacterMod : BaseUnityPlugin
    {
        static public readonly string ModID = "pkuyo.thevanguard";
        static public readonly string WandererName = "wanderer";
        public WandererCharacterMod()
        {
            _hooks = new List<HookBase>();
            WandererOptions = new WandererOptions(Logger);
        }

        public void OnEnable()
        {
            var modList = FindObjectsOfType<BaseUnityPlugin>();
            foreach(var mod in modList)
            {
                if(mod.Info.Metadata.GUID == "nutils")
                {
                    hasNutils = true; 
                    break; 
                }
            }

            if (!hasNutils)
            {
                Logger.LogError("Missing Nutils Mod. Auto Disable The Vanguard Mod.");
                On.ModManager.RefreshModsLists += ModManager_RefreshModsLists_RemoveMyMod;
                On.RainWorld.PostModsInit += RainWorld_PostModsInit_ClearHook;
            }
            else
            {
                WandererModEnum.RegisterValues();

                _hooks.Add(WandererAssetManager.Instance(Logger));
                _hooks.Add(MessionHook.Instance(Logger));
                _hooks.Add(SSOracleHook.Instance(Logger));
                _hooks.Add(CoolObjectHook.Instance(Logger));
                _hooks.Add(AchievementHook.Instance(Logger));
                _hooks.Add(DreamSceneHook.Instance(Logger));

                _hooks.Add(ToxicSpiderHook.Instance(Logger));

                _hooks.Add(ClimbWallFeature.Instance(Logger));
                _hooks.Add(ListenLizardFeature.Instance(Logger));
                _hooks.Add(LoungeFeature.Instance(Logger));
                _hooks.Add(ScareLizardFeature.Instance(Logger));
                _hooks.Add(LizardRelationFeature.Instance(Logger));
                _hooks.Add(WandererGraphicsFeature.Instance(Logger));

                Content.Register(new ToxicSpiderCritob());
                Content.Register(new CoolObjectFisob());

                On.RainWorld.OnModsInit += RainWorld_OnModsInit;
                On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
                On.RainWorld.PostModsInit += ModCompatibilityFix.RainWorld_PostModsInit;
            }
        }

        private void RainWorld_PostModsInit_ClearHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            On.ModManager.RefreshModsLists -= ModManager_RefreshModsLists_RemoveMyMod;
        }

        private void ModManager_RefreshModsLists_RemoveMyMod(On.ModManager.orig_RefreshModsLists orig, RainWorld rainWorld)
        {
            orig(rainWorld);
            foreach (var mod in ModManager.InstalledMods)
                if(mod.id == ModID || mod.id == "nutils")
                    mod.enabled = false;

            ModManager.ActiveMods.RemoveAll(mod => mod.id == ModID || mod.id == "nutils");
        }




        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch(Exception e)
            {
                Logger.LogError("Other Mod Init Failed!");
                Logger.LogError(e);
            }
            try
            {
                if (!isLoad)
                {
                    MachineConnector.SetRegisteredOI(ModID, WandererOptions);

                    CampaignHook.AddSpawnPos(WandererName, 8, 4, -1, "SB_INTROROOM1");

                    SceneHook.AddIntroSlideShow(WandererName, "RW_Intro_Theme", WandererModEnum.Scene.WandererIntro, DreamSceneHook.BuildSlideShow);
                    SceneHook.AddScene(WandererModEnum.Scene.Intro_W1, DreamSceneHook.BuildWandererScene1);
                    SceneHook.AddScene(WandererModEnum.Scene.Intro_W2, DreamSceneHook.BuildWandererScene2);

                    foreach (var feature in _hooks)
                        feature.OnModsInit(self);

                    isLoad = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Wanderer Init Failed!");
                Logger.LogError(e);
            }

        }


        private void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
        {

            foreach (var mod in newlyDisabledMods)
            {
                if (mod.id == ModID)
                {
                    WandererModEnum.UnRegisterValues();
                    return;
                }
            }
        }

        static private List<HookBase> _hooks;

        private bool isLoad=false;
        private bool hasNutils = false;
        static public WandererOptions WandererOptions;
    }
}
