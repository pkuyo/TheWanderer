using BepInEx.Logging;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
    class WandererLongHair : CosmeticBase
    {
        public WandererLongHair(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            numberOfSprites = 2;
            hairs = new TailSegment[8];
            if(graphics.RenderAsPup)
            {
                HairSpacing = 4.5f;
                MaxLength = 5f;
            }
            for (int i = 0; i < 2; i++)
            {
                var player = graphics.owner as Player;  
                hairs[i*4+0] = new TailSegment(graphics, 6f, 4f, null, 0.85f, 1f, 1f, true);
                hairs[i*4+1] = new TailSegment(graphics, 4f, 7f, hairs[i*4+0], 0.85f, 1f, 0.5f, true);
                hairs[i*4+2] = new TailSegment(graphics, 2.5f, 7f, hairs[i*4+1], 0.85f, 1f, 0.5f, true);
                hairs[i*4+3] = new TailSegment(graphics, 1f, 7f, hairs[i*4+2], 0.85f, 1f, 0.5f, true);
            }

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            
            for (int k=0;k<2;k++)
            {
                var dir = Custom.DirVec(iGraphics.owner.bodyChunks[0].pos, iGraphics.owner.bodyChunks[1].pos).normalized;
                var RootPos = iGraphics.head.pos + (k == 0 ? -1 : 1) * Custom.PerpendicularVector(dir).normalized * HairSpacing + dir * -0f;

                Vector2 vector = RootPos;
                var lastDir = Custom.DirVec(iGraphics.owner.bodyChunks[0].lastPos, iGraphics.owner.bodyChunks[1].lastPos).normalized;
                Vector2 vector2 = Vector2.Lerp(iGraphics.head.lastPos + (k == 0 ? -1 : 1) * Custom.PerpendicularVector(lastDir).normalized * HairSpacing + lastDir * 5f, RootPos, timeStacker);
                Vector2 vector4 = (vector2 * 3f + vector) / 4f;
                float HairWidth = 0.2f;
                float d2 = 6f;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector5 = Vector2.Lerp(hairs[k * 4 + i].lastPos, hairs[k * 4 + i].pos, timeStacker);
                    Vector2 normalized = (vector5 - vector4).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);
                    float d3 = Vector2.Distance(vector5, vector4) / 5f;
                    if (i == 0)
                    {
                        d3 = 0f;
                    }
                    //设置坐标
                    (sLeaser.sprites[startSprite+k] as TriangleMesh).MoveVertice(i * 4, vector4 - a * d2 * HairWidth + normalized * d3 - camPos);
                    (sLeaser.sprites[startSprite+k] as TriangleMesh).MoveVertice(i * 4 + 1, vector4 + a * d2 * HairWidth + normalized * d3 - camPos);
                    if (i < 3)
                    {
                        (sLeaser.sprites[startSprite+k] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - a * hairs[k * 4 + i].StretchedRad * HairWidth - normalized * d3 - camPos);
                        (sLeaser.sprites[startSprite+k] as TriangleMesh).MoveVertice(i * 4 + 3, vector5 + a * hairs[k * 4 + i].StretchedRad * HairWidth - normalized * d3 - camPos);
                    }
                    else
                    {
                        (sLeaser.sprites[startSprite+k] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - camPos);
                    }
                    d2 = hairs[k * 4 + i].StretchedRad;
                    vector4 = vector5;
                }
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {   

            for (int i = 0; i < 2; i++)
            {
       
                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                {
                //new TriangleMesh.Triangle(0, 1, 2),
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
                };//一个带状mesh 结尾为三角
                TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, true, false);
                sLeaser.sprites[startSprite + i] = triangleMesh;

                //TODO 切屏防止闪现
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            var faceColor = GetFaceColor(iGraphics, rCam);
            var bodyColor = GetBodyColor(iGraphics);
            var fadeLength = 2;
            for (int i = 0; i < 2; i++)
            {
                var mesh = (sLeaser.sprites[startSprite+i] as TriangleMesh);
                for (int j = 0; j < fadeLength; j++)
                    mesh.verticeColors[j] = Color.Lerp(bodyColor,faceColor,Mathf.Pow(j/(float)(fadeLength - 1),0.5f));
                for (int j = fadeLength; j < mesh.verticeColors.Length; j++)
                    mesh.verticeColors[j] = faceColor;
            }
        }

        public override void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                var dir = Custom.DirVec(iGraphics.owner.bodyChunks[0].pos, iGraphics.owner.bodyChunks[1].pos).normalized;
                var RootPos = iGraphics.head.pos + (i == 0 ? -1 : 1) * Custom.PerpendicularVector(dir).normalized * HairSpacing + dir * -0f;

                var num3 = 1f - Mathf.Clamp((Mathf.Abs(iGraphics.owner.bodyChunks[1].vel.x) - 1f) * 0.5f, 0f, 1f);
                bool flag = (Mathf.Abs(iGraphics.owner.bodyChunks[0].vel.x) > 2f && Mathf.Abs(iGraphics.owner.bodyChunks[1].vel.x) > 2f);
                Vector2 vector2 = RootPos;

                Vector2 pos = RootPos;
                float num9 = 28f;

                hairs[i*4].connectedPoint = new Vector2?(RootPos);
                for (int k = 0; k < 4; k++)
                {
                    hairs[k + i * 4].Update();
                    hairs[k + i * 4].vel *= Mathf.Lerp(0.75f, 0.95f, num3 * (1f - iGraphics.owner.bodyChunks[1].submersion));//水中减少速度
                    TailSegment tailSegment7 = hairs[k + i * 4];
                    tailSegment7.vel.y = tailSegment7.vel.y - Mathf.Lerp(0.1f, 0.5f, num3) * (1f - iGraphics.owner.bodyChunks[1].submersion) * iGraphics.owner.EffectiveRoomGravity;
                    num3 = (num3 * 10f + 1f) / 11f;
                    if (!Custom.DistLess(hairs[k + i * 4].pos, RootPos, MaxLength * (float)(k + 1)))
                    {
                        hairs[k + i * 4].pos = RootPos + Custom.DirVec(RootPos, hairs[k + i * 4].pos) * MaxLength * (float)(k + 1);
                    }
                    hairs[k + i * 4].vel += Custom.DirVec(vector2, hairs[k + i * 4].pos) * num9 / Vector2.Distance(vector2, hairs[k + i * 4].pos);
                    num9 *= 0.5f;
                    vector2 = pos;
                    pos = hairs[k + i * 4].pos;
                }
            }
        }

        TailSegment[] hairs;
        float HairSpacing = 6f;

        float MaxLength = 9f;
    }
    
}
