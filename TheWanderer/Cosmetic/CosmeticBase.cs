using BepInEx.Logging;
using HarmonyLib;
using SlugBase.DataTypes;
using System;
using UnityEngine;

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
            if (IsDirty)
                if (UpdateDirtyShader(sLeaser, rCam))
                {
                    ApplyPalette(sLeaser, rCam, new RoomPalette());
                    IsDirty = false;
                }
        }

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            IsDirty = true;
        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }

        public virtual void Update()
        {

        }

        public virtual bool UpdateDirtyShader(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            return true;
        }
        protected Color GetBodyColor(PlayerGraphics self)
        {
            Color color = PlayerGraphics.SlugcatColor(self.CharacterForColor);

            if (BodyColor.GetColor(self) != null)
                color = (Color)BodyColor.GetColor(self);
            return color;
        }

        protected Color GetFaceColor(PlayerGraphics self)
        {
            Color color = new Color(186 / 255.0f, 252 / 255.0f, 240 / 255.0f);
            //if (self.useJollyColor)
            //    color = PlayerGraphics.JollyColor((self.owner as Player).playerState.playerNumber, 1);
            //if (PlayerGraphics.CustomColorsEnabled())
            //    color = PlayerGraphics.CustomColorSafety(1);

            if (EyeColor.GetColor(self) != null)
                color = (Color)EyeColor.GetColor(self);
            if (IsLounge)
            {
                if (LoungeColor.GetColor(self) != null)
                    color = (Color)LoungeColor.GetColor(self);
            }

            return color;
        }

        protected Color GetLoungeColor(PlayerGraphics self)
        {
            Color color = new Color(186 / 255.0f, 252 / 255.0f, 240 / 255.0f);

            if (LoungeColor.GetColor(self) != null)
                color = (Color)LoungeColor.GetColor(self);


            return color;
        }


        protected bool TryGetMaterial(FSprite sprite, out Material material)
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

        public bool IsLounge
        {
            get
            {
                return _IsLounge;
            }
            set
            {
                if (_IsLounge != value)
                    IsDirty = true;
                _IsLounge = value;
            }
        }

        virtual public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner==null)
                newContatiner = rCam.ReturnFContainer("Midground");
            for (int i = startSprite; i < startSprite + numberOfSprites; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }


        private bool _IsLounge;
        private bool IsDirty = true;

        public WeakReference<PlayerGraphics> iGraphicsRef;

        static readonly PlayerColor LoungeColor = new PlayerColor("Lounge");
        static readonly PlayerColor EyeColor = new PlayerColor("Eyes");
        static readonly PlayerColor BodyColor = new PlayerColor("Body");
    }
}
