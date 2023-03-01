using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Cosmetic
{
    public class CosmeticBase
    {
        protected CosmeticBase(PlayerGraphics graphics, ManualLogSource log)
        {
            iGraphics = graphics;
            _log = log;
        }
        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

        }

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {

        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
 
        }

        public virtual void Update()
        {

        }


        protected Color GetBodyColor(PlayerGraphics self)
        {
            Color color = PlayerGraphics.SlugcatColor(self.CharacterForColor);
            return color;
        }

        protected Color GetFaceColor(PlayerGraphics self, RoomCamera rCam)
        {
            Color color = new Color(186 / 255.0f, 252 / 255.0f, 240 / 255.0f);
            Player player = self.owner as Player;
            if (self.useJollyColor)
            {
                color = PlayerGraphics.JollyColor(player.playerState.playerNumber, 1);
            }
            if (PlayerGraphics.CustomColorsEnabled())
            {
                color = PlayerGraphics.CustomColorSafety(1);
            }
            return color;
        }

        protected ManualLogSource _log;

        public int startSprite;
        public int numberOfSprites;
        public PlayerGraphics iGraphics;
    }
}
