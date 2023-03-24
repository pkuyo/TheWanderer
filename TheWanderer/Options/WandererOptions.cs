using BepInEx.Logging;
using Menu.Remix.MixedUI;
using RWCustom;
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
        }

        public override void Initialize()
        {
            var translator = Custom.rainWorld.inGameTranslator;
            base.Initialize();
            OpTab opTab = new OpTab(this, "Options");
            this.Tabs = new OpTab[]
            {
                 opTab
            };
            opTab.AddItems(new UIelement[]
            {
                new OpLabel(10f, 540f, translator.Translate("The Vanguard"), true)
                {
                    alignment=FLabelAlignment.Left
                },
                new OpLabel(new Vector2(10f, 450f), new Vector2(200f, 24f), translator.Translate("Lounge KeyBind"), FLabelAlignment.Left, false)
                {
                    size=new Vector2(50,50)
                }

            });
            for (int i = 0; i < 4; i++)
            {
                _LoungeKeys[i] = new OpKeyBinder(LoungeKeys[i], new Vector2(150f, 450f) + new Vector2(0, -30) * i, new Vector2(100f, 20f), false, OpKeyBinder.BindController.AnyController);
                opTab.AddItems(new UIelement[]
                {
                     new OpLabel(new Vector2(100f, 450f) + new Vector2(0,-30) * i, new Vector2(200f, 24f), translator.Translate("Player "+(i+1)), FLabelAlignment.Left, false, null),
                     _LoungeKeys[i]
                });
            }
        }
        public readonly OpKeyBinder[] _LoungeKeys;
        public readonly Configurable<KeyCode>[] LoungeKeys;
    }
}
