using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMSC.Characher
{

    class WandererClimbTurtorial : UpdatableAndDeletable
    {
        public WandererClimbTurtorial(Room room)
        {
            this.room = room;
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                this.Destroy();
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if(room.game.session.Players[0].realizedCreature != null && room.game.cameras[0].hud != null && 
                room.game.cameras[0].hud.textPrompt != null && room.game.cameras[0].hud.textPrompt.messages.Count < 1)
            {
                if (!isUse)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("While in the air, tap jump and pick-up together to climb background wall."), 120, 500, false, ModManager.MMF);
                    isUse = true;
                }
                else
                {
                    this.Destroy();
                }
            }
        }
        bool isUse = false;
    }

    class WandererScareTurtorial : UpdatableAndDeletable
    {
        public WandererScareTurtorial(Room room)
        {
            this.room = room;
            if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
            {
                this.Destroy();
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room.game.session.Players[0].realizedCreature != null && room.game.cameras[0].hud != null &&
                room.game.cameras[0].hud.textPrompt != null && room.game.cameras[0].hud.textPrompt.messages.Count < 1)
            {
                switch(isUse)
                {
                    case 0:
                        this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("hold grab and throw to make a noise to scare lizards."), 120, 160, false, ModManager.MMF);
                        isUse++;
                        break;
                    case 1:
                        this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("Avoid hurting the lizards, try to have a good relationship with them!"), 0, 160, false, ModManager.MMF);
                        isUse++;
                        break;
                    case 2:
                        this.Destroy();
                        break;
                }
            }
        }
        int isUse = 0;
    }
}
