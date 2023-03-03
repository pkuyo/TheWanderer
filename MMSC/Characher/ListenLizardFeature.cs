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
            
        }


        public override void OnModsInit(RainWorld rainWorld)
        {
            RandomMessagePicker.InitLizardMessage(_log,rainWorld);
            StatePriority.InitStatePriority(_log);
            LizardDialogBox.InitDialogBoxStaticData(_log);

            On.LizardAI.Update += LizardAI_Update;
            _log.LogDebug("ListenLizardFeature Init");
        }


        private void LizardAI_Update(On.LizardAI.orig_Update orig, LizardAI self)
        {
            orig(self);
            var lizard = self.creature.realizedCreature as Lizard;

            //若房间内存在漫游者则可以聆听
            var canListen = false;
            foreach (var player in lizard.room.game.Players)
                if ((player.realizedCreature as Player).slugcatStats.name.value == "wanderer")
                    canListen = true;

            if(canListen)
                LizardDialogBox.LizardDialogUpdate(lizard);
        }




    }
}
