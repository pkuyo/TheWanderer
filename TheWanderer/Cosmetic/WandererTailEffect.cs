using BepInEx.Logging;
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
            sLeaser.sprites[2] = triangleMesh;
            iGraphics.AddToContainer(sLeaser, rCam, null);

        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;

            var mesh = (sLeaser.sprites[2] as TriangleMesh);
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                if (i >= 7 && i<=9)
                    mesh.verticeColors[i] = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics, rCam), Mathf.Pow(Mathf.InverseLerp(7, 9, i), 3f));
                else if (i < 7)
                    mesh.verticeColors[i] = GetBodyColor(iGraphics);
                else
                    mesh.verticeColors[i] = GetFaceColor(iGraphics, rCam);
        }
    }
}
