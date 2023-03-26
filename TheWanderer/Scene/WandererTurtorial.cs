using RWCustom;

namespace Pkuyo.Wanderer
{
    class WandererTurtorial : UpdatableAndDeletable
    {
        public WandererTurtorial(Room room, Message[] list)
        {
            this.room = room;
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
                Destroy();
            messageList = list;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room.game.session.Players[0].realizedCreature != null && room.game.cameras[0].hud != null &&
                room.game.cameras[0].hud.textPrompt != null && room.game.cameras[0].hud.textPrompt.messages.Count < 1)
            {
                if (index < messageList.Length)
                {
                    var texts = messageList[index].text.Split('/');
                    string transTest = "";
                    foreach(var text in texts)
                        transTest += Custom.rainWorld.inGameTranslator.Translate(text);
                    
                    room.game.cameras[0].hud.textPrompt.AddMessage(transTest, messageList[index].wait, messageList[index].time, false, ModManager.MMF);
                    index++;
                }
                else
                    slatedForDeletetion = true;
            }
        }
        public class Message
        {
            public string text;
            public int wait;
            public int time;
            Message(string s, int w, int t)
            {
                text = s;
                wait = w;
                time = t;
            }
            static public Message NewMessage(string s, int w, int t)
            {
                return new Message(s, w, t);
            }
        }
        int index = 0;
        readonly Message[] messageList;
    }

    class WandererClimbTurtorial : WandererTurtorial
    {
        public WandererClimbTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("While in the air, tap jump and pick-up together to climb background wall.", 0, 300) })
        {
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                Destroy();
            }
        }
    }
    class WandererToolTurtorial : WandererTurtorial
    {
        public WandererToolTurtorial(Room room)
              : base(room, new Message[] {  Message.NewMessage("Tap jump and pick-up together to active tools.", 120, 160),
                                            Message.NewMessage("Activating this tool allows you to climb air!", 120, 160)})
        {
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                Destroy();
            }
        }
    }

    class WandererScareTurtorial : WandererTurtorial
    {
        public WandererScareTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("hold grab and throw to make a noise to scare lizards.", 120, 160) ,
                                         Message.NewMessage("Avoid hurting the lizards, try to have a good relationship with them!", 120, 160)})

        {
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                Destroy();
            }
        }
    }

    class WandererLoungeTurtorial : WandererTurtorial
    {
        public WandererLoungeTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("Press [/"+WandererCharacterMod.WandererOptions.LoungeKeys[0].Value.ToString() +"/] to enter the sprint state, but it will reduce satiety.",0, 500)})

        {
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                Destroy();
            }
        }
    }
}
