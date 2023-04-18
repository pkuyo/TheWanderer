using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer.Creatures
{
    class ParasiteGraphics : GraphicsModule
    {
        bool debugVisual = true;
        public ParasiteGraphics(PhysicalObject ow) : base(ow, false)
        {
            parasite = ow as Parasite;
            Random.State state = Random.state;
            Random.InitState(parasite.abstractCreature.ID.RandomSeed);
            ////////////////////////
            
            drawPositions = new Vector2[parasite.bodyChunks.Length, 2];
            //pinchers = new GenericBodyPart[2];
            //for(int  i = 0; i<2;i++)
            //    pinchers[i] = new GenericBodyPart(this, 1f, 0.5f, 1f, owner.bodyChunks[2]);

            //limbs = new Limb[3, 2];
            //for(int i =0;i<3;i++)
            //    for(int j =0;j<2;i++)
            //        limbs[i,j] = new Limb(this, owner.bodyChunks[0], i * 4 + j, 0.1f, 0.7f, 0.99f, 22f, 0.95f);

            int index = 0;
            //bodyParts = new BodyPart[15];
            //bodyParts[index++] = pinchers[0];
            //bodyParts[index++] = pinchers[1];
            //for (int i = 0; i < 3; i++)
            //    for (int j = 0; j < 2; i++)
            //        bodyParts[index++] = limbs[i, j];

            limbsTravelDirs = new Vector2[3, 2];
            ////////////////////////
            Random.state = state;
            if(debugVisual)
                debugLabel = new FLabel(Custom.GetFont(), "");
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            //sLeaser.sprites = new FSprite[TotalSprites];
            //sLeaser.sprites[HeadSprite] = new FSprite("Circle20", true);
            //sLeaser.sprites[HeadSprite].scaleX = 0.7f;
            //sLeaser.sprites[HeadSprite].scaleY = 0.8f;
            ////TODO :花纹装饰

            //for(int i=0;i<pinchers.Length;i++)
            //    sLeaser.sprites[PincherSprite(i)] = TriangleMesh.MakeLongMesh(8, false, false);
            //for (int j = 0; j < limbs.GetLength(0); j++)
            //    for (int k = 0; k < limbs.GetLength(1); k++)
            //        sLeaser.sprites[LegSprite(j, k)] = TriangleMesh.MakeLongMesh(12, false, false);
                
            //DEBUG 
            if (debugVisual)
            {
                if (sLeaser.sprites == null)
                    sLeaser.sprites = new FSprite[1];
                else
                    Array.Resize(ref sLeaser.sprites,sLeaser.sprites.Length+1);
                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                {
                    new TriangleMesh.Triangle(0, 1, 2),
                    new TriangleMesh.Triangle(1, 2, 3),
                    new TriangleMesh.Triangle(2, 3, 4),
                    new TriangleMesh.Triangle(3, 4, 5)
                };
                var sprite = new TriangleMesh("Futile_White", tris, true, false);
                sLeaser.sprites[sLeaser.sprites.Length - 1] = sprite;
                if (parasite.isFemale)
                    debugColor = Color.yellow;
                else if (parasite.isMale)
                    debugColor = Color.red;
                else
                    debugColor = Color.green;
                sprite.verticeColors[0] = sprite.verticeColors[1] = debugColor;
                for (int i = 1; i < owner.bodyChunks.Length; i++)
                {
                    sprite.verticeColors[0 + i * 2] = sprite.verticeColors[1 + i * 2] = Color.white;
                }
                AddToContainer(sLeaser, rCam, null);
            }
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < drawPositions.GetLength(0); i++)
            {
                drawPositions[i, 1] = drawPositions[i, 0];
                drawPositions[i, 0] = parasite.bodyChunks[i].pos;
            }
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            //Vector2 vector = Vector2.Lerp(drawPositions[0, 1], drawPositions[0, 0], timeStacker);
            //Vector2 vector2 = Vector2.Lerp(drawPositions[1, 1], drawPositions[1, 0], timeStacker) + Custom.RNV() * Random.value * 10f * d;
            //Vector2 vector3 = Vector2.Lerp(drawPositions[2, 1], drawPositions[2, 0], timeStacker);
            //Vector2 vector4 = Custom.DirVec(vector2, vector);
            //Vector2 a = Custom.PerpendicularVector(vector4);
            //Vector2 normalized = rCam.room.lightAngle.normalized;
            //normalized.y *= -1f;
            //sLeaser.sprites[HeadSprite].x = vector.x - camPos.x;
            //sLeaser.sprites[HeadSprite].y = vector.y - camPos.y;
            //sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(vector4);
            //Vector2 vector6 = vector + vector4;
            //float num = Mathf.Lerp(this.lastFlip, this.flip, timeStacker);
            //float num2 = 0f;
            //float num3 = 0f;
            //Vector2 a2 = vector;
            //for (int j = 0; j < limbs.GetLength(0); j++)
            //{
            //    for (int k = 0; k < limbs.GetLength(1); k++)
            //    {
            //        float t2 = Mathf.InverseLerp(0f, (limbs.GetLength(1) - 1), k);
            //        Vector2 vector11 = Vector2.Lerp(vector, vector2, 0.3f);
            //        vector11 += a * ((j == 0) ? 1f : -1f) * 3f * (1f - Mathf.Abs(num));
            //        vector11 += vector4 * Mathf.Lerp(5f, -11f, t2);
            //        Vector2 vector12 = Vector2.Lerp(limbs[j, k].lastPos, limbs[j, k].pos, timeStacker);
            //        Vector2 vector13 = Vector2.Lerp(knees[j, k].lastPos, knees[j, k].pos, timeStacker);
            //        Vector2 vector14 = Vector2.Lerp(vector11, vector13, 0.5f);
            //        Vector2 vector15 = Vector2.Lerp(vector13, vector12, 0.5f);
            //        float d2 = 5f;
            //        Vector2 vector16 = Vector2.Lerp(vector14, vector15, 0.5f);
            //        vector14 = vector16 + Custom.DirVec(vector16, vector14) * d2 / 2f;
            //        vector15 = vector16 + Custom.DirVec(vector16, vector15) * d2 / 2f;
            //        vector6 = vector11;
            //        num2 = 2f;
            //        for (int l = 0; l < 12; l++)
            //        {
            //            float num7 = Mathf.InverseLerp(0f, 11f, (float)l);
            //            Vector2 vector17;
            //            if (num7 < 0.5f)
            //            {
            //                vector17 = Custom.Bezier(vector11, vector11 + Custom.DirVec(vector11, vector12) * 10f, (vector15 + vector14) / 2f, vector14 + Custom.DirVec(vector15, vector14) * 7f, Mathf.InverseLerp(0f, 0.5f, num7));
            //            }
            //            else
            //            {
            //                vector17 = Custom.Bezier((vector15 + vector14) / 2f, vector15 + Custom.DirVec(vector14, vector15) * 7f, vector12, vector12 + Custom.DirVec(vector12, vector11) * 14f, Mathf.InverseLerp(0.5f, 1f, num7));
            //            }
            //            float num8 = (Mathf.Lerp(4f, 0.5f, Mathf.Pow(num7, 0.25f)) + Mathf.Sin(Mathf.Pow(num7, 2.5f) * 3.1415927f) * 1.5f) * limbsThickness;
            //            Vector2 a4 = Custom.PerpendicularVector(vector17, vector6);
            //            (sLeaser.sprites[LegSprite(j, k)] as TriangleMesh).MoveVertice(l * 4, (vector6 + vector17) / 2f - a4 * (num8 + num2) * 0.5f - camPos);
            //            (sLeaser.sprites[LegSprite(j, k)] as TriangleMesh).MoveVertice(l * 4 + 1, (vector6 + vector17) / 2f + a4 * (num8 + num2) * 0.5f - camPos);
            //            (sLeaser.sprites[LegSprite(j, k)] as TriangleMesh).MoveVertice(l * 4 + 2, vector17 - a4 * num8 - camPos);
            //            (sLeaser.sprites[LegSprite(j, k)] as TriangleMesh).MoveVertice(l * 4 + 3, vector17 + a4 * num8 - camPos);
            //            vector6 = vector17;
            //            num2 = num8;
            //        }
            //    }
            //}


            //DEBUG 
            if (debugVisual)
            {
                var tris = sLeaser.sprites[sLeaser.sprites.Length-1] as TriangleMesh;
                for (int i = 0; i < owner.bodyChunks.Length; i++)
                {
                    tris.MoveVertice(0 + i * 2, Vector2.Lerp(owner.bodyChunks[i].lastPos, owner.bodyChunks[i].pos, timeStacker) + Vector2.Perpendicular(owner.bodyChunks[i].Rotation) * owner.bodyChunks[i].rad *((i==2) ? -1 : 1)* (1) - camPos);
                    tris.MoveVertice(1 + i * 2, Vector2.Lerp(owner.bodyChunks[i].lastPos, owner.bodyChunks[i].pos, timeStacker) + Vector2.Perpendicular(owner.bodyChunks[i].Rotation) * owner.bodyChunks[i].rad * ((i == 2) ? -1 : 1)*(-1) - camPos);
                }
               // tris.MoveVertice(6, Vector2.Lerp(owner.bodyChunks[2].lastPos, owner.bodyChunks[2].pos, timeStacker) + Vector2.Perpendicular(owner.bodyChunks[2].Rotation) * owner.bodyChunks[2].rad * (-1) - camPos);
                debugLabel.text = parasite.AI.behavior.ToString() + " " + parasite.travelDir;
                debugLabel.x = tris.vertices[0].x;
                debugLabel.y = tris.vertices[0].y + 20;
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            if (debugVisual)
            {
                rCam.ReturnFContainer("HUD").RemoveChild(debugLabel);
                rCam.ReturnFContainer("HUD").AddChild(debugLabel);
            }
        }

        int TotalSprites
        {
            get => 1 + 3 * 2 + 1 * 2;
        }


        int HeadSprite { get => 0; }

        int LegSprite(int a,int b)
        {
            return 1 + 2 + a + b * 3;
        }

        int PincherSprite(int a)
        {
            return a + 1;
        }
        FLabel debugLabel;
        Color debugColor;
        Parasite parasite;

        float flip;

        GenericBodyPart[] pinchers;
        Limb[,] limbs;
        Vector2[,] limbsTravelDirs;

        Vector2[,] drawPositions;
    }
}
