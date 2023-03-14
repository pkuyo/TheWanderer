using BepInEx.Logging;
using Pkuyo.Wanderer.Characher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
    class WandererHead : CosmeticBase
    {
        public WandererHead(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            numberOfSprites = 0;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[3].shader = rCam.game.rainWorld.Shaders["TwoColorShader"];
            base.InitiateSprites(sLeaser, rCam);
        }

   

        public override bool UpdateDirtyShader(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics owner;
            if (!iGraphicsRef.TryGetTarget(out owner))
                return false;
            Material mat;
            if (TryGetMaterial(sLeaser.sprites[3], out mat))
            {
                mat.SetColor("_EffectColor", GetFaceColor(owner));
                return true;
            }
            return false;
        }
    }

}
