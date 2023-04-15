using Nutils.Hook;
using RWCustom;

namespace Pkuyo.Wanderer
{

    class WandererClimbTurtorial : CustomTurtorial
    {
        public WandererClimbTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("While in the air, tap jump and pick-up together to climb background wall.", 0, 300) })
        {
        }
    }
    class WandererToolTurtorial : CustomTurtorial
    {
        public WandererToolTurtorial(Room room)
              : base(room, new Message[] {  Message.NewMessage("Tap jump and pick-up together to active tools.", 120, 160),
                                            Message.NewMessage("Activating this tool allows you to climb air!", 120, 160)})
        {
        }
    }

    class WandererScareTurtorial : CustomTurtorial
    {
        public WandererScareTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("hold grab and throw to make a noise to scare lizards.", 120, 160) ,
                                         Message.NewMessage("Avoid hurting the lizards, try to have a good relationship with them!", 120, 160)})

        {
        }
    }

    class WandererLoungeTurtorial : CustomTurtorial
    {
        public WandererLoungeTurtorial(Room room)
            : base(room, new Message[] { Message.NewMessage("Press [/"+WandererMod.WandererOptions.LoungeKeys[0].Value.ToString() +"/] to enter the sprint state, but it will reduce satiety.",0, 500)})

        {
        }
    }
}
