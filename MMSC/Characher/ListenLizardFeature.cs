using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using MMSC.LizardMessage;

namespace MMSC.Characher
{
    class ListenLizardFeature : FeatureBase
    {

        public ListenLizardFeature(ManualLogSource log) :base(log)
        {
            WandererLizards = new Dictionary<Lizard, WandererLizard>();
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
            WandererLizards[self].Update();

        }

        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature,world);

            foreach(var wandererLizard in WandererLizards)
                if(wandererLizard.Key==null)
                {
                    wandererLizard.Value.Destroy();
                    WandererLizards.Remove(wandererLizard.Key);
                }
            if(!WandererLizards.ContainsKey(self))
                WandererLizards.Add(self, new WandererLizard(self));
        }
        Dictionary<Lizard,WandererLizard> WandererLizards;
    }
}
