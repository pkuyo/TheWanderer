using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Random;

namespace MMSC.LizardMessage
{
    class RandomMessagePicker
    {


        public static void InitLizardMessage(ManualLogSource log)
        {
            _log = log;

            //消息记录
            behaviorMessage = new Dictionary<CreatureTemplate.Type, Dictionary<LizardAI.Behavior, List<string>>>();
            animMessage = new Dictionary<CreatureTemplate.Type, Dictionary<Lizard.Animation, List<string>>>();
            var path = AssetManager.ResolveFilePath("text/mmsc/lizard.dat");
            FileStream fileStream = new FileStream(path, FileMode.Open);

            //反序列化Json
            byte[] a = new byte[fileStream.Length];
            fileStream.Read(a, 0, (int)fileStream.Length);
            var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(Encoding.Unicode.GetString(a));
            foreach (var creature in all)
            {
                CreatureTemplate.Type type = (CreatureTemplate.Type)ExtEnumBase.Parse(typeof(CreatureTemplate.Type), creature.Key,false);

                behaviorMessage.Add(type, new Dictionary<LizardAI.Behavior, List<string>>());
                animMessage.Add(type, new Dictionary<Lizard.Animation, List<string>>());

                //前十个是行为，之后则为动画
                int index = 0;
                foreach (var messages in creature.Value)
                {
                    if(index<10)
                    {
                        LizardAI.Behavior behavior = (LizardAI.Behavior)ExtEnumBase.Parse(typeof(LizardAI.Behavior), messages.Key,false);
                        behaviorMessage[type].Add(behavior, messages.Value);
                    }
                    else
                    {
                        Lizard.Animation anim = (Lizard.Animation)ExtEnumBase.Parse(typeof(Lizard.Animation), messages.Key,false);
                        animMessage[type].Add(anim, messages.Value);
                    }
                    index++;
                }
            }
            _log.LogDebug("Loaded Lizard Messages");
            fileStream.Close();
        }

        //对外接口
        public static string GetRandomMessage(Lizard target,StatePriority priority)
        {
            if (priority._anim != null)
                return GetRandomMessage(target, priority._anim);
            else
                return GetRandomMessage(target, priority._behavior);
        }

        private static string GetRandomMessage(Lizard target, Lizard.Animation anim)
        {
            if (!animMessage[target.abstractCreature.creatureTemplate.type].ContainsKey(anim))
                return anim.ToString() + " " + Range(0, 4);

            var messageList = animMessage[target.abstractCreature.creatureTemplate.type][anim];

            ////////////////////DEBUG///////////////////
            if (messageList.Count == 0)
                return anim.ToString() + " " + Range(0, 4);
            ////////////////////////////////////////////

            if (messageList.Count == 0)
                return null;
            else
                return messageList[Range(0, messageList.Count - 1)];
        }
        private static string GetRandomMessage(Lizard target, LizardAI.Behavior behavior)
        {
            if (!behaviorMessage[target.abstractCreature.creatureTemplate.type].ContainsKey(behavior))
                return behavior.ToString() + " " + Range(0, 4);

            var messageList = behaviorMessage[target.abstractCreature.creatureTemplate.type][behavior];

            ////////////////////DEBUG///////////////////
            if (messageList.Count == 0)
                return behavior.ToString() + " " + Range(0, 4);
            ////////////////////////////////////////////

            if (messageList.Count == 0)
                return null;
            else
                return messageList[Range(0, messageList.Count - 1)];
        }

        static private Dictionary<CreatureTemplate.Type, Dictionary<LizardAI.Behavior, List<string>>> behaviorMessage;
        static private Dictionary<CreatureTemplate.Type, Dictionary<Lizard.Animation, List<string>>> animMessage;
        static private ManualLogSource _log;
    }
}
