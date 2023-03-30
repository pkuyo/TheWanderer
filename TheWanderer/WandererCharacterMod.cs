using BepInEx;
using Pkuyo.Wanderer.Feature;
using Pkuyo.Wanderer.Options;
using System;
using System.Collections.Generic;
using System.Security.Permissions;

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

            _hooks.Add(WandererAssetManager.Instance(Logger));
            _hooks.Add(ShitRegionMergeFix.Instance(Logger));
            _hooks.Add(MessionHook.Instance(Logger));
            _hooks.Add(SSOracleHook.Instance(Logger));
            _hooks.Add(CoolObjectHook.Instance(Logger));
            _hooks.Add(AchievementHook.Instance(Logger));
            _hooks.Add(SceneHook.Instance(Logger));
            

            _hooks.Add(PlayerBaseFeature.Instance(Logger));
            _hooks.Add(ClimbWallFeature.Instance(Logger));
            _hooks.Add(ListenLizardFeature.Instance(Logger));
            _hooks.Add(LoungeFeature.Instance(Logger));
            _hooks.Add(ScareLizardFeature.Instance(Logger));
            _hooks.Add(LizardRelationFeature.Instance(Logger));
            _hooks.Add(WandererGraphicsFeature.Instance(Logger));

            

            WandererOptions = new WandererOptions(Logger);

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;

            On.RainWorld.PostModsInit += ModCompatibilityFix.RainWorld_PostModsInit;
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

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {

            orig(self);

            try
            {
                WandererModEnum.RegisterValues();
                MachineConnector.SetRegisteredOI(ModID, WandererOptions);

                foreach (var feature in _hooks)
                    feature.OnModsInit(self);

            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + e.StackTrace);
            }

        }



        static private List<HookBase> _hooks;


        static public WandererOptions WandererOptions;
    }
}
