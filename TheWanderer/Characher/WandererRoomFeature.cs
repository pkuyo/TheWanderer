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

        private void SetupValues_ctor(On.RainWorldGame.SetupValues.orig_ctor orig, ref RainWorldGame.SetupValues self, bool player2, int pink, int green, int blue, int white, int spears, int flies, int leeches, int snails, int vultures, int lanternMice, int cicadas, int palette, bool lizardLaserEyes, bool invincibility, int fliesToWin, bool worldCreaturesSpawn, bool bake, bool OBSwidescreen, bool startScreen, bool cycleStartUp, bool OBSfullscreen, int yellows, int reds, int spiders, bool playerGlowing, int garbageWorms, int jetFish, int black, int seaLeeches, int salamanders, int bigEels, bool player1, int defaultSettingsScreen, int deers, bool devToolsActive, int daddyLongLegs, int tubeWorms, int broLongLegs, int tentaclePlants, int poleMimics, int mirosBirds, bool loadGame, bool multiUseGates, int templeGuards, int centipedes, bool world, int gravityFlickerCycleMin, int gravityFlickerCycleMax, bool revealMap, int scavengers, int scavengersShy, int scavengersLikePlayer, int centiWings, int smallCentipedes, bool loadProg, int lungs, bool playMusic, int cycleTimeMin, int cycleTimeMax, int cheatKarma, bool loadAllAmbientSounds, int overseers, int ghosts, int fireSpears, int scavLanterns, bool alwaysTravel, int scavBombs, bool theMark, int custom, int bigSpiders, int eggBugs, int singlePlayerChar, int needleWorms, int smallNeedleWorms, int spitterSpiders, int dropbugs, int cyanLizards, int kingVultures, bool logSpawned, int redCentis, int proceedLineages)
        {
            orig(ref self, player2, pink,  green,  blue,  white,  spears,  flies,  leeches,  snails,  vultures,  lanternMice,  cicadas,  palette,  true,  invincibility,  fliesToWin,  worldCreaturesSpawn,  bake,  OBSwidescreen,  startScreen,  cycleStartUp,  OBSfullscreen,  yellows,  reds,  spiders,  playerGlowing,  garbageWorms,  jetFish,  black,  seaLeeches,  salamanders,  bigEels,  player1,  defaultSettingsScreen,  deers,  devToolsActive,  daddyLongLegs,  tubeWorms,  broLongLegs,  tentaclePlants,  poleMimics,  mirosBirds,  loadGame,  multiUseGates,  templeGuards,  centipedes,  world,  gravityFlickerCycleMin,  gravityFlickerCycleMax,  revealMap,  scavengers,  scavengersShy,  scavengersLikePlayer,  centiWings,  smallCentipedes,  loadProg,  lungs,  playMusic,  cycleTimeMin,  cycleTimeMax,  cheatKarma,  loadAllAmbientSounds,  overseers,  ghosts,  fireSpears,  scavLanterns,  alwaysTravel,  scavBombs,  theMark,  custom,  bigSpiders,  eggBugs,  singlePlayerChar,  needleWorms,  smallNeedleWorms,  spitterSpiders,  dropbugs,  cyanLizards,  kingVultures,  logSpawned,  redCentis,  proceedLineages);
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
           // On.RainWorldGame.SetupValues.ctor += SetupValues_ctor;
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
