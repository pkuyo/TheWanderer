using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pkuyo.Wanderer
{
    class WandererModEnum
    {
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
    }
}
