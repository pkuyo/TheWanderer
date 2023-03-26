using BepInEx.Logging;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace Pkuyo.Wanderer.Options
{
    public class WandererOptions : OptionInterface
    {
        public WandererOptions(ManualLogSource log)
        {
            LoungeKeys = new Configurable<KeyCode>[4];
            _LoungeKeys = new OpKeyBinder[4];
            LoungeKeys[0] = config.Bind<KeyCode>("loungeKeyCode1", KeyCode.LeftControl);
            LoungeKeys[1] = config.Bind<KeyCode>("loungeKeyCode2", KeyCode.None);
            LoungeKeys[2] = config.Bind<KeyCode>("loungeKeyCode3", KeyCode.None);
            LoungeKeys[3] = config.Bind<KeyCode>("loungeKeyCode4", KeyCode.None);

            DisableDash = config.Bind<bool>("DisableDashFront", false);
            DisableTamedAIChange = config.Bind<bool>("DisableTamedAIChange", false);
            PreventToolFalling = config.Bind<bool>("PreventToolFalling", true);

            MessionReputationBonus = config.Bind<float>("MessionReputationBonus", 4f);
        }

        public override void Initialize()
        {
            var translator = Custom.rainWorld.inGameTranslator;
            List<OpTab> tabs = new List<OpTab>();
            base.Initialize();

            {
                OpTab opTab = new OpTab(this, translator.Translate("KeyBind"));
                tabs.Add(opTab);
                opTab.AddItems(new UIelement[]
                {
                    new OpLabel(10f, 540f, translator.Translate("The Vanguard"), true) { alignment=FLabelAlignment.Left },
                    new OpLabel(new Vector2(20f, 450f), new Vector2(200f, 24f), translator.Translate("Sprint KeyBind : "), FLabelAlignment.Left, false),
                    new OpRect(new Vector2(10f, 340f), new Vector2(400, 150))
                });
                for (int i = 0; i < 4; i++)
                {
                    _LoungeKeys[i] = new OpKeyBinder(LoungeKeys[i], new Vector2(230f, 450f) + new Vector2(0, -30) * i, new Vector2(120f, 5f), false, OpKeyBinder.BindController.AnyController);
                    opTab.AddItems(new UIelement[]
                    {
                        new OpLabel(new Vector2(150f, 450f) + new Vector2(0,-30) * i, new Vector2(200f, 24f), translator.Translate("Player " +(i+1)+":"), FLabelAlignment.Left, false, null),
                        _LoungeKeys[i]
                    });
                }
            }
            {
                int i = 0;
                OpTab opTab = new OpTab(this, translator.Translate("Gameplay"));
                tabs.Add(opTab);
                opTab.AddItems(new OpLabel(10f, 540f, translator.Translate("The Vanguard"), true) { alignment=FLabelAlignment.Left });

                _DisableDash = new OpCheckBox(DisableDash, new Vector2(300f, 445f) + new Vector2(0, -30) * i);
                opTab.AddItems(new UIelement[]
                {
                     new OpLabel(new Vector2(20f, 450f)+ new Vector2(0,-30) * (i++), new Vector2(200f, 24f), translator.Translate("Disable pre-sprint delay effect"), FLabelAlignment.Left, false, null),
                     _DisableDash
                });

                _DisableTamedAIChange = new OpCheckBox(DisableTamedAIChange, new Vector2(300f, 445f) + new Vector2(0, -30) * i);
                opTab.AddItems(new UIelement[]
                {
                     new OpLabel(new Vector2(20f, 450f)+ new Vector2(0,-30) * (i++), new Vector2(200f, 24f), translator.Translate("Disable tamed lizard AI change"), FLabelAlignment.Left, false, null),
                     _DisableTamedAIChange
                });

                _PreventToolFalling = new OpCheckBox(PreventToolFalling, new Vector2(300f, 445f) + new Vector2(0, -30) * i);
                opTab.AddItems(new UIelement[]
                {
                     new OpLabel(new Vector2(20f, 450f)+ new Vector2(0,-30) * (i++), new Vector2(200f, 24f), translator.Translate("Prevent Tools from Falling when Climbing Walls"), FLabelAlignment.Left, false, null),
                     _PreventToolFalling
                });

                i++;
                _MessionReputationBonus = new OpFloatSlider(MessionReputationBonus, new Vector2(300f, 445f) + new Vector2(0, -30) * i, 200);
                _MessionReputationBonus.max = 10;
                _MessionReputationBonus.min = 1;
                opTab.AddItems(new UIelement[]
                {
                     new OpLabel(new Vector2(20f, 450f)+ new Vector2(0,-30) * (i++), new Vector2(200f, 24f), translator.Translate("in Mession Lizards Reputation Bonus"), FLabelAlignment.Left, false, null),
                     _MessionReputationBonus
                });

     

            }
            this.Tabs = tabs.ToArray();


        }
        public readonly OpKeyBinder[] _LoungeKeys;
        public readonly Configurable<KeyCode>[] LoungeKeys;

        public OpCheckBox _DisableDash;
        public readonly Configurable<bool> DisableDash;

        public OpFloatSlider _MessionReputationBonus;
        public readonly Configurable<float> MessionReputationBonus;

        public OpCheckBox _DisableTamedAIChange;
        public readonly Configurable<bool> DisableTamedAIChange;

        public OpCheckBox _PreventToolFalling;
        public readonly Configurable<bool> PreventToolFalling;
    }
}
