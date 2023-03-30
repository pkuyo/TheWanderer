using RWCustom;
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
            foreach(var mod in ModManager.ActiveMods)
            {
                if(mod.id == "dressmyslugcat")
                    On.Menu.MainMenu.ctor += MainMenu_ctor;
            }
        }

        static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            ErrorLabel = new FLabel(Custom.GetFont(), Custom.rainWorld.inGameTranslator.Translate("DRESS MY SLUGCAT MAY HAVE COMPATIBILITY ISSUES [The Vanguard]"));
            ErrorLabel.color = Color.red;
            ErrorLabel.anchorX = 0;
            ErrorLabel.anchorY = 0;
            ErrorLabel.x += 20;
            ErrorLabel.scale= 2;
            ErrorLabel.y = Custom.rainWorld.screenSize.y-50;
            self.container.AddChild(ErrorLabel);
        }
        static FLabel ErrorLabel;
    }
}
