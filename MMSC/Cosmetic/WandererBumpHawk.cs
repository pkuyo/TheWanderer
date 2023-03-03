using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Cosmetic
{
    class WandererBumpHawk :CosmeticBase
    {
        public WandererBumpHawk(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            if(graphics.RenderAsPup)
            {
                bumps = 3;
            }
            numberOfSprites = bumps;
           
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = base.startSprite + this.numberOfSprites - 1; i >= base.startSprite; i--)
            {
                float num = Mathf.InverseLerp((float)base.startSprite, (float)(base.startSprite + this.numberOfSprites - 1), (float)i);
                sLeaser.sprites[i].scale = Mathf.Lerp(this.sizeRangeMin, this.sizeRangeMax, Mathf.Lerp(Mathf.Sin(Mathf.Pow(num, this.sizeSkewExponent) * 3.1415927f), 1f, (num >= 0.5f) ? 0f : 0.5f));
                float spineFactor = Mathf.Lerp(0.05f, 1, num);
                PlayerGraphics.PlayerSpineData spineData = this.iGraphics.SpinePosition(spineFactor, timeStacker);
                sLeaser.sprites[i].x = spineData.outerPos.x - camPos.x;
                sLeaser.sprites[i].y = spineData.outerPos.y - camPos.y;
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = base.startSprite + this.numberOfSprites - 1; i >= base.startSprite; i--)
            {
                Mathf.InverseLerp((float)base.startSprite, (float)(base.startSprite + this.numberOfSprites - 1), (float)i);
                sLeaser.sprites[i] = new FSprite("tinyStar", true);
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
 
            for (int i = base.startSprite + this.numberOfSprites - 1; i >= base.startSprite; i--)
            {
                float t = Mathf.InverseLerp((float)base.startSprite, (float)(base.startSprite + this.numberOfSprites - 1), (float)i);
                float y = Mathf.Lerp(0.05f, this.spineLength, t);
                sLeaser.sprites[i].color = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics, rCam), t);
            }
        }

        int bumps = 6;
		float spineLength = 0.6f;
		float sizeRangeMin = 0.5f;
		float sizeRangeMax = 1f;
		float sizeSkewExponent = 0.4f;
    }
}
