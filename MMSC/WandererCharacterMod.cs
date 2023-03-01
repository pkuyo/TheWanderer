using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using MMSC.Characher;

namespace MMSC
{
    [BepInPlugin("pkuyo.thewanderer", "The Wanderer", "1.0.0")]
    public class WandererCharacterMod : BaseUnityPlugin
    {
        public WandererCharacterMod()
        {
            _climbWallFeature = new ClimbWallFeature(Logger);
            _listenLizardFeature = new ListenLizardFeature(Logger);
            _wandererGraphics = new WandererGraphics(Logger);
            _scareLizardFeature = new ScareLizardFeature(Logger);
            _lizardRelationFeature = new LizardRelationFeature(Logger);
            _hudHook = new HudHook(Logger);


        }

        private ClimbWallFeature _climbWallFeature;
        private ListenLizardFeature _listenLizardFeature;
        private WandererGraphics _wandererGraphics;
        private ScareLizardFeature _scareLizardFeature;
        private LizardRelationFeature _lizardRelationFeature;
        private HudHook _hudHook;
    }
}
