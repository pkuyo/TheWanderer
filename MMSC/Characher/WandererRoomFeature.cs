using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Characher
{
    class WandererRoomFeature : FeatureBase
    {
        public WandererRoomFeature(ManualLogSource log) : base(log)
        {

        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
        }
        class WHYROOMRENDERSOHARD
        {

        }
    }
}
