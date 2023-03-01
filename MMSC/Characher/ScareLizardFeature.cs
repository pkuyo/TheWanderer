using BepInEx.Logging;
using Noise;
using RWCustom;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Characher
{
    class ScareLizardFeature : FeatureBase
    {
        static readonly PlayerFeature<bool> ScareLizard = PlayerBool("wanderer/scare_lizard");
        public ScareLizardFeature(ManualLogSource log) : base(log)
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            _ScareLizardData = new Dictionary<Player, int>();
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            On.Player.checkInput += Player_checkInput;
            On.Player.ctor += Player_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            _log.LogDebug("ScareLizardFeature Init");
        }

        private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            var player = self.owner as Player;
            bool isEnable;
            ScareLizard.TryGet(player, out isEnable);
            if (isEnable && _ScareLizardData[player]>0)
            {
                self.blink = 5;
            }
        }

        //为了修改输入
        private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            bool isEnable;
            ScareLizard.TryGet(self, out isEnable);
            if (isEnable && self.input[0].pckp && self.input[0].thrw && !self.lungsExhausted && _ScareLizardData[self] >= 20)
            {
                self.input[0].thrw = false;
                self.input[0].pckp = false;
                if (self.dangerGrasp == null)
                {
                    self.LoseAllGrasps();
                }

                _ScareLizardData[self] = 0;

                //劳累状态
                self.lungsExhausted = true;
                self.airInLungs = 0.05f;
                self.Stun(5);

                //惊吓物体
                var scareObject = new FirecrackerPlant.ScareObject(self.mainBodyChunk.pos);
                scareObject.lifeTime = 300;
                self.room.AddObject(new ShockWave(self.mainBodyChunk.pos, 175f, 0.035f, 15, false));
                self.room.AddObject(scareObject);
                
                self.room.PlaySound(SoundID.Firecracker_Disintegrate, self.mainBodyChunk.pos);

            }
            else if (isEnable && self.input[0].pckp && self.input[0].thrw )
            {
                if(!self.lungsExhausted)
                    _ScareLizardData[self]++;

                self.input[0].thrw = false;
                self.input[0].pckp = false;
            }
            else if (isEnable && !(self.input[0].pckp && self.input[0].thrw) && _ScareLizardData[self] > 0)
                _ScareLizardData[self]--;
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            foreach (var data in _ScareLizardData)
                if (data.Key == null)
                    _ScareLizardData.Remove(data.Key);

            orig(self, abstractCreature, world);

            if(!_ScareLizardData.ContainsKey(self))
                _ScareLizardData.Add(self, 0);
        }


        Dictionary<Player, int> _ScareLizardData;

    }
}
