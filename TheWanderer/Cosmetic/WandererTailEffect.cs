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
    class WandererTailEffect : CosmeticBase
    {


        public WandererTailEffect(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            this.numberOfSprites = 0;
           
        }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;

            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
{
            new TriangleMesh.Triangle(0, 1, 2),
            new TriangleMesh.Triangle(1, 2, 3),
            new TriangleMesh.Triangle(4, 5, 6),
            new TriangleMesh.Triangle(5, 6, 7),
            new TriangleMesh.Triangle(8, 9, 10),
            new TriangleMesh.Triangle(9, 10, 11),
            new TriangleMesh.Triangle(12, 13, 14),
            new TriangleMesh.Triangle(2, 3, 4),
            new TriangleMesh.Triangle(3, 4, 5),
            new TriangleMesh.Triangle(6, 7, 8),
            new TriangleMesh.Triangle(7, 8, 9),
            new TriangleMesh.Triangle(10, 11, 12),
            new TriangleMesh.Triangle(11, 12, 13)
};
            TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, true, false);
            //triangleMesh.shader = rCam.game.rainWorld.Shaders["TwoColorShader"];
            sLeaser.sprites[2] = triangleMesh;
            iGraphics.AddToContainer(sLeaser, rCam, null);

            isDirty = true;

        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;

            var triangleMesh = (sLeaser.sprites[2] as TriangleMesh);
            for (int j = triangleMesh.verticeColors.Length - 1; j >= 0; j--)
            {

                float num = (float)(j / 2) / (float)(triangleMesh.verticeColors.Length / 2);
                Vector2 vector;
                if (j % 2 == 0)
                {
                    vector = new Vector2(num, 0f);
                }
                else if (j < triangleMesh.verticeColors.Length - 1)
                {
                    vector = new Vector2(num, 1f);
                }
                else
                {
                    vector = new Vector2(1f, 0f);
                }
                vector.x = Mathf.Lerp(triangleMesh.element.uvBottomLeft.x, triangleMesh.element.uvTopRight.x, vector.x);
                vector.y = Mathf.Lerp(triangleMesh.element.uvBottomLeft.y, triangleMesh.element.uvTopRight.y, vector.y);
                triangleMesh.UVvertices[j] = vector;
            }
            triangleMesh.Refresh();

            //if(isDirty)
            //{
            //    Material mat = null;
            //    if(TryGetMaterial(sLeaser.sprites[2],out mat))
            //    {
            //        mat.SetColor("_EffectColor", GetFaceColor(iGraphics));
            //        isDirty = false;
            //    }
            //}

            for (int i = 0; i < triangleMesh.verticeColors.Length; i++)
                triangleMesh.verticeColors[i] =  Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics),Mathf.InverseLerp(4,triangleMesh.verticeColors.Length-1,i));
        }

        bool isDirty = true;
    }
}
