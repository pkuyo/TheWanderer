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


        public class PlayerBodyModeIndex
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


        public class WandererSSOracle
        {
            static public SSOracleBehavior.SubBehavior.SubBehavID Meet_Wanderer;
            static public SSOracleBehavior.SubBehavior.SubBehavID ThrowOut_Wanderer;

            static public SSOracleBehavior.Action MeetWanderer_Talk;
            static public SSOracleBehavior.Action MeetWanderer_Kill;
            static public SSOracleBehavior.Action ThrowOutWanderer;
            public static void RegisterValues()
            {
                Meet_Wanderer = new SSOracleBehavior.SubBehavior.SubBehavID("Meet_Wanderer", true);
                MeetWanderer_Kill = new SSOracleBehavior.Action("MeetWanderer_Kill", true);
                MeetWanderer_Talk = new SSOracleBehavior.Action("MeetWanderer_Talk", true);
                ThrowOutWanderer = new SSOracleBehavior.Action("ThrowOutWanderer", true);
            }

            public static void UnregisterValues()
            {
                if (Meet_Wanderer != null) { Meet_Wanderer.Unregister(); Meet_Wanderer = null; }
                if (MeetWanderer_Kill != null) { MeetWanderer_Kill.Unregister(); MeetWanderer_Kill = null; }
                if (MeetWanderer_Talk != null) { MeetWanderer_Talk.Unregister(); MeetWanderer_Talk = null; }
                if (ThrowOutWanderer != null) { ThrowOutWanderer.Unregister(); ThrowOutWanderer = null; }

            }
        }

        public class WandererConversation
        {
            public static Conversation.ID Pebbles_Wanderer_FirstMeet;
            public static Conversation.ID Pebbles_Wanderer_AfterMet;
            public static Conversation.ID Pebbles_Wanderer_AfterWorkMet;

            public static void RegisterValues()
            {
                Pebbles_Wanderer_FirstMeet = new Conversation.ID("Pebbles_Wanderer_FirstMeet", true);
                Pebbles_Wanderer_AfterMet = new Conversation.ID("Pebbles_Wanderer_AfterMet", true);
                Pebbles_Wanderer_AfterWorkMet = new Conversation.ID("Pebbles_Wanderer_AfterWorkMet", true);

            }

            public static void UnregisterValues()
            {

                if (Pebbles_Wanderer_FirstMeet != null) { Pebbles_Wanderer_FirstMeet.Unregister(); Pebbles_Wanderer_FirstMeet = null; }
                if (Pebbles_Wanderer_AfterMet != null) { Pebbles_Wanderer_AfterMet.Unregister(); Pebbles_Wanderer_AfterMet = null; }
                if (Pebbles_Wanderer_AfterWorkMet != null) { Pebbles_Wanderer_AfterWorkMet.Unregister(); Pebbles_Wanderer_AfterWorkMet = null; }
            }
        }
    }
}
