using BepInEx.Logging;
using HUD;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.LizardMessage
{
    public class WandererLizard
    {
        public WandererLizard(Lizard self)
        {
            lizardRef = new WeakReference<Lizard>(self);
            speakSpeed = Custom.LerpMap(self.abstractCreature.personality.energy * 4, 0, 0.5f, 1.3f, 0.7f);
            ConstantCounter = (int)(Random.Range(0, 200) * speakSpeed);
            InstantCounter = (int)(Random.Range(0, 200) * speakSpeed);

        }

        //false代表程序错误
        public bool Update()
        {
            //若房间内存在漫游者则可以聆听
            var canListen = false;
            Lizard lizard = null;
            if (!lizardRef.TryGetTarget(out lizard))
                return false;

            if (lizard.room == null)
                return false;

            foreach (var player in lizard.room.game.Players)
                if (player.realizedCreature != null && (player.realizedCreature as Player).slugcatStats.name.value == WandererMod.WandererName)
                    canListen = true;

            if (!canListen)
                return true;

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
                    ConstantCounter = (int)((350 + Random.Range(50, 200)) * speakSpeed);
                }
                return true;
            }

            //尝试获取前一次对话框信息
            StatePriority lastPriority = new StatePriority();
            if (dialogBox != null)
                lastPriority = dialogBox.Priority;


            //判断需要即时触发的
            var astate = new StatePriority(lizard.animation, true);
            var bstate = new StatePriority(lizard.AI.behavior, true);
            var instantState = astate > bstate ? astate : bstate;
            if ((InstantCounter == 0 || instantState > lastPriority) && instantState.priority != -1)
            {
                dialog = LizardDialogBox.CreateLizardDialog(lizard, instantState);
                InstantCounter = (int)((150 + Random.Range(25, 200)) * speakSpeed);
            }
            else
            {
                //判断通常触发的
                var castate = new StatePriority(lizard.animation, false);
                var cbstate = new StatePriority(lizard.AI.behavior, false);
                var constantState = castate > cbstate ? castate : cbstate;
                if (ConstantCounter == 0 && constantState.priority != -1)
                {
                    dialog = LizardDialogBox.CreateLizardDialog(lizard, constantState);
                    ConstantCounter = (int)((250 + Random.Range(50, 250)) * speakSpeed);
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
            return true;
        }

        public void Destroy()
        {
            if (dialogBox != null)
                dialogBox.DeleteDialogBox();
        }

        public int InstantCounter = 0;
        public int ConstantCounter = 0;
        private LizardDialogBox dialogBox = null;
        public WeakReference<Lizard> lizardRef;

        public float speakSpeed;

    }

    class LizardDialogBox : DialogBox
    {
        LizardDialogBox(HUD.HUD hud, RoomCamera camera, Lizard target, string message, StatePriority pri) : base(hud)
        {
            _camera = camera;
            _room = target.abstractCreature.Room.realizedRoom;
            _pos = target.mainBodyChunk.pos + new Vector2(Random.Range(-25, 25), 25.0f + Random.Range(-10, 10));
            Priority = pri;

            float dis = 10000f;
            float like = 0;

            if (target.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.YellowLizard)
            {

                //朋友蜥蜴蓝色
                if (target.AI.friendTracker.friend != null && target.AI.friendTracker.friend is Player && target.AI.friendTracker.followClosestFriend)
                    currentColor = new Color(79 / 255f, 208 / 255f, 234 / 255f);
                //根据距离最近的玩家设置好感度
                else if (_room != null)
                {
                    foreach (var player in _room.PlayersInRoom)
                    {
                        if (player.slugcatStats.name.value == WandererMod.WandererName && Custom.DistLess(player.mainBodyChunk.pos, target.mainBodyChunk.pos, dis))
                        {
                            dis = Custom.Dist(player.mainBodyChunk.pos, target.mainBodyChunk.pos);
                            like = LikeOfPlayer(target, player);
                        }
                    }
                    var HSLColor = Vector3.Lerp(Custom.RGB2HSL(Color.red), Custom.RGB2HSL(Color.green), like);
                    currentColor =  Custom.HSL2RGB(HSLColor.x, HSLColor.y, HSLColor.z);
                }
            }

            NewMessage(message, 60);

        }

        public float LikeOfPlayer(Lizard lizard, Player player)
        {
            var ai = lizard.AI;
            if (player == null)
            {
                return 0f;
            }
            float num = ai.creature.world.game.session.creatureCommunities.LikeOfPlayer(ai.creature.creatureTemplate.communityID, ai.creature.world.RegionNumber, (player.State as PlayerState).playerNumber);
            num = Mathf.Lerp(num, -lizard.spawnDataEvil, Mathf.Abs(lizard.spawnDataEvil));
            float tempLike = ai.creature.state.socialMemory.GetTempLike(player.abstractCreature.ID);
            num = Mathf.Lerp(num, tempLike, Mathf.Abs(tempLike));
            if (ai.friendTracker.giftOfferedToMe != null && ai.friendTracker.giftOfferedToMe.owner == player)
            {
                num = Custom.LerpMap(num, -0.5f, 1f, 0f, 1f, 0.8f);
            }
            return num;
        }

        public override void Update()
        {
            base.Update();
            if (_room == null || _camera.room != _room || messages.Count == 0)
            {
                DeleteDialogBox();
                return;
            }
        }

        public override void Draw(float timeStacker)
        {
            if (this._camera == null || this._camera.room != this._room)
            {
                DeleteDialogBox();
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
            slatedForDeletion = true;
            _room = null;
            _camera = null;
            messages.Clear();
            label.RemoveFromContainer();
            if(hud.dialogBox==this)
                hud.dialogBox = null;
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
            string value = behavior.value;
            if (isIns)
            {
                if (InstantPri.ContainsKey(value))
                {
                    this.isInstant = 1;
                    priority = InstantPri[value];
                }
                else
                    priority = -1;
            }
            else
            {
                if (ConstantPri.ContainsKey(value))
                {
                    this.isInstant = 2;
                    priority = ConstantPri[value];
                }
                else
                    priority = -1;
            }
            if (priority != -1)
                State = value;
        }

        public StatePriority(Lizard.Animation anim, bool isIns)
        {
            string value = anim.value;
            if (isIns)
            {
                if (InstantPri.ContainsKey(value))
                {
                    this.isInstant = 2;
                    priority = InstantPri[value];
                }
                else
                    priority = -1;
            }
            else
            {
                if (ConstantPri.ContainsKey(value))
                {
                    this.isInstant = 2;
                    priority = ConstantPri[value];
                }
                else
                    priority = -1;
                if (priority != -1)
                    State = value;
            }
        }

        public int priority { get; protected set; }
        private readonly int isInstant = 0;

        public string State { get; protected set; }


        /// STATIC PART ///

        public static void InitStatePriority(ManualLogSource log)
        {
            _log = log;
            InstantPri = new Dictionary<string, int>();
            ConstantPri = new Dictionary<string, int>();

            {
                //打开文件-即时触发行为
                var path = AssetManager.ResolveFilePath("text/wanderer/shortlist.json");
                FileStream fileStream = new FileStream(path, FileMode.Open);
                byte[] a = new byte[fileStream.Length];
                fileStream.Read(a, 0, (int)fileStream.Length);
                var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(Encoding.UTF8.GetString(a));

                foreach (var pri in all)
                {
                    InstantPri.Add(pri.Key, pri.Value);
                }
                _log.LogDebug("ShortList loaded");
            }

            {
                //打开文件-长期触发行为
                var path = AssetManager.ResolveFilePath("text/wanderer/longlist.json");
                FileStream fileStream = new FileStream(path, FileMode.Open);
                byte[] a = new byte[fileStream.Length];
                fileStream.Read(a, 0, (int)fileStream.Length);
                var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(Encoding.UTF8.GetString(a));
                foreach (var pri in all)
                {
                    ConstantPri.Add(pri.Key, pri.Value);

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
            if (a.priority != b.priority || a.State != b.State)
                return false;
            return true;
        }
        static public bool operator !=(StatePriority a, StatePriority b)
        {
            return !(a == b);
        }


        private static Dictionary<string, int> InstantPri;
        private static Dictionary<string, int> ConstantPri;
        private static ManualLogSource _log;

    }
}
