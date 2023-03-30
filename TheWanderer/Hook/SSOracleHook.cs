using BepInEx.Logging;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer
{
    class SSOracleHook : HookBase
    {
        SSOracleHook(ManualLogSource log) : base(log)
        {

        }

        static public SSOracleHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new SSOracleHook(log);
            return _instance;
        }
        public override void OnModsInit(RainWorld rainWorld)
        {
            On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.HandTowardsPlayer += SSOracleBehavior_HandTowardsPlayer;

            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        }

        private bool SSOracleBehavior_HandTowardsPlayer(On.SSOracleBehavior.orig_HandTowardsPlayer orig, SSOracleBehavior self)
        {
            var re = orig(self);
            if (self.killFac > 0 || self.currSubBehavior.ID == WandererModEnum.WandererSSOracle.ThrowOut_Wanderer)
                re = true;
            return re;
        }

        private void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            orig(self);
            if (self.id == WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet_Talk1)
            {
                self.events.Clear();
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 30));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I must be crazy, but I have to try..."), 100));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Little creature, take this and go west inside my body."), 20));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It can unlock the door to the Phase Filter Unit."), 30));
            }
            else if (self.id == WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet_Talk2)
            {
                self.events.Clear();
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Help me reconnect to an important part that is recently rotten, So I may still be able to turn the tide!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You can achieve your mission with this too, can't you?"), 30));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Now hurry up!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("[The Phase Filter Unit is still WIP, so you can directly take the tool to open the gate above Five Pebbles or Moon]"), 0));
            }
        }

        private void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {

            bool seePeople = false;
            foreach (var player in self.oracle.room.game.Players)
            {
                Player realizePlayer = player.realizedCreature as Player;
                if (realizePlayer != null)
                {
                    seePeople = true;
                }
            }
            if (seePeople && self.oracle.room.game.session.characterStats.name.value == WandererCharacterMod.WandererName)
            {
                _log.LogDebug("Conversation " + self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
                //我们已经讨论过了（
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 3)
                {
                    self.NewAction(WandererModEnum.WandererSSOracle.ThrowOutWanderer);
                }
                //还没对话呢
                else if (self.action != WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill
                    || self.action != WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
                {
                    if (self.timeSinceSeenPlayer < 0)
                    {
                        self.timeSinceSeenPlayer = 0;
                    }
                    //不关工作状态 没空
                    self.oracle.room.PlaySound(SoundID.SS_AI_Exit_Work_Mode, 0f, 1f, 1f);
                    if (self.oracle.graphicsModule != null)
                    {
                        (self.oracle.graphicsModule as OracleGraphics).halo.ChangeAllRadi();
                        (self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 1f;
                    }
                    self.TurnOffSSMusic(true);

                    if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0 && self.currSubBehavior.ID != WandererModEnum.WandererSSOracle.Meet_Wanderer)
                    {
                        self.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill);
                    }

                }
            }
            else
            {
                orig(self);
            }


        }

        private void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
        {
            _log.LogDebug(nextAction.value);
            if (nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill
                || nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Talk
                || nextAction == WandererModEnum.WandererSSOracle.ThrowOutWanderer
                || nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_GiveObject)
            {
                var behaviorID = SSOracleBehavior.SubBehavior.SubBehavID.ThrowOut;

                if (nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill
                    || nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Talk
                    || nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_GiveObject)
                {
                    behaviorID = WandererModEnum.WandererSSOracle.Meet_Wanderer;
                }
                else if (nextAction == WandererModEnum.WandererSSOracle.ThrowOutWanderer)
                {
                    behaviorID = WandererModEnum.WandererSSOracle.ThrowOut_Wanderer;
                }
                self.inActionCounter = 0;
                self.action = nextAction;
                if (self.currSubBehavior.ID == behaviorID)
                {
                    self.currSubBehavior.Activate(self.action, nextAction);
                    return;
                }
                //lookup行为
                SSOracleBehavior.SubBehavior subBehavior = null;
                for (int i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == behaviorID)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    if (behaviorID == WandererModEnum.WandererSSOracle.ThrowOut_Wanderer)
                        subBehavior = new SSOracleThrowOutWanderer(self);
                    else if (behaviorID == WandererModEnum.WandererSSOracle.Meet_Wanderer)
                        subBehavior = new SSOracleMeetWanderer(self);

                    self.allSubBehaviors.Add(subBehavior);
                }

                subBehavior.Activate(self.action, nextAction);
                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;
            }
            else
            {
                orig(self, nextAction);
            }

        }

        static private SSOracleHook _instance;

        public class SSOracleThrowOutWanderer : SSOracleBehavior.TalkBehavior
        {
            public SSOracleThrowOutWanderer(SSOracleBehavior owner) : base(owner, WandererModEnum.WandererSSOracle.ThrowOut_Wanderer)
            {
            }

            public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
            {
                base.NewAction(oldAction, newAction);
                PickUnfortunatelyFpTime();

                foreach (var a in oracle.room.drawableObjects)
                {
                    if (a is CoolObject)
                        coolObject = a as CoolObject;
                }
            }

            public override void Update()
            {
                base.Update();
                owner.UnlockShortcuts();
                if (player == null) return;
                if (panicObject != null && panicObject.slatedForDeletetion)
                    panicObject = null;

                
                //if
                //TODO: 一打对话
                if (owner.throwOutCounter == 700)
                    dialogBox.Interrupt(Translate("That's all. You'll have to go now."), 0);
                else if (owner.throwOutCounter == 980)
                    dialogBox.Interrupt(Translate("LEAVE."), 0);        
                else if (owner.throwOutCounter == 1530)
                    dialogBox.Interrupt(Translate("Little creature. This is your last warning."), 0);
                
                if (player.room == oracle.room)
                {
                    owner.throwOutCounter++;
                }
                if ((owner.playerOutOfRoomCounter > 100 && owner.throwOutCounter > 400) || owner.throwOutCounter > 3200)
                {
                    owner.NewAction(SSOracleBehavior.Action.General_Idle);
                    owner.getToWorking = 1f;
                    return;
                }



                Vector2 vector = new Vector2(403, 655);

                player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, vector) * 0.2f * (1f - oracle.room.gravity) * Mathf.InverseLerp(140f, 200f, inActionCounter);
                if (coolObject != null)
                    coolObject.firstChunk.vel += Custom.DirVec(coolObject.firstChunk.pos, vector) * 0.2f * (1f - oracle.room.gravity) * Mathf.InverseLerp(140f, 200f, inActionCounter);



                if (owner.throwOutCounter > 1780)
                {
                    owner.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                }  
                panicTimer++;
                if (panicObject == null)
                {
                    //缺少fp痛苦呻吟.jpg
                    if (panicTimer >= timeUntilNextPanic)
                    {
                        PickUnfortunatelyFpTime();
                        panicTimer = 0;
                        panicObject = new FpPanicDisplay(oracle);
                        oracle.room.AddObject(panicObject);
                    }
                }
            }

            public override void Deactivate()
            {
                base.Deactivate();
                if (panicObject != null)
                {
                    panicObject.slatedForDeletetion = true;
                    panicObject = null;
                }
            }
            public void PickUnfortunatelyFpTime()
            {
                timeUntilNextPanic = Random.Range(800, 2400);
            } 
            private int timeUntilNextPanic = 0;
            private int panicTimer = 0;
            private FpPanicDisplay panicObject;

            private CoolObject coolObject;
        }
        public class SSOracleMeetWanderer : SSOracleBehavior.ConversationBehavior
        {
            public SSOracleMeetWanderer(SSOracleBehavior owner) : base(owner, WandererModEnum.WandererSSOracle.Meet_Wanderer, WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet_Talk1)
            {
            }



            public override void Update()
            {
                if (player == null) return;

                if (panicObject != null && panicObject.slatedForDeletetion)
                    panicObject = null;


                //功能判断
                if (action == WandererModEnum.WandererSSOracle.MeetWanderer_GiveObject)
                {
                    owner.LockShortcuts();
                    giveTimer++;
                    if (coolObject != null)
                    {
                        if (coolObject.IsVis)
                            coolObject.FlyTarget = owner.player;
                        if (giveTimer >= 300 || (coolObject.grabbedBy.Count != 0 && coolObject.grabbedBy[0].grabber is Player))
                        {
                            coolObject.FlyTarget = null;
                            coolObject = null;
                            if (!endPage)
                            {
                                owner.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_Talk);
                                endPage = true;
                            }
                        }
                    }
                }


                else if (action == WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill)
                {
                    killDelay--;

                    owner.LockShortcuts();
                    if (panicObject == null && !endPage)
                    {
                        if (owner.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                        {
                            owner.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_Talk);
                        }
                        else
                        {
                            owner.afterGiveMarkAction = WandererModEnum.WandererSSOracle.MeetWanderer_Talk;
                            owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
                        }
                        endPage = true;
                        return;

                    }
                    else if (killDelay <= 0)
                        owner.killFac += 0.015f;

                }
                //谈话 三段对话公用
                else if (action == WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
                {

                    //说完话后根据对话次数判断下一事件
                    if (owner.conversation == null || owner.conversation.slatedForDeletion == true)
                    {
                        if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 2)
                            owner.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_GiveObject);

                        if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 3)
                        {
                            owner.UnlockShortcuts();
                            owner.NewAction(WandererModEnum.WandererSSOracle.ThrowOutWanderer);
                        }
                    }

                }
            }

            public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
            {
                base.NewAction(oldAction, newAction);

                //判断初始化需要什么...
                if (newAction == WandererModEnum.WandererSSOracle.MeetWanderer_Tend_Kill)
                {
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                    panicObject = new FpPanicDisplay(oracle);
                    oracle.room.AddObject(panicObject);
                    oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad = 1;
                }
                else if (newAction == WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
                {
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                    if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 1)
                        owner.InitateConversation(WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet_Talk1, this);

                    else if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 2)
                        owner.InitateConversation(WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet_Talk2, this);

                    else if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 100)//做完工作了
                        owner.InitateConversation(WandererModEnum.WandererConversation.Pebbles_Wanderer_AfterWorkMet, this);

                    oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                }
                else if (newAction == WandererModEnum.WandererSSOracle.MeetWanderer_GiveObject)
                {
                    giveTimer = 0;
                    AbstractCoolObject abstractCoolObject = new AbstractCoolObject(oracle.room.game.world, oracle.room.GetWorldCoordinate(new Vector2(base.oracle.room.PixelWidth / 2f, base.oracle.room.PixelHeight / 2f)), oracle.room.game.GetNewID());
                    abstractCoolObject.SSRealize();
                    coolObject = abstractCoolObject.realizedObject as CoolObject;
                    coolObject.PlaceInRoom(oracle.room);

                    oracle.room.abstractRoom.AddEntity(abstractCoolObject);
                    oracle.room.AddObject(new ShowObjectSprite(coolObject));
                }
                endPage = false;
            }

            public override void Deactivate()
            {
                base.Deactivate();
                owner.UnlockShortcuts();
                if (panicObject != null)
                    panicObject = null;
                giveTimer = 0;

                killDelay = 65;

            }
            private FpPanicDisplay panicObject;

            private int giveTimer = 0;
            private CoolObject coolObject;

            private int killDelay = 65;

            private bool endPage = false;


        }
    }

    public class ShowObjectSprite : CosmeticSprite
    {
        public ShowObjectSprite(CoolObject @object)
        {
            if (@object == null)
                Destroy();
            focusObject = @object;
        }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLight"];
            rCam.ReturnFContainer("Shortcuts").AddChild(sLeaser.sprites[0]);

            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
                return;
            sLeaser.sprites[0].x = Mathf.Lerp(focusObject.firstChunk.lastPos.x, focusObject.firstChunk.pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(focusObject.firstChunk.lastPos.y, focusObject.firstChunk.pos.y, timeStacker) - camPos.y;
            float f = Mathf.Lerp(lastShowFac, showFac, timeStacker);

            sLeaser.sprites[0].scale = Mathf.Lerp(200f, 2f, Mathf.Pow(f, 0.5f));
            sLeaser.sprites[0].alpha = Mathf.Pow(f, 3f);

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void Update(bool eu)
        {
            if (showFac >= 1)
            {
                focusObject.AddProjectedCircle();
                focusObject = null;
                slatedForDeletetion = true;
            }
            base.Update(eu);
            lastShowFac = showFac;
            showFac += 0.02f;
            showFac = showFac > 1 ? 1 : showFac;
        }
        float showFac = 0;
        float lastShowFac = 0;
        CoolObject focusObject;
    }


    public class FpPanicDisplay : UpdatableAndDeletable
    {
        public FpPanicDisplay(Oracle oracle)
        {
            this.oracle = oracle;
            timings = new int[]
            {
                120,
                200,
                320,
                520
            };
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            timer++;
            if (timer == 1)
            {
                gravOn = true;
                if (oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) == null)
                {
                    oracle.room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.DarkenLights, 0f, false));
                }
                if (oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness) == null)
                {
                    oracle.room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0f, false));
                }
                if (oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) == null)
                {
                    oracle.room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Contrast, 0f, false));
                }
                oracle.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_Off, 0f, 1f, 1f);
            }
            if (timer < timings[0])
            {
                float t = timer / (float)timings[0];
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0f, 1f, t);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness).amount = Mathf.Lerp(0f, 0.4f, t);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0f, 0.3f, t);
            }
            if (timer == timings[0])
            {
                var behavior = oracle.oracleBehavior as SSOracleBehavior;
                if (behavior != null)
                    behavior.killFac = 0;
                oracle.arm.isActive = false;
                oracle.setGravity(0.9f);
                oracle.stun = 9999;
                oracle.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Moon_Panic_Attack, 0f, 0.85f, 1f);
                for (int i = 0; i < oracle.room.game.cameras.Length; i++)
                {
                    if (oracle.room.game.cameras[i].room == oracle.room && !oracle.room.game.cameras[i].AboutToSwitchRoom)
                    {
                        oracle.room.game.cameras[i].ScreenMovement(null, Vector2.zero, 15f);
                    }
                }
            }
            if (timer == (timings[1] + timings[2]) / 2)
            {
                oracle.arm.isActive = false;
                this.room.PlaySound(SoundID.SS_AI_Talk_1, 0f, 0.5f, 1f);
                //oracle.room.PlaySound((Random.value < 0.5f) ? SoundID.SL_AI_Pain_1 : SoundID.SL_AI_Pain_2, 0f, 0.5f, 1f);
                chatLabel = new OracleChatLabel(oracle.oracleBehavior);
                chatLabel.pos = new Vector2(485f, 360f);
                chatLabel.NewPhrase(99);
                oracle.setGravity(0.9f);
                oracle.stun = 9999;
                oracle.room.AddObject(chatLabel);
            }
            if (timer > timings[1] && timer < timings[2] && timer % 16 == 0)
            {
                oracle.room.ScreenMovement(null, new Vector2(0f, 0f), 2.5f);
                for (int j = 0; j < 6; j++)
                {
                    if (Random.value < 0.5f)
                    {
                        oracle.room.AddObject(new OraclePanicDisplay.PanicIcon(new Vector2(Random.Range(230, 740), Random.Range(100, 620))));
                    }
                }
            }
            if (timer >= timings[2] && timer <= timings[3])
            {
                oracle.room.ScreenMovement(null, new Vector2(0f, 0f), 1f);
            }
            if (timer == timings[3])
            {
                chatLabel.Destroy();
                oracle.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1f, 1f);
                gravOn = false;
            }
            if (timer > timings[3])
            {
                float t2 = (timer - timings[3]) / (float)timings[0];
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(1f, 0f, t2);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness).amount = Mathf.Lerp(0.4f, 0f, t2);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0.3f, 0f, t2);
            }
            if (timer == timings[3] + timings[0])
            {
                oracle.setGravity(0f);
                oracle.arm.isActive = true;
                oracle.stun = 0;
                Destroy();
            }
        }

        public Oracle oracle;

        public int timer;

        private readonly int[] timings;

        public bool gravOn;


        public OracleChatLabel chatLabel;
    }

}
