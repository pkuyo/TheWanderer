﻿using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using Pkuyo.Wanderer.LizardMessage;
using System.Runtime.CompilerServices;

namespace Pkuyo.Wanderer.Characher
{
    class ListenLizardFeature : FeatureBase
    {

        public ListenLizardFeature(ManualLogSource log) :base(log)
        {
            WandererLizards = new ConditionalWeakTable<Lizard, WandererLizard>();
        }


        public override void OnModsInit(RainWorld rainWorld)
        {
            RandomMessagePicker.InitLizardMessage(_log,rainWorld);
            StatePriority.InitStatePriority(_log);
            LizardDialogBox.InitDialogBoxStaticData(_log);

            On.Lizard.ctor += Lizard_ctor;
            On.Lizard.Update += Lizard_Update;

            _log.LogDebug("ListenLizardFeature Init");
        }

        private void Lizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
        {
            orig(self,eu);
            WandererLizard lizard;
            if (WandererLizards.TryGetValue(self, out lizard))
                lizard.Update();


        }

        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature,world);


            WandererLizard lizard;
            if(!WandererLizards.TryGetValue(self,out lizard))
                WandererLizards.Add(self, new WandererLizard(self));
        }
        ConditionalWeakTable<Lizard,WandererLizard> WandererLizards;
    }
}
