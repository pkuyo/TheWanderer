using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Pkuyo.Wanderer.Characher;
using Pkuyo.Wanderer.Options;

namespace Pkuyo.Wanderer
{
    [BepInPlugin("pkuyo.thewanderer", "The Wanderer", "1.0.0")]
    public class WandererCharacterMod : BaseUnityPlugin
    {
        public WandererCharacterMod()
        {
            _features = new List<FeatureBase>();

            _features.Add(new ClimbWallFeature(Logger));
            _features.Add(new ListenLizardFeature(Logger));
            _features.Add(new WandererGraphicsFeature(Logger));
            _features.Add(new ScareLizardFeature(Logger));
            _features.Add(new LizardRelationFeature(Logger));
            _features.Add(new MessionHudFeature(Logger));
            _wandererOptions = new WandererOptions(Logger);

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
     
            orig(self);
            try
            {
                foreach (var feature in _features)
                    feature.OnModsInit(self);
                _wandererOptions.OnModsInit(self);
            }
            catch(Exception e)
            {
                Logger.LogError(e.Message + e.StackTrace);
            }
        }

        private List<FeatureBase> _features;

        private WandererOptions _wandererOptions;
    }
}
