using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static PathFinder;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.Creatures
{
    class ParasiteHook : HookBase
    {

        public delegate SlugcatStats orig_slugcatStats(Player self);

        static public ParasiteHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
            {
                _instance = new ParasiteHook(log);
            }
            return _instance;
        }

        ParasiteHook(ManualLogSource log) : base(log)
        {
            parasiteData = new ConditionalWeakTable<Creature, ParasiteData> ();
        }

        public static SlugcatStats Player_slugcatStats_get(orig_slugcatStats orig, Player self)
        {
            if (self.abstractCreature.world.game.session is ParasiteGameSession)
                return self.abstractCreature.world.game.session.characterStats;
            return orig(self);
        }

        public override void OnModsInit(RainWorld rainWorld)
        {

            On.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;


            On.RainWorldGame.ExitGame += RainWorldGame_ExitGame;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.Update += RainWorldGame_Update;

            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;

            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;

            IL.RainWorldGame.ctor += RainWorldGame_ctor;
            IL.World.ctor += World_ctor;

            On.Creature.ctor += Creature_ctor;
            On.Creature.Die += Creature_Die;

            Hook overseerColourHook = new Hook(
                typeof(Player).GetProperty("slugcatStats", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ParasiteHook).GetMethod("Player_slugcatStats_get", BindingFlags.Static | BindingFlags.Public)
            );


        }


        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            if (self.session is ParasiteGameSession)
                (self.session as ParasiteGameSession).Update();
            orig(self);
        }

        private void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if (self.game.session is ParasiteGameSession)
            {
                var text = "accelerator";
                self.LoadWorld(text, self.PlayerCharacterNumber, true);
                return;
            }
            orig(self);
        }

        private void World_ctor(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchCallvirt<RainWorldGame>("get_GetArenaGameSession")))
            {
                c.GotoPrev(MoveType.After, i => i.MatchLdarg(1));

                var notArena = c.DefineLabel();
                var arena = c.DefineLabel();
                c.EmitDelegate<Func<RainWorldGame, bool>>((game) =>
                {
                    return game.IsArenaSession;
                });
                c.Emit(OpCodes.Brfalse_S, notArena);

                c.Emit(OpCodes.Ldarg_1);
                c.GotoNext(MoveType.After, i => i.MatchStfld<World>("rainCycle"));
                c.Emit(OpCodes.Br_S, arena);
                c.MarkLabel(notArena);

                c.EmitDelegate<Action<World, World>>((self, world) =>
                {
                    //我无聊 好吧是为了清除返回值
                    self.rainCycle = new RainCycle(world, 100);
                });
                c.MarkLabel(arena);
            }
        }

        private void RainWorldGame_ExitGame(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
        {
            if (self.session is ParasiteGameSession)
            {
                ExitParasiteDream(self, asDeath || asQuit);
            }
            orig(self, asDeath, asQuit);
        }


        public void ExitParasiteDream(RainWorldGame self, bool death)
        {
            parasiteDream = false;
            var game = self.manager.oldProcess as RainWorldGame;
            if (game == null)
                throw new Exception("[ParasiteGameSession] OldPrcess is not a RainWorldGame Class!");

            //progression会在切换process时清空(PostSwitchMainProcess)，需重新赋值

            game.rainWorld.progression.currentSaveState = game.GetStorySession.saveState;

            if (!death)
                game.GetStorySession.saveState.SessionEnded(game, death, ma);


            if (self.manager.musicPlayer != null)
                self.manager.musicPlayer.FadeOutAllSongs(20f);

            self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;

            if (death)
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen, 3f);
            else
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen, 3f);

            return;
        }

        private void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {

            if (self.oldProcess is RainWorldGame && self.currentMainLoop is RainWorldGame && (self.currentMainLoop as RainWorldGame).session is ParasiteGameSession)
            {
                //切换回story进行数据传输
                var game = self.oldProcess;
                self.oldProcess = self.currentMainLoop;
                self.currentMainLoop = game;

                //手动删除梦境
                self.oldProcess.ShutDownProcess();
                self.oldProcess.processActive = false;

                //清除恼人的coop控件
                if (!game.processActive && ModManager.JollyCoop)
                {
                    foreach (var camera in (game as RainWorldGame).cameras)
                    {
                        if (camera.hud != null && camera.hud.jollyMeter != null)
                        {
                            camera.hud.parts.Remove(camera.hud.jollyMeter);
                            camera.hud.jollyMeter = null;
                        }
                    }
                }
            }
            orig(self, ID);
        }

        private void RainWorldGame_ctor(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before, i => i.MatchNewobj<OverWorld>(),
                                              i => i.MatchStfld<RainWorldGame>("overWorld")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RainWorldGame>>((self) =>
                {
                    if (parasiteDream && self.manager.oldProcess is RainWorldGame)
                    {
                        self.session = new ParasiteGameSession(self, (self.manager.oldProcess as RainWorldGame).session.characterStats.name);
                    }
                });
            }
        }

        private void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            //TODO : 获取
            if (IsParasite && !(self.session is ParasiteGameSession))
            {
                parasiteDream = true;
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                ma = malnourished;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            }
            orig(self, malnourished);
        }

        private void DreamsState_StaticEndOfCycleProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
        }

        private void Creature_ctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!parasiteData.TryGetValue(self, out _))
                parasiteData.Add(self, new ParasiteData());
        }

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            Debug.Log("creature Die!");
            if (parasiteData.TryGetValue(self, out var data) && data.isParasite)
            {
                Debug.Log("Try Create Parasite");
                var hasAdult = Random.value < 0.3f;
                if (hasAdult)
                {
                    AbstractCreature abstractCreature = new AbstractCreature(self.abstractCreature.world, StaticWorld.GetCreatureTemplate(WandererEnum.Creatures.FemaleParasite), null, self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());
                    abstractCreature.state = new HealthState(abstractCreature);
                    abstractCreature.RealizeInRoom();

                    if (abstractCreature.realizedCreature != null)
                    {
                        foreach (var bodychunk in abstractCreature.realizedCreature.bodyChunks)
                        {
                            bodychunk.vel += Custom.RNV() * Random.Range(5f, 20f);
                            bodychunk.pos = self.mainBodyChunk.pos;
                        }
     

                    }
                }
                int count = Random.Range(1, 3) - (hasAdult ? 1 : 0);
                for (int i = 0; i < count; i++)
                {
                    AbstractCreature abstractCreature = new AbstractCreature(self.abstractCreature.world, StaticWorld.GetCreatureTemplate(WandererEnum.Creatures.ChildParasite), null, self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());
                    abstractCreature.state = new HealthState(abstractCreature);     
                    abstractCreature.RealizeInRoom();
                    
                    if (abstractCreature.realizedCreature != null)
                    {
                        foreach (var bodychunk in abstractCreature.realizedCreature.bodyChunks)
                        {
                            bodychunk.vel += Custom.RNV() * Random.Range(5f, 20f);
                            bodychunk.pos = self.mainBodyChunk.pos;
                        }
                       
                    }
                }
            }
            orig(self);
        }
        static ParasiteHook _instance;

        bool ma = false;

        bool parasiteDream;
        bool IsParasite = false;

        public class ParasiteData
        {
            public bool isParasite = false;
        }
        public ConditionalWeakTable<Creature, ParasiteData> parasiteData;
    }

    //TODO : 多人
    class ParasiteGameSession : GameSession
    {
        public ParasiteGameSession(RainWorldGame game, SlugcatStats.Name name) : base(game)
        {
            characterStats = new SlugcatStats(name, false);

        }
        public Room room
        {
            get
            {
                return game.cameras[0].room;
            }
        }

        public void Update()
        {
            if (EndSession) return;
            if (!isInit && room != null && room.shortCutsReady)
            {
                Init();
            }
            foreach (var player in Players)
            {
                if (player.realizedCreature != null && player.realizedCreature.dead)
                {
                    game.ExitGame(true, false);
                    EndSession = true;
                    return;
                }
            }
            if (isInit && targetCreature != null && targetCreature.dead)
            {
                game.ExitGame(false, false);
                EndSession = true;
                return;
            }
        }

        public void Init()
        {
            SpawnPlayers();
            SpawnCreatures();

            isInit = true;
        }

        public void SpawnCreatures()
        {
            int exits = game.world.GetAbstractRoom(0).exits;

            int node = Random.Range(1, exits);
            AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(WandererEnum.Creatures.ToxicSpider), null, new WorldCoordinate(0, -1, -1, -1), game.GetNewID());
            abstractCreature.state = new HealthState(abstractCreature);

            abstractCreature.Realize();
            ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new IntVector2(-1, -1), abstractCreature.realizedCreature, game.world.GetAbstractRoom(0), 0);
            shortCutVessel.entranceNode = node;
            shortCutVessel.room = game.world.GetAbstractRoom(0);
            abstractCreature.pos.room = game.world.offScreenDen.index;
            game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            targetCreature = abstractCreature.realizedCreature;
        }

        public void SpawnPlayers()
        {
            AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
            game.cameras[0].followAbstractCreature = abstractCreature;
            abstractCreature.state = new PlayerState(abstractCreature, 0, characterStats.name, false);

            abstractCreature.Realize();
            ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new IntVector2(-1, -1), abstractCreature.realizedCreature,game.world.GetAbstractRoom(0), 0);
            shortCutVessel.entranceNode = 0;
            shortCutVessel.room = game.world.GetAbstractRoom(0);
            abstractCreature.pos.room = game.world.offScreenDen.index;
            game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            AddPlayer(abstractCreature);
        }

        Creature targetCreature;
        bool isInit = false;
        bool EndSession = false;

    }
}
