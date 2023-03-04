using BepInEx.Logging;
using HUD;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;



namespace Pkuyo.Wanderer.LizardMessage
{
    public class WandererLizard 
    {
        public WandererLizard(Lizard self)
        {
            lizard = self;         
        }
        public void Update()
        {
            //若房间内存在漫游者则可以聆听
            var canListen = false;
            foreach (var player in lizard.room.game.Players)
                if ((player.realizedCreature as Player).slugcatStats.name.value == "wanderer")
                    canListen = true;
            if (!canListen)
                return;

            //计数器触发
            if (InstantCounter != 0) InstantCounter--;
            if (ConstantCounter != 0) ConstantCounter--;

            LizardDialogBox dialog = null;

            //黄蜥蜴无线电通信
            if (lizard.lizardParams.template == CreatureTemplate.Type.YellowLizard)
            {
                if (ConstantCounter == 0)
                {
                    dialog = LizardDialogBox.CreateLizardDialog(lizard, new StatePriority());
                    ConstantCounter = 350 + Random.Range(50, 200);
                }
                return;
            }

            //尝试获取前一次对话框信息
            StatePriority lastPriority = new StatePriority();
            if (dialogBox != null)
                lastPriority = dialogBox.Priority;


            //判断需要即时触发的
            var astate = new StatePriority(lizard.animation);
            var bstate = new StatePriority(lizard.AI.behavior, true);
            var instantState = astate > bstate ? astate : bstate;
            if ((InstantCounter == 0 || instantState > lastPriority) && instantState.priority != -1)
            {
                dialog = LizardDialogBox.CreateLizardDialog(lizard, instantState);
                InstantCounter = 150 + Random.Range(25, 100);
            }
            else
            {
                //判断通常触发的
                var constantState = new StatePriority(lizard.AI.behavior, false);
                if (ConstantCounter == 0)
                {
                    dialog = LizardDialogBox.CreateLizardDialog(lizard, constantState);
                    ConstantCounter = 200 + Random.Range(50, 150);
                }
            }

            //更新对话框
            if (dialog != null)
            {
                if (dialogBox != null)
                {
                    dialogBox.DeleteDialogBox();
                }
                dialogBox = dialog;
            }
        }

        public void Destroy()
        {
            if (dialogBox != null)
                dialogBox.DeleteDialogBox();
        }

        public int InstantCounter = 0;
        public int ConstantCounter = 0;
        private LizardDialogBox dialogBox = null;
        public Lizard lizard;
    }

    class LizardDialogBox : DialogBox
    {
        LizardDialogBox(HUD.HUD hud, RoomCamera camera , Lizard target, string message, StatePriority pri) : base(hud)
        {
            _camera = camera;
            _room = target.abstractCreature.Room.realizedRoom;
            _pos = target.mainBodyChunk.pos +new Vector2(25.0f,25.0f);
            Priority = pri;
            base.NewMessage(message,60);
        }

        public override void Update()
        {
            base.Update();
            if (this._camera.room != this._room || this.messages.Count == 0)
            {
                this.slatedForDeletion = true;
                this._room = null;
                this._camera = null;
                this.messages.Clear();
                return;
            }
        }

        public override void Draw(float timeStacker)
        {
            if (this._camera == null || this._camera.room != this._room)
            {
                this.messages.Clear();
            }
            else if (this.messages.Count > 0)
            {
                Vector2 vector = this._pos - this._camera.pos;
                
                this.messages[0].yPos = vector.y;
                this.messages[0].xOrientation = vector.x / this.hud.rainWorld.screenSize.x;
            }
            base.Draw(timeStacker);
        }

        public void DeleteDialogBox()
        {
            this.messages.Clear();
        }


        private Room _room;
        private Vector2 _pos;
        private RoomCamera _camera;

        public StatePriority Priority;

        /// STATIC PART ///

        static public void InitDialogBoxStaticData(ManualLogSource log)
        {
            _log = log;
        }

        public static LizardDialogBox CreateLizardDialog(Lizard target, StatePriority priority)
        {

            var message = RandomMessagePicker.GetRandomMessage(target, priority);
            //无文本则不响应
            if (message == null) return null;

            var room = target.room;
            var hud = room.game.cameras[0].hud;
            if (hud != null && hud.owner is Player)
            {
                var camera = room.game.cameras[0];
                var followCreature = camera.followAbstractCreature;
                var re = new LizardDialogBox(hud, camera, target, message, priority);
                camera.hud.AddPart(re);
                return re;
            }
            return null;
        }

        private static ManualLogSource _log;

    }

    //对话框创建时的参数
    public class StatePriority
    {
  

        //空置优先级，无实际意义
        public StatePriority()
        {
            priority = -100;
        }

        public StatePriority(LizardAI.Behavior behavior, bool isIns)
        {
            this._behavior = behavior;

            if (isIns)
            {
                if (_behaivornInstantPri.ContainsKey(behavior))
                {
                    this.isInstant = 1;
                    priority = _behaivornInstantPri[behavior];
                }
                else
                    priority = -1;
            }
            else
            {
                if (_behaivornInstantPri.ContainsKey(behavior))
                    priority = _behaivornConstantPri[behavior];
                else
                    priority = -1;
            }
        }

        public StatePriority(Lizard.Animation anim)
        {
            this._anim = anim;

            if (_animInstantPri.ContainsKey(anim))
            {
                this.isInstant = 2;
                priority = _animInstantPri[anim];
            }
            else
                priority = -1;
        }

        public int priority { get; protected set; }
        private int isInstant = 0;

        public LizardAI.Behavior _behavior { get; protected set; }
        public Lizard.Animation _anim { get; protected set; }


        /// STATIC PART ///

        public static void InitStatePriority(ManualLogSource log)
        {
            _log = log;
            _animInstantPri = new Dictionary<Lizard.Animation, int>();
            _behaivornInstantPri = new Dictionary<LizardAI.Behavior, int>();
            _behaivornConstantPri = new Dictionary<LizardAI.Behavior, int>();

            {
                //打开文件-即时触发行为
                var path = AssetManager.ResolveFilePath("text/wanderer/shortlist.json");
                FileStream fileStream = new FileStream(path, FileMode.Open);
                //反序列化Json
                byte[] a = new byte[fileStream.Length];
                fileStream.Read(a, 0, (int)fileStream.Length);
                var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(Encoding.UTF8.GetString(a));
                bool flag = false; //是否到达anim区段
                foreach (var pri in all)
                {
                    if (pri.Value == -1)
                    {
                        flag = true;
                        continue;
                        //更换区段
                    }

                    if (!flag)
                    {
                        LizardAI.Behavior behavior = (LizardAI.Behavior)ExtEnumBase.Parse(typeof(LizardAI.Behavior), pri.Key, false);
                        _behaivornInstantPri.Add(behavior, pri.Value);
                    }
                    else
                    {
                        Lizard.Animation anim = (Lizard.Animation)ExtEnumBase.Parse(typeof(Lizard.Animation), pri.Key, false);
                        _animInstantPri.Add(anim, pri.Value);
                    }
                }
                _log.LogDebug("ShortList loaded");
            }

            {
                //打开文件-长期触发行为
                var path = AssetManager.ResolveFilePath("text/wanderer/longlist.json");
                FileStream fileStream = new FileStream(path, FileMode.Open);
                //反序列化Json
                byte[] a = new byte[fileStream.Length];
                fileStream.Read(a, 0, (int)fileStream.Length);
                var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(Encoding.UTF8.GetString(a));
                foreach (var pri in all)
                {
                    LizardAI.Behavior behavior = (LizardAI.Behavior)ExtEnumBase.Parse(typeof(LizardAI.Behavior), pri.Key, false);
                    _behaivornConstantPri.Add(behavior, pri.Value);

                }
                _log.LogDebug("LongList Loaded");
            }
        }

        static public bool operator <(StatePriority a, StatePriority b)
        {
            if (a.priority != b.priority)
                return a.priority < b.priority;
            else
                return a.isInstant < b.isInstant;
        }
        static public bool operator >(StatePriority a, StatePriority b)
        {
            if (a.priority != b.priority)
                return a.priority > b.priority;
            else
                return a.isInstant > b.isInstant;
        }

        static public bool operator ==(StatePriority a, StatePriority b)
        {
            if (a.priority != b.priority || a._anim != b._anim || (a._anim != b._anim && a._behavior != b._behavior))
                return false;
            return true;
        }
        static public bool operator !=(StatePriority a, StatePriority b)
        {
            return !(a == b);
        }

        private static Dictionary<Lizard.Animation, int> _animInstantPri;
        private static Dictionary<LizardAI.Behavior, int> _behaivornInstantPri;
        private static Dictionary<LizardAI.Behavior, int> _behaivornConstantPri;
        private static ManualLogSource _log;

    }
}
