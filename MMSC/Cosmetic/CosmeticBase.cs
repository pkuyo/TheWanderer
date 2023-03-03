using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugBase.DataTypes;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
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

            if (BodyColor.GetColor(self)!=null)
                color = (Color)BodyColor.GetColor(self);
            return color;
        }

        protected Color GetFaceColor(PlayerGraphics self, RoomCamera rCam)
        {
            Color color = new Color(186 / 255.0f, 252 / 255.0f, 240 / 255.0f);
            if (self.useJollyColor)
                color = PlayerGraphics.JollyColor((self.owner as Player).playerState.playerNumber, 1);
            if (PlayerGraphics.CustomColorsEnabled())
                color = PlayerGraphics.CustomColorSafety(1);

           
            if (EyeColor.GetColor(self) != null)
                color = (Color)EyeColor.GetColor(self);

            return color;
        }

        protected ManualLogSource _log;

        public int startSprite;
        public int numberOfSprites;
        public PlayerGraphics iGraphics;

        PlayerColor EyeColor = new PlayerColor("Eyes");
        PlayerColor BodyColor = new PlayerColor("Body");
    }
}
