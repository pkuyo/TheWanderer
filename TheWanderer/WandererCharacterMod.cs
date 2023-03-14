using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Pkuyo.Wanderer.Characher;
using Pkuyo.Wanderer.Options;

namespace Pkuyo.Wanderer
{
    [BepInPlugin("pkuyo.thevanguard", "The Vanguard", "1.0.0")]
    public class WandererCharacterMod : BaseUnityPlugin
    {
        public WandererCharacterMod()
        {
            _features = new List<FeatureBase>();

            _features.Add(WandererAssetFeature.Instance(Logger));
            _features.Add(ShitRegionMergeFix.Instance(Logger));

            _features.Add(ClimbWallFeature.Instance(Logger));
            _features.Add(ListenLizardFeature.Instance(Logger));
            _features.Add(LoungeFeature.Instance(Logger));
            _features.Add(ScareLizardFeature.Instance(Logger));
            _features.Add(LizardRelationFeature.Instance(Logger));

            _features.Add(WandererGraphicsFeature.Instance(Logger));
            _features.Add(MessionHudFeature.Instance(Logger));

            WandererOptions = new WandererOptions(Logger);

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
     
            orig(self);
            MachineConnector.SetRegisteredOI("pkuyo.thevanguard", WandererOptions);
            try
            {
                foreach (var feature in _features)
                    feature.OnModsInit(self);
                
            }
            catch(Exception e)
            {
                Logger.LogError(e.Message + e.StackTrace);
            }
            
        }

        static private List<FeatureBase> _features;

  

        static public WandererOptions WandererOptions;
    }
}
