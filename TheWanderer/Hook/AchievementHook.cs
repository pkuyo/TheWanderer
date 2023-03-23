using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pkuyo.Wanderer
{
    class AchievementHook : HookBase
    {
        AchievementHook(ManualLogSource log) : base(log)
        {

        }

        static public AchievementHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new AchievementHook(log);
            return _instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            //On.
        }

        static private AchievementHook _instance;
    }
}
