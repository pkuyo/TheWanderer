using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Nutils.hook;
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
            parasiteData = new ConditionalWeakTable<AbstractCreature, ParasiteData> ();
        }


        public override void OnModsInit(RainWorld rainWorld)
        {

            On.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;
            On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.Creature.Die += Creature_Die;
        }


        private void DreamsState_StaticEndOfCycleProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
        }


        private void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self,world,creatureTemplate, realizedCreature, pos, ID);
            if (self.creatureTemplate.type == CreatureTemplate.Type.Fly ||
                self.creatureTemplate.type == WandererEnum.Creatures.ChildParasite ||
                self.creatureTemplate.type == WandererEnum.Creatures.FemaleParasite ||
                self.creatureTemplate.type == WandererEnum.Creatures.MaleParasite)
                return;

            if (!parasiteData.TryGetValue(self, out _))
            {
                parasiteData.Add(self, new ParasiteData());
                WandererMod.Log("Add parasite data for" + self.type + " " + self.ID);
            }
        }

 

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            if (parasiteData.TryGetValue(self.abstractCreature, out var data) && data.isParasite && !data.hasBirth)
            {
                data.hasBirth = true;
                self.room.AddObject(new ParasiteBirth(self));
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
            public bool hasBirth = false;
        }
        public ConditionalWeakTable<AbstractCreature, ParasiteData> parasiteData;

        public static void AddParasiteFood(Player owner)
        {
            if (!owner.room.game.IsStorySession)
                return;
            WandererMod.Log("Increase food limit to hibernate");
            owner.room.game.session.characterStats.foodToHibernate = Mathf.Min(owner.room.game.session.characterStats.maxFood, owner.room.game.session.characterStats.foodToHibernate + 1);
            if (owner.room.game.cameras[0].hud.foodMeter != null)
            {
                owner.showKarmaFoodRainTime = 80;
                owner.room.game.cameras[0].hud.foodMeter.MoveSurvivalLimit(owner.room.game.session.characterStats.foodToHibernate, true);
                owner.room.game.cameras[0].hud.foodMeter.survivalLimit = owner.room.game.session.characterStats.foodToHibernate;
            }
        }
            

    }
    
    class ParasiteBirth : UpdatableAndDeletable
    {
        public ParasiteBirth(Creature origin)
        {
            WandererMod.Log("Add ParasiteBirth");
            if (origin == null)
            {
                hasExplosion = true;
                Destroy();
            }
            this.origin = origin;

            rads = new float[origin.bodyChunks.Length];
            pos = new Vector2[origin.bodyChunks.Length];
            for (int i = 0; i < origin.bodyChunks.Length; i++)
            {
                rads[i] = origin.bodyChunks[i].rad;
                pos[i] = origin.bodyChunks[i].pos;

            }



        }

        public void UpdateColor()
        {
            if (slatedForDeletetion)
                return;
            if (sLeaser == null && origin.graphicsModule != null)
            {
                foreach (var sl in room.game.cameras[0].spriteLeasers)
                {
                    if (sl.drawableObject == origin.graphicsModule)
                    {
                        sLeaser = sl;
                        if (origin is Lizard)
                        {
                            color = new Color[1];
                            color[0] = ((origin as Lizard).graphicsModule as LizardGraphics).palette.blackColor;
                            break;
                        }
                        else
                        {
                            color = new Color[sLeaser.sprites.Length];
                            for (int i = 0; i < color.Length; i++)
                                color[i] = sLeaser.sprites[i].color;
                            break;
                        }
                    }
                }
            }
            
            if (sLeaser!=null)
            {
                if (!hasExplosion)
                {
                    if (origin is Lizard && (origin as Lizard).graphicsModule != null)
                    {
                        var pa = ((origin as Lizard).graphicsModule as LizardGraphics).palette;
                        pa.blackColor = Color.Lerp(color[0], Color.white, Mathf.InverseLerp(0, maxCount, counter));
                        ((origin as Lizard).graphicsModule as LizardGraphics).ApplyPalette(sLeaser, room.game.cameras[0], pa);
                    }
                    else
                    {
                        for (int i = 0; i < sLeaser.sprites.Length; i++)
                            sLeaser.sprites[i].color = Color.Lerp(color[i], Color.white, Mathf.InverseLerp(0, maxCount, counter));
                    }
                }
                else
                {
                    if (origin is Lizard && (origin as Lizard).graphicsModule != null)
                    {
                        var pa = ((origin as Lizard).graphicsModule as LizardGraphics).palette;
                        pa.blackColor = Color.Lerp(pa.blackColor, color[0], 0.02f);
                        ((origin as Lizard).graphicsModule as LizardGraphics).ApplyPalette(sLeaser, room.game.cameras[0], pa);
                    }
                    else
                    {
                        for (int i = 0; i < sLeaser.sprites.Length; i++)
                            sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, color[i], 0.02f);
                    }
                }
            }
           
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion || room == null)
                return;

            UpdateColor();

            if (room.PlayersInRoom.Count == 0)
            {
                Explosion();
                Destroy();
                return;
            }
            if (counter > maxCount && !hasExplosion)
            {
                Explosion();
            }
            else if (!hasExplosion)
            {
                for (int i = 0; i < origin.bodyChunks.Length; i++)
                {
                    Vector2 rnv;
                    rnv = new Vector2((Random.value - 0.5f), (Random.value - 0.5f) / 2).normalized;
                    if (!(origin is Player))
                        origin.bodyChunks[i].rad = rads[i] * Custom.LerpMap(counter, 0, maxCount, 1, 2f);
                    origin.bodyChunks[i].vel += rnv * Custom.LerpMap(counter, 0, maxCount, 1, 6f) * Mathf.Clamp(origin.bodyChunks[i].mass, 0.4f, 1.5f);
                    origin.bodyChunks[i].pos.x = pos[i].x;
                }

   
                counter++;
            }
            else
            {
                for (int i = 0; i < origin.bodyChunks.Length; i++)
                    origin.bodyChunks[i].rad = Mathf.Lerp(origin.bodyChunks[i].rad, rads[i], 0.02f);
                if (origin.bodyChunks[0].rad / rads[0]< 1.1f)
                    Destroy();
            }
            
        }
        public override void Destroy()
        {
            if (!hasExplosion)
                Explosion();    

            if(sLeaser != null)
            {
                if (origin is Lizard && (origin as Lizard).graphicsModule != null)
                {
                    var pa = ((origin as Lizard).graphicsModule as LizardGraphics).palette;
                    pa.blackColor =  color[0];
                    ((origin as Lizard).graphicsModule as LizardGraphics).ApplyPalette(sLeaser, room.game.cameras[0], pa);
                }
                else
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                        sLeaser.sprites[i].color = color[i];
                }
            }
            for (int i = 0; i < origin.bodyChunks.Length; i++)
                origin.bodyChunks[i].rad = rads[i];

            origin = null;
            base.Destroy();
        }

        public void Explosion()
        {
            hasExplosion = true;
            for (int i = 0; i < origin.bodyChunks.Length; i++)
            {
                origin.bodyChunks[i].pos.x = pos[i].x;
                origin.bodyChunks[i].vel = Vector2.zero;
            }
            WandererMod.Log("Parasite explode from " + origin.abstractCreature.ID);
            var hasAdult = Random.value < 0.3f;
            if (hasAdult)
            {
                AbstractCreature abstractCreature = new AbstractCreature(origin.abstractCreature.world, StaticWorld.GetCreatureTemplate(WandererEnum.Creatures.FemaleParasite), null, origin.abstractCreature.pos, origin.abstractCreature.world.game.GetNewID());
                abstractCreature.state = new HealthState(abstractCreature);
                abstractCreature.RealizeInRoom();

                if (abstractCreature.realizedCreature != null)
                {
                    foreach (var bodychunk in abstractCreature.realizedCreature.bodyChunks)
                    {
                        bodychunk.vel += Custom.RNV() * Random.Range(5f, 20f);
                        bodychunk.pos = origin.mainBodyChunk.pos + bodychunk.vel;
                    }


                }
            }
            int count = Random.Range(1, 3) - (hasAdult ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                AbstractCreature abstractCreature = new AbstractCreature(origin.abstractCreature.world, StaticWorld.GetCreatureTemplate(WandererEnum.Creatures.ChildParasite), null, origin.abstractCreature.pos, origin.abstractCreature.world.game.GetNewID());
                abstractCreature.state = new HealthState(abstractCreature);
                abstractCreature.RealizeInRoom();

                if (abstractCreature.realizedCreature != null)
                {
                    foreach (var bodychunk in abstractCreature.realizedCreature.bodyChunks)
                    {
                        bodychunk.vel += Custom.RNV() * Random.Range(5f, 20f);
                        bodychunk.pos = origin.mainBodyChunk.pos + bodychunk.vel;
                    }

                }
            }
        }
        int counter = 0;
        bool hasExplosion = false;
       

        readonly int maxCount = 200;

        RoomCamera.SpriteLeaser sLeaser;
        float[] rads;
        Vector2[] pos;
        Creature origin;
        Color[] color;

    }
    //TODO : 多人

    class ParasiteDreamNutils : DreamNutils
    {
        public override bool HasDreamThisCycle(RainWorldGame game, bool malnourished)
        {
            foreach(var a in game.Players)
            {
                if (ParasiteHook.Instance().parasiteData.TryGetValue(a, out var data) && data.isParasite)
                    return true;
            }
            return false;
        }
        public override DreamGameSession GetSession(RainWorldGame game, SlugcatStats.Name name)
        {
            return new ParasiteGameSession(game, name, this);
        }
        public override string FirstRoom => "parasitecaveE";
        public override bool HiddenRoomInArena => true;
        public override bool IsSingleWorld => true;
    }

    class ParasiteGameSession : DreamGameSession
    {
        public ParasiteGameSession(RainWorldGame game, SlugcatStats.Name name,DreamNutils ow) : base(game, name,ow)
        {
        }


        public override void Update()
        {
            base.Update();
            if (EndSession) return;

            if (afterDieCounter != -1)
            {
                if ((--afterDieCounter) == 0)
                {
                    game.ExitGame(true, false);
                    EndSession = true;
                }
                return;
            }
            foreach (var player in Players)
            {
                if (!player.state.alive)
                {
                    WandererMod.Log("exit parasite session : player dead");
                    afterDieCounter = 200;
                    return;
                }
            }
            if (!targetCreature.state.alive)
            {
                WandererMod.Log("exit parasite session : parasite dead");
                game.ExitGame(false, false);
                EndSession = true;
                return;
            }
        }

        public override void PostFirstRoomRealized()
        {
            SpawnPlayerInShortCut(0, 0);
            targetCreature = SpawnCreatureInShortCut(WandererEnum.Creatures.MaleParasite, 0, 1);
        }


        AbstractCreature targetCreature;

        int afterDieCounter = -1;

    }
}
