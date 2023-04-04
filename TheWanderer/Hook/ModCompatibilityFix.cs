using HarmonyLib;
using RWCustom;
using System;
using System.Reflection;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class ModCompatibilityFix 
    {
        public ModCompatibilityFix()
        {

        }



        static public void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
        }


    }
}
