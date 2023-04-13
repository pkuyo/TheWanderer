using BepInEx.Logging;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class DreamScene
    {


        static public void BuildSlideShow(SlideShow self)
        {
            self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));
            self.playList.Add(new SlideShow.Scene(WandererModEnum.Scene.Intro_W1,
                self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 26), self.ConvertTime(0, 6, 26)));
            self.playList.Add(new SlideShow.Scene(WandererModEnum.Scene.Intro_W2,
                self.ConvertTime(0, 7, 26), self.ConvertTime(0, 10, 26), self.ConvertTime(0, 13, 26)));
            for (int n = 1; n < self.playList.Count; n++)
            {
                self.playList[n].startAt += 0.6f;
                self.playList[n].fadeInDoneAt += 0.6f;
                self.playList[n].fadeOutStartAt += 0.6f;
            }
            self.processAfterSlideShow = ProcessManager.ProcessID.Game;

        }
        static public void BuildWandererScene1(MenuScene self)
        {

            self.sceneFolder = "scenes/Intro - Wanderer - 1";

            if (self.flatMode)
            {
                //TODO : flat Mode
                //self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "Intro - Wanderer - Flat - 1", new Vector2(683f, 404f), false, true));
                return;
            }
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 1 - Ground", new Vector2(0f, 0f), 4.5f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 1 - Draw", new Vector2(0f, 0f), 3f, MenuDepthIllustration.MenuShader.Basic));

        }

        static public void BuildWandererScene2(MenuScene self)
        {
            self.sceneFolder = "scenes" + Path.DirectorySeparatorChar.ToString() + "Intro - Wanderer - 2";
            if (self.flatMode)
            {
                //TODO : flat Mode
                //self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "Intro - Wanderer - Flat - 2", new Vector2(683f, 404f), false, true));
                return;
            }
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 2 - Ground", new Vector2(0f, 0f), 4.5f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 2 - Draw", new Vector2(0f, 0f), 3f, MenuDepthIllustration.MenuShader.Basic));
        } 
    }
}
