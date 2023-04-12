using Menu;
using static MultiplayerUnlocks;

namespace Pkuyo.Wanderer
{
    static class WandererModEnum
    {

        static private bool isReg = false;

        static public void RegisterValues()
        {
            if (!isReg)
            {
                PlayerBodyModeIndex.RegisterValues();
                SSOracle.RegisterValues();
                Conversation.RegisterValues();
                WinState.RegisterValues();
                Creatures.RegisterValues();
                Scene.RegisterValues();
                Objects.RegisterValues();
                Sandbox.RegisterValues();   
                isReg = true;
            }
        }

        static public void UnRegisterValues()
        {
            if (isReg)
            {
                PlayerBodyModeIndex.UnregisterValues();
                SSOracle.UnregisterValues();
                Conversation.UnregisterValues();
                WinState.UnregisterValues();
                Creatures.UnregisterValues();
                Scene.UnregisterValues();
                Objects.UnregisterValues();
                Sandbox.UnregisterValues();
                isReg = false;
            }
        }


        static public class PlayerBodyModeIndex
        {
            static public Player.BodyModeIndex ClimbBackWall;

            public static void RegisterValues()
            {
                ClimbBackWall = new Player.BodyModeIndex("ClimbBackWall", true);
            }

            public static void UnregisterValues()
            {
                if (ClimbBackWall != null) { ClimbBackWall.Unregister(); ClimbBackWall = null; }
            }

        }

        static public class WinState
        {
            static public global::WinState.EndgameID Dragonlord;
            static public int PassageDragonlord = 51;

            public static void RegisterValues()
            {
                Dragonlord = new global::WinState.EndgameID("Dragonlord", true);
            }
            public static void UnregisterValues()
            {
                if (Dragonlord != null) { Dragonlord.Unregister(); Dragonlord = null; }
            }
        }

        static public class SSOracle
        {
            static public SSOracleBehavior.SubBehavior.SubBehavID Meet_Wanderer;
            static public SSOracleBehavior.SubBehavior.SubBehavID ThrowOut_Wanderer;

            static public SSOracleBehavior.Action MeetWanderer_Talk;
            static public SSOracleBehavior.Action MeetWanderer_Tend_Kill;
            static public SSOracleBehavior.Action MeetWanderer_GiveObject;
            static public SSOracleBehavior.Action ThrowOutWanderer;


            public static void RegisterValues()
            {
                Meet_Wanderer = new SSOracleBehavior.SubBehavior.SubBehavID("Meet_Wanderer", true);
                ThrowOut_Wanderer = new SSOracleBehavior.SubBehavior.SubBehavID("ThrowOut_Wanderer", true);

                MeetWanderer_Tend_Kill = new SSOracleBehavior.Action("MeetWanderer_Tend_Kill", true);
                MeetWanderer_Talk = new SSOracleBehavior.Action("MeetWanderer_Talk", true);
                MeetWanderer_GiveObject = new SSOracleBehavior.Action("MeetWanderer_GiveObject", true);

                ThrowOutWanderer = new SSOracleBehavior.Action("ThrowOutWanderer", true);

            }

            public static void UnregisterValues()
            {
                if (Meet_Wanderer != null) { Meet_Wanderer.Unregister(); Meet_Wanderer = null; }

                if (MeetWanderer_Tend_Kill != null) { MeetWanderer_Tend_Kill.Unregister(); MeetWanderer_Tend_Kill = null; }
                if (MeetWanderer_Talk != null) { MeetWanderer_Talk.Unregister(); MeetWanderer_Talk = null; }
                if (MeetWanderer_GiveObject != null) { MeetWanderer_GiveObject.Unregister(); MeetWanderer_GiveObject = null; }

                if (ThrowOutWanderer != null) { ThrowOutWanderer.Unregister(); ThrowOutWanderer = null; }



            }
        }

        static public class Scene
        {
            public static SlideShow.SlideShowID WandererIntro;
            public static MenuScene.SceneID Intro_W1;
            public static MenuScene.SceneID Intro_W2;
            public static void RegisterValues()
            {
                WandererIntro = new SlideShow.SlideShowID("WandererIntro", true);
                Intro_W1 = new MenuScene.SceneID("Intro_W1", true);
                Intro_W2 = new MenuScene.SceneID("Intro_W2", true);
            }
            public static void UnregisterValues()
            {
                if (WandererIntro != null) { WandererIntro.Unregister(); WandererIntro = null; }
                if (Intro_W1 != null) { Intro_W1.Unregister(); Intro_W1 = null; }
                if (Intro_W2 != null) { Intro_W2.Unregister(); Intro_W2 = null; }
            }
        }
        static public class Conversation
        {
            public static global::Conversation.ID Pebbles_Wanderer_FirstMeet_Talk1;
            public static global::Conversation.ID Pebbles_Wanderer_FirstMeet_Talk2;
            public static global::Conversation.ID Pebbles_Wanderer_AfterWorkMet;

            public static void RegisterValues()
            {
                Pebbles_Wanderer_FirstMeet_Talk1 = new global::Conversation.ID("Pebbles_Wanderer_FirstMeet_Talk1", true);
                Pebbles_Wanderer_FirstMeet_Talk2 = new global::Conversation.ID("Pebbles_Wanderer_FirstMeet_Talk2", true);
                Pebbles_Wanderer_AfterWorkMet = new global::Conversation.ID("Pebbles_Wanderer_AfterWorkMet", true);

            }

            public static void UnregisterValues()
            {

                if (Pebbles_Wanderer_FirstMeet_Talk1 != null) { Pebbles_Wanderer_FirstMeet_Talk1.Unregister(); Pebbles_Wanderer_FirstMeet_Talk1 = null; }
                if (Pebbles_Wanderer_FirstMeet_Talk2 != null) { Pebbles_Wanderer_FirstMeet_Talk2.Unregister(); Pebbles_Wanderer_FirstMeet_Talk2 = null; }

                if (Pebbles_Wanderer_AfterWorkMet != null) { Pebbles_Wanderer_AfterWorkMet.Unregister(); Pebbles_Wanderer_AfterWorkMet = null; }
            }
        }

        static public class Creatures
        {
            public static CreatureTemplate.Type ToxicSpider;
            public static void RegisterValues()
            {
                ToxicSpider = new CreatureTemplate.Type("ToxicSpider",true);

            }
            public static void UnregisterValues()
            {
                if(ToxicSpider!=null) { ToxicSpider.Unregister(); ToxicSpider = null; }
            }
        }
        static public class Objects
        {
            static public AbstractPhysicalObject.AbstractObjectType CoolObject;
            static public AbstractPhysicalObject.AbstractObjectType PoisonNeedle;
            static public void RegisterValues()
            {
                CoolObject = new AbstractPhysicalObject.AbstractObjectType("CoolObject", true);
                PoisonNeedle = new AbstractPhysicalObject.AbstractObjectType("PoisonNeedle", true);
            }

            static public void UnregisterValues()
            {
                if (CoolObject != null) { CoolObject.Unregister(); CoolObject = null; }
                if (PoisonNeedle != null) { PoisonNeedle.Unregister(); PoisonNeedle = null; }
            }
        }

        static public class Sandbox
        {
            static public SandboxUnlockID ToxicSpider;
            static public SandboxUnlockID CoolObject;
            static public void RegisterValues()
            {
                ToxicSpider = new SandboxUnlockID("ToxicSpider", true);
                CoolObject = new SandboxUnlockID("CoolObject", true);
            }
            static public void UnregisterValues()
            {
                if (ToxicSpider != null) { ToxicSpider.Unregister(); ToxicSpider = null; }
                if(CoolObject != null) { CoolObject.Unregister(); CoolObject = null; }
            }
        }
    }
}
