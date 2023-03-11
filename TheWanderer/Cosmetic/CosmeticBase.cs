using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugBase.DataTypes;
using UnityEngine;
using HarmonyLib;

namespace Pkuyo.Wanderer.Cosmetic
{
    public class CosmeticBase
    {
        protected CosmeticBase(PlayerGraphics graphics, ManualLogSource log)
        {
            iGraphicsRef = new WeakReference<PlayerGraphics>(graphics);
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

        protected Color GetFaceColor(PlayerGraphics self)
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

        
        protected bool TryGetMaterial(FSprite sprite,out Material material)
        {
            material = null;
            var layer = Traverse.Create(sprite).Field("_renderLayer").GetValue<FFacetRenderLayer>();
            if (layer == null)
                return false;

            var mat = Traverse.Create(layer).Field("_material").GetValue<Material>();
            if (mat == null)
                return false;

            material = mat;
            return true;

        }
        protected ManualLogSource _log;

        public int startSprite;
        public int numberOfSprites;

        public WeakReference<PlayerGraphics> iGraphicsRef;


        static PlayerColor EyeColor = new PlayerColor("Eyes");
        static PlayerColor BodyColor = new PlayerColor("Body");
    }
}
