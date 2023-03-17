using BepInEx.Logging;
using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        }

        private void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            orig(self);
            if (self.id == WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 30));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I must be crazy, but I have to try..."), 100));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Little creature, take this and go west inside my body."), 20));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It can unlock the door to the Phase Filter Unit."),30));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Help me reconnect to an important part that is recently rotten, So I may still be able to turn the tide!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You can achieve your mission with this too, can't you?"), 30));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Now hurry up!"), 0));



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
            if (seePeople && self.oracle.room.game.session.characterStats.name.value == "wanderer")
            {
                if (self.action != WandererModEnum.WandererSSOracle.MeetWanderer_Kill
                    || self.action != WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
                {
                    if (self.timeSinceSeenPlayer < 0)
                    {
                        self.timeSinceSeenPlayer = 0;
                    }

                    self.SlugcatEnterRoomReaction();
                    if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
                    {
                        self.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_Kill);
                    }
                    else
                    {
                        self.NewAction(WandererModEnum.WandererSSOracle.MeetWanderer_Talk);
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
            if (nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Kill || nextAction == WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
            {
      
                if (self.currSubBehavior.ID == WandererModEnum.WandererSSOracle.Meet_Wanderer)
                {
                    self.currSubBehavior.Activate(self.action, nextAction);
                    self.inActionCounter = 0;
                    self.action = nextAction;
                    return;
                }
                self.inActionCounter = 0;
                self.action = nextAction;
                //lookup行为
                SSOracleBehavior.SubBehavior subBehavior = null;
                for (int i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == WandererModEnum.WandererSSOracle.Meet_Wanderer)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    subBehavior = new SSOracleMeetWanderer(self);
                    self.allSubBehaviors.Add(subBehavior);
                }

                subBehavior.Activate(self.action, nextAction);
                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;
            }
            //友善的撇出去
            else if (nextAction == WandererModEnum.WandererSSOracle.ThrowOutWanderer)
            {
                if (self.currSubBehavior.ID == WandererModEnum.WandererSSOracle.ThrowOut_Wanderer) return;
                
                self.inActionCounter = 0;
                self.action = nextAction;
                //lookup行为
                SSOracleBehavior.SubBehavior subBehavior = null;
                for (int i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == WandererModEnum.WandererSSOracle.ThrowOut_Wanderer)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    subBehavior = new SSOracleThrowOutWanderer(self);
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
            public SSOracleThrowOutWanderer(SSOracleBehavior owner) : base(owner, WandererModEnum.WandererSSOracle.Meet_Wanderer)
            {
            }

            public override void Update()
            {
                base.Update();
                owner.UnlockShortcuts();
                if (player == null) return;
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
                if (owner.throwOutCounter < 900)
                {
                    base.player.mainBodyChunk.vel *= Mathf.Lerp(0.9f, 1f, base.oracle.room.gravity);
                    base.player.bodyChunks[1].vel *= Mathf.Lerp(0.9f, 1f, base.oracle.room.gravity);
                    base.player.mainBodyChunk.vel += Custom.DirVec(base.player.mainBodyChunk.pos, new Vector2(base.oracle.room.PixelWidth / 2f, base.oracle.room.PixelHeight / 2f)) * 0.5f * (1f - base.oracle.room.gravity);
                }
            }
        }
        public class SSOracleMeetWanderer : SSOracleBehavior.ConversationBehavior
        {
            public SSOracleMeetWanderer(SSOracleBehavior owner) : base(owner, WandererModEnum.WandererSSOracle.Meet_Wanderer, WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet)
            {
            }

            public void PickUnfortunatelyFpTime()
            {
                timeUntilNextPanic = Random.Range(800, 2400);
            }

            public override void Update()
            {
                if(action!=null)
                    SSOracleHook.Instance()._log.LogError(action.value);
                if (player == null) return;

                if (panicObject != null && panicObject.slatedForDeletetion)
                    panicObject = null;

                if (action == WandererModEnum.WandererSSOracle.MeetWanderer_Kill)
                {
                    killDelay--;
                    //没初始化文本的话..
                    if (!initMessageKill && !dialogBox.ShowingAMessage && dialogBox.messages.Count == 0)
                    {
                        
                        if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 1)
                        {
                            dialogBox.NewMessage(". . .", 50);
                        }

                        initMessageKill = true;
                        panicObject = new FpPanicDisplay(oracle);
                        oracle.room.AddObject(panicObject);
                        oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad = 1;
                    }

                    if (!initKill && killDelay <= 0)
                        owner.killFac += 0.015f;
                    else if (panicObject == null)
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
                        return;
                        
                    }

                    if (owner.killFac >= 0.9)
                    {
                        initKill = true;
                        owner.killFac = 0;
                    }

                }
                else if (action == WandererModEnum.WandererSSOracle.MeetWanderer_Talk)
                {
                    panicTimer++;
                    if (initMessageTalk && panicObject == null)
                    {
                        //缺少fp痛苦呻吟.jpg
                        if (panicTimer >= timeUntilNextPanic)
                        {
                            PickUnfortunatelyFpTime();
                            panicTimer = 0;
                            panicObject = new FpPanicDisplay(oracle);
                            oracle.room.AddObject(panicObject);
                            if (owner.conversation != null)
                            {
                                owner.conversation.Interrupt("...", 600);
                                owner.conversation.paused = true;
                            }
                        }
                        else if (owner.conversation != null && owner.conversation.paused) owner.conversation.paused = false;
                    }

                    //没初始化文本的话..
                    if (!initMessageTalk && !dialogBox.ShowingAMessage && dialogBox.messages.Count == 0)
                    {
                        if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 1)
                        {
                            owner.InitateConversation(WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet, this);
                            oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                        }
                        else
                        {
                            owner.InitateConversation(WandererModEnum.WandererConversation.Pebbles_Wanderer_FirstMeet, this);
                        }
                        initMessageTalk = true;
                    }
                    //话说完了
                    else if (initMessageTalk && (owner.conversation==null || owner.conversation.slatedForDeletion))
                    {
                        owner.NewAction(WandererModEnum.WandererSSOracle.ThrowOutWanderer);
                    }
                }
            }

            public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
            {
                base.NewAction(oldAction, newAction);
                PickUnfortunatelyFpTime();
            }

            public override void Deactivate()
            {
                base.Deactivate();
                if (panicObject != null)
                    panicObject = null;
                initMessageTalk = false;
                panicTimer = 0;

                
                timeUntilNextPanic = 0;
                initKill = false;
                initMessageKill = false;
                killDelay = 65;

            }
            private int timeUntilNextPanic = 0;
            private int panicTimer = 0;
            bool initMessageTalk;

            bool initKill;
            bool initMessageKill;
            int killDelay = 60;
            private FpPanicDisplay panicObject;

        }
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
                float t = (float)timer / (float)timings[0];
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0f, 1f, t);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness).amount = Mathf.Lerp(0f, 0.4f, t);
                oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0f, 0.3f, t);
            }
            if (timer == timings[0])
            {
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
                        oracle.room.AddObject(new OraclePanicDisplay.PanicIcon(new Vector2((float)Random.Range(230, 740), (float)Random.Range(100, 620))));
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
                float t2 = (float)(timer - timings[3]) / (float)timings[0];
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

        private int[] timings;

        public bool gravOn;


        public OracleChatLabel chatLabel;
    }

}
