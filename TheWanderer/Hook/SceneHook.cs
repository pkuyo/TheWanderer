using BepInEx.Logging;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class SceneHook : HookBase
    {

        static bool loaded = false;
        SceneHook(ManualLogSource log) : base(log)
        {

        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            if (!loaded)
            {
                IL.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGameIL;
                IL.Menu.SlideShow.ctor += SlideShow_ctorIL;
                loaded = true;
            }
            On.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        }



        private void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);
            if (self.sceneID == WandererModEnum.WandererScene.Intro_W1)
            {
                BuildWandererScene1(self);
            }

        }

        private void BuildWandererScene1(MenuScene self)
        {

            self.sceneFolder = "scenes/Intro - Wanderer - 1";

            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "Intro - Wanderer - Flat - 1", new Vector2(683f, 404f), false, true));
                return;
            }
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 1 - Ground", new Vector2(0f, 0f), 4.5f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                "Intro - Wanderer - 1 - Draw", new Vector2(0f, 0f), 3f, MenuDepthIllustration.MenuShader.Basic));

            if (self is InteractiveMenuScene)
            {
                (self as InteractiveMenuScene).idleDepths.Add(3.1f);
                (self as InteractiveMenuScene).idleDepths.Add(2.8f);
            }

        }

        private void BuildWandererScene2(MenuScene self)
        {
            self.sceneFolder = "scenes" + Path.DirectorySeparatorChar.ToString() + "Intro - Wanderer - 2";
        }
        private void SlideShow_ctor(On.Menu.SlideShow.orig_ctor orig, SlideShow self, ProcessManager manager, SlideShow.SlideShowID slideShowID)
        {
            //处理音乐部分
            if (slideShowID == WandererModEnum.WandererScene.WandererIntro)
            {
                if (manager.musicPlayer != null)
                {
                    self.waitForMusic = "BM_SS_DOOR";
                    self.stall = true;
                    manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 10f);
                }
            }
            orig(self, manager, slideShowID);
        }

        private void SlideShow_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchStfld<SlideShow>("playList")))
            {
                var label1 = c.DefineLabel(); //非wanderer跳过用的label
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<SlideShow.SlideShowID, bool>>((id) =>
                 {
                     if (id == WandererModEnum.WandererScene.WandererIntro)
                         return true;
                     return false;
                 });
                c.Emit(OpCodes.Brfalse_S, label1);

                //添加wanderer处理
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<SlideShow>>((self) =>
                {
                    self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));
                    self.playList.Add(new SlideShow.Scene(WandererModEnum.WandererScene.Intro_W1,
                        self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 26), self.ConvertTime(0, 6, 26)));
                    for (int n = 1; n < self.playList.Count; n++)
                    {
                        self.playList[n].startAt += 0.6f;
                        self.playList[n].fadeInDoneAt += 0.6f;
                        self.playList[n].fadeOutStartAt += 0.6f;
                    }
                    self.processAfterSlideShow = ProcessManager.ProcessID.Game;
                });
                c.MarkLabel(label1);
            }
            else
                _log.LogError("SLIDE SCENE HOOK FAILED");
        }


        static public SceneHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new SceneHook(log);
            return _instance;
        }

        private void SlugcatSelectMenu_StartGameIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdfld<MainLoopProcess>("manager"),
                i => i.MatchLdsfld<ProcessManager.ProcessID>("Game")))
            {
                //注：这里堆栈留了一个arg0
                c.GotoPrev(MoveType.After, i => i.OpCode == OpCodes.Ldarg_0);
                var label = c.DefineLabel(); //跳过wanderer设置
                var label2 = c.DefineLabel();//跳过标准设置

                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<SlugcatStats.Name, bool>>((name) =>
                {
                    if (name.value == WandererCharacterMod.WandererName)
                        return true;
                    return false;
                });
                c.Emit(OpCodes.Brfalse_S, label);//如果不是则直接跳过

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<MainLoopProcess>>((main) =>
                {
                    main.manager.nextSlideshow = WandererModEnum.WandererScene.WandererIntro;
                    main.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
                });
                c.Emit(OpCodes.Br_S, label2);//跳过标准设置
                c.MarkLabel(label);

                //跳转时略过arg0
                c.GotoNext(MoveType.After, i => i.MatchLdsfld<SoundID>("MENU_Start_New_Game"));
                c.GotoPrev(MoveType.After, i => i.OpCode == OpCodes.Ldarg_0);

                c.MarkLabel(label2);

            }
            else
                _log.LogError("START GAME HOOK FAILED");

        }
        static SceneHook _instance;
    }
}
