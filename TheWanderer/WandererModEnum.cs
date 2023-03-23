using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pkuyo.Wanderer
{
    static class WandererModEnum
    {

        static private bool isReg = false;

        static public void RegisterValues()
        {
            if(!isReg)
            {
                PlayerBodyModeIndex.RegisterValues();
                WandererSSOracle.RegisterValues();
                WandererConversation.RegisterValues();
                isReg = true;
            }
        }

        static public void UnRegisterValues()
        {
            if (isReg)
            {
                PlayerBodyModeIndex.UnregisterValues();
                WandererSSOracle.UnregisterValues();
                WandererConversation.UnregisterValues();
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

        static public class WandererWinState
        {
           // static public WinState.EndgameID 
        }

        static public class WandererSSOracle
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
                ThrowOut_Wanderer = new SSOracleBehavior.SubBehavior.SubBehavID("ThrowOut_Wanderer",true);

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

        public class WandererConversation
        {
            public static Conversation.ID Pebbles_Wanderer_FirstMeet_Talk1;
            public static Conversation.ID Pebbles_Wanderer_FirstMeet_Talk2;
            public static Conversation.ID Pebbles_Wanderer_AfterWorkMet;

            public static void RegisterValues()
            {
                Pebbles_Wanderer_FirstMeet_Talk1 = new Conversation.ID("Pebbles_Wanderer_FirstMeet_Talk1", true);
                Pebbles_Wanderer_FirstMeet_Talk2 = new Conversation.ID("Pebbles_Wanderer_FirstMeet_Talk2", true);
                Pebbles_Wanderer_AfterWorkMet = new Conversation.ID("Pebbles_Wanderer_AfterWorkMet", true);

            }

            public static void UnregisterValues()
            {

                if (Pebbles_Wanderer_FirstMeet_Talk1 != null) { Pebbles_Wanderer_FirstMeet_Talk1.Unregister(); Pebbles_Wanderer_FirstMeet_Talk1 = null; }
                if (Pebbles_Wanderer_FirstMeet_Talk2 != null) { Pebbles_Wanderer_FirstMeet_Talk2.Unregister(); Pebbles_Wanderer_FirstMeet_Talk2 = null; }

                if (Pebbles_Wanderer_AfterWorkMet != null) { Pebbles_Wanderer_AfterWorkMet.Unregister(); Pebbles_Wanderer_AfterWorkMet = null; }
            }
        }
    }
}
