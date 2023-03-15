﻿using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pkuyo.Wanderer.Characher
{
    class FeatureBase
    {
        public FeatureBase(ManualLogSource log)
        {
            _log = log;
        }
        protected ManualLogSource _log;

        virtual public void OnModsInit(RainWorld rainWorld)
        {

        }
    }
}
