using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
    class WandererShaderTest : CosmeticBase
    {
        public WandererShaderTest(PlayerGraphics graphics, ManualLogSource log) : base(graphics,log)
        {
            numberOfSprites = 1;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[startSprite] = new FSprite("Wanderer_Tail");
            sLeaser.sprites[startSprite].shader = rCam.game.rainWorld.Shaders["TwoColorShader"];
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            PlayerGraphics a=null;
            if (!iGraphicsRef.TryGetTarget(out a))
                return;
            sLeaser.sprites[startSprite].SetPosition(a.head.pos+new Vector2(30,0));

            var layer = Traverse.Create(sLeaser.sprites[startSprite]).Field("_renderLayer").GetValue<FFacetRenderLayer>();
            if(layer!=null)
            {
                var mat = Traverse.Create(layer).Field("_material").GetValue<Material>();
                if(mat !=null)
                {
                    mat.SetColor("_EffectColor",GetFaceColor(a));
                }
            }
        }

    }
}
