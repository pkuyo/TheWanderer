using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.LizardMessage
{
    static class RandomMessagePicker
    {

        public static void InitLizardMessage(ManualLogSource log, RainWorld rainWorld)
        {
            _log = log;

            //消息记录
            Message = new Dictionary<CreatureTemplate.Type, Dictionary<string, List<string>>>();

            //选择语言
            //TODO: 动态语言调整
            string path;
            if(rainWorld.inGameTranslator.currentLanguage==InGameTranslator.LanguageID.Chinese)
                path = AssetManager.ResolveFilePath("text/wanderer/lizard_cn.json");
            else
                path = AssetManager.ResolveFilePath("text/wanderer/lizard_en.json");

            FileStream fileStream = new FileStream(path, FileMode.Open);

            //反序列化Json
            byte[] a = new byte[fileStream.Length];
            fileStream.Read(a, 0, (int)fileStream.Length);
            var all = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(Encoding.UTF8.GetString(a));
            foreach (var creature in all)
            {
                CreatureTemplate.Type type = (CreatureTemplate.Type)ExtEnumBase.Parse(typeof(CreatureTemplate.Type), creature.Key,false);
                Message.Add(type, new Dictionary<string, List<string>>());

                foreach (var messages in creature.Value)
                    Message[type].Add(messages.Key, messages.Value);
                
            }
            _log.LogDebug("Loaded Lizard Messages");
            fileStream.Close();
        }

        //对外接口
        public static string GetRandomMessage(Lizard target,StatePriority priority)
        {
            if(target.lizardParams.template == CreatureTemplate.Type.YellowLizard)
            {
                string message = string.Empty;
                int lastIndex = -1;

                //构建乱码
                for(int i=0;i<Random.Range(4,7);i++)
                {
                    int index;
                    while ((index = Random.Range(0, 9)) == lastIndex) ;
                    message += YellowLizardText[index];
                    lastIndex = index;
                }
                return message;
            }
            else if (priority._anim != null)
                return GetRandomMessage(target, priority._anim.value);
            else
                return GetRandomMessage(target, priority._behavior.value);
        }


        private static string GetRandomMessage(Lizard target, string value)
        {
            //无状态
            if (!Message[target.abstractCreature.creatureTemplate.type].ContainsKey(value))
            {
                _log.LogError("Can't get message use " + value);
                return null;
            }
            //如果是朋友则尝试查找朋友对话
            if (target.AI.friendTracker.friend != null && (target.AI.friendTracker.friend is Player) && target.AI.friendTracker.followClosestFriend == true && Message[target.abstractCreature.creatureTemplate.type].ContainsKey(value + "Friend"))
                value += "Friend";

            var messageList = Message[target.abstractCreature.creatureTemplate.type][value];
            if (messageList.Count == 0)
                messageList = Message[CreatureTemplate.Type.PinkLizard][value];

            if (messageList.Count == 0)
                return null;
            else
                return messageList[Random.Range(0, messageList.Count - 1)];
        }

        static private Dictionary<CreatureTemplate.Type, Dictionary<string, List<string>>> Message;
        static private ManualLogSource _log;
        static private string YellowLizardText = "!@#$%^&*+-";
    }
}
