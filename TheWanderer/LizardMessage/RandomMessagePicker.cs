using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
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
        public class MessageLoadException : Exception
        {
            public MessageLoadException(string lan)
            {
                language = lan;
            }
            public override string Message => language  + " file load failed";

            string language;
        }

        public static void InitLizardMessage(ManualLogSource log, RainWorld rainWorld)
        {
            _log = log;
            rw = rainWorld;

            //消息记录
            
            Messages = new ConcurrentDictionary<InGameTranslator.LanguageID, ConcurrentDictionary<CreatureTemplate.Type, ConcurrentDictionary<string, List<string>>>>();

            AddSingleLanguage(InGameTranslator.LanguageID.Chinese);
            AddSingleLanguage(InGameTranslator.LanguageID.English);

            _log.LogDebug("Loaded Lizard Messages");
            
        }

        private static void AddSingleLanguage(InGameTranslator.LanguageID language)
        {
            var message = new ConcurrentDictionary<CreatureTemplate.Type, ConcurrentDictionary<string, List<string>>>();

            //选择语言
            string path;
            if (language == InGameTranslator.LanguageID.Chinese)
                path = AssetManager.ResolveFilePath("text/wanderer/lizard_cn.json");
            else
                path = AssetManager.ResolveFilePath("text/wanderer/lizard_en.json");

            FileStream fileStream = new FileStream(path, FileMode.Open);

            //反序列化Json
            byte[] a = new byte[fileStream.Length];
            fileStream.Read(a, 0, (int)fileStream.Length);
            var all = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>>>(Encoding.UTF8.GetString(a));
            foreach (var creature in all)
            {
                CreatureTemplate.Type type = (CreatureTemplate.Type)ExtEnumBase.Parse(typeof(CreatureTemplate.Type), creature.Key, false);
                message.TryAdd(type, new ConcurrentDictionary<string, List<string>>());

                foreach (var messages in creature.Value)
                {
                    message[type].TryAdd(messages.Key, messages.Value);
                }

            }
            fileStream.Close();
            _log.LogDebug("[Message] Load " + all.Count + " lizards with " + all["PinkLizard"].Count + " states.[" + language.value+"]");

            if (all["PinkLizard"].Count == 0)
                throw new MessageLoadException(language.value);

            Messages.TryAdd(language, message);
        }

        //对外接口
        public static string GetRandomMessage(Lizard target,StatePriority priority)
        {
            if (target.lizardParams.template == CreatureTemplate.Type.YellowLizard)
            {
                string message = string.Empty;
                int lastIndex = -1;

                //构建乱码
                for (int i = 0; i < Random.Range(4, 7); i++)
                {
                    int index;
                    while ((index = Random.Range(0, 9)) == lastIndex) ;
                    message += YellowLizardText[index];
                    lastIndex = index;
                }
                return message;
            }
            else if (priority.State != null)
                return GetRandomMessage(target, priority.State);
            else
                return null;
        }


        private static string GetRandomMessage(Lizard target, string value)
        {
            //无状态
            CreatureTemplate.Type type = target.abstractCreature.creatureTemplate.type;

            var message = Messages[rw.inGameTranslator.currentLanguage];

            //其他种类蜥蜴
            if (!message.ContainsKey(type))
                type = CreatureTemplate.Type.PinkLizard;

            if (!message[type].ContainsKey(value))
            {
                _log.LogError("[Message] Can't get message use " + value);
                return "Message load failed!";
            }
            //如果是朋友则尝试查找朋友对话
            if (target.AI.friendTracker.friend != null && (target.AI.friendTracker.friend is Player) && target.AI.friendTracker.followClosestFriend == true && message[type].ContainsKey(value + "Friend"))
                value += "Friend";

            var messageList = message[type][value];
            if (messageList.Count == 0)
                messageList = message[CreatureTemplate.Type.PinkLizard][value];

            if (messageList.Count == 0)
                return null;
            else
                return messageList[Random.Range(0, messageList.Count - 1)];
        }

 
        static private ConcurrentDictionary<InGameTranslator.LanguageID, ConcurrentDictionary<CreatureTemplate.Type, ConcurrentDictionary<string, List<string>>>> Messages;
        static private RainWorld rw;
        static private ManualLogSource _log;
        static private string YellowLizardText = "!@#$%^&*+-";
    }
}
