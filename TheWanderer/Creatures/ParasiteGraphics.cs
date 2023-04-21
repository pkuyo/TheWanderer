using RWCustom;
using System;

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
            ///
            legLength = parasite.bodyChunkConnections[0].distance;

            if (parasite.isChild)
            {
                pincherLength = 3f;
            }
            else if(parasite.isFemale)
            {
                pincherLength = 6f;
                needleLength = 10;
            }
            else
            {
                pincherLength = 20f;
            }
            
            drawPositions = new Vector2[parasite.bodyChunks.Length, 2];
            pinchers = new GenericBodyPart[2];
            for (int i = 0; i < 2; i++)
                pinchers[i] = new GenericBodyPart(this, 1f, 0.5f, 1f, owner.bodyChunks[0]);

            if (parasite.isFemale)
            {
                needles = new GenericBodyPart[2];
                for (int i = 0; i < 2; i++)
                    needles[i] = new GenericBodyPart(this, 1f, 0.5f, 1f, owner.bodyChunks[2]);
            }
            legs = new Limb[3, 2];
            knees = new GenericBodyPart[3, 2];
            for (int i = 0; i < legs.GetLength(0); i++)
                for (int j = 0; j < legs.GetLength(1); j++)
                {
                    knees[i, j] = new GenericBodyPart(this, 1f, 0.5f, 0.99f, parasite.mainBodyChunk);
                    legs[i, j] = new Limb(this, owner.bodyChunks[0], i * 4 + j, 0.2f, 0.7f, 0.99f, 22f, 0.95f);
                }

            int index = 0;
            bodyParts = new BodyPart[parasite.isFemale ? 10 : 8];
            bodyParts[index++] = pinchers[0];
            bodyParts[index++] = pinchers[1];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                    bodyParts[index++] = legs[i, j];
            if (parasite.isFemale)
            {
                bodyParts[index++] = needles[0];
                bodyParts[index++] = needles[1];
            }
            legsTravelDirs = new Vector2[3, 2];
            ////////////////////////
            Random.state = state;
            if(debugVisual)
                debugLabel = new FLabel(Custom.GetFont(), "");
        }

   
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2+6 + (parasite.isFemale ? 2 : 0)];
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[i] = new FSprite("Futile_White");
                sLeaser.sprites[i].color = Color.Lerp(Color.white,Color.blue,0.5f);
                sLeaser.sprites[i].anchorX = 1;
                sLeaser.sprites[i].anchorY = 1;
            }
  

            for (int i = 0; i < 6; i++)
            {
                sLeaser.sprites[i + 2] = new FSprite("Futile_White");
                sLeaser.sprites[i + 2].color = Color.Lerp(Color.white, Color.green,0.5f);
                sLeaser.sprites[i + 2].anchorX = 1;
                sLeaser.sprites[i + 2].anchorY = 1;
            }

            if (parasite.isFemale)
            {
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[i + 8] = new FSprite("Futile_White");
                    sLeaser.sprites[i + 8].color = Color.Lerp(Color.white, Color.cyan, 0.5f);
                    sLeaser.sprites[i + 8].anchorX = 1;
                    sLeaser.sprites[i + 8].anchorY = 1;
                }
            }
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
                sprite.MoveToBack();
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


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[i].width = pinchers[i].rad * 3;
                sLeaser.sprites[i].x = Mathf.Lerp(pinchers[i].lastPos.x, pinchers[i].pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[i].y = Mathf.Lerp(pinchers[i].lastPos.y, pinchers[i].pos.y, timeStacker) - camPos.y;
                sLeaser.sprites[i].height = Mathf.Lerp(Custom.Dist(pinchers[i].lastPos, pinchers[i].connection.lastPos), Custom.Dist(pinchers[i].pos, pinchers[i].connection.pos), timeStacker);
                sLeaser.sprites[i].rotation = Custom.VecToDeg(Custom.DirVec(Vector2.Lerp(pinchers[i].connection.lastPos, pinchers[i].connection.pos, timeStacker), Vector2.Lerp(pinchers[i].lastPos, pinchers[i].pos, timeStacker)));
            }

            if(parasite.isFemale)
            {
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[2 + 6 + i].width = needles[i].rad * 3;
                    sLeaser.sprites[2 + 6 + i].x = Mathf.Lerp(needles[i].lastPos.x, needles[i].pos.x, timeStacker) - camPos.x;
                    sLeaser.sprites[2 + 6 + i].y = Mathf.Lerp(needles[i].lastPos.y, needles[i].pos.y, timeStacker) - camPos.y;
                    sLeaser.sprites[2 + 6 + i].height = Mathf.Lerp(Custom.Dist(needles[i].lastPos, needles[i].connection.lastPos), Custom.Dist(needles[i].pos, needles[i].connection.pos), timeStacker);
                    sLeaser.sprites[2 + 6 + i].rotation = Custom.VecToDeg(Custom.DirVec(Vector2.Lerp(needles[i].connection.lastPos, needles[i].connection.pos, timeStacker), Vector2.Lerp(needles[i].lastPos, needles[i].pos, timeStacker)));
                }

            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float t = Mathf.InverseLerp(0f, legs.GetLength(0) - 1, i);
                    Vector2 mainPos = parasite.mainBodyChunk.pos - Custom.DirVec(parasite.bodyChunks[1].pos, parasite.bodyChunks[0].pos) * parasite.bodyChunkConnections[0].distance * t;
                    Vector2 mainLastPos = parasite.mainBodyChunk.lastPos - Custom.DirVec(parasite.bodyChunks[1].lastPos, parasite.bodyChunks[0].lastPos) * parasite.bodyChunkConnections[0].distance * t;
                    sLeaser.sprites[i + j * 3 + 2].width = legs[i,j].rad *3;
                    sLeaser.sprites[i + j * 3 + 2].x = Mathf.Lerp(legs[i, j].lastPos.x, legs[i, j].pos.x, timeStacker) - camPos.x;
                    sLeaser.sprites[i + j * 3 + 2].y = Mathf.Lerp(legs[i, j].lastPos.y, legs[i, j].pos.y, timeStacker) - camPos.y;
                    sLeaser.sprites[i + j * 3 + 2].height = Mathf.Lerp(Custom.Dist(legs[i, j].lastPos, mainLastPos), Custom.Dist(legs[i, j].pos, mainPos),timeStacker);
                    sLeaser.sprites[i + j * 3 + 2].rotation = Custom.VecToDeg(Custom.DirVec(Vector2.Lerp(mainLastPos, mainPos, timeStacker), Vector2.Lerp(legs[i, j].lastPos, legs[i, j].pos, timeStacker)));
                }
            }
            //DEBUG 
            if (debugVisual)
            {
                var tris = sLeaser.sprites[sLeaser.sprites.Length-1] as TriangleMesh;
                for (int i = 0; i < owner.bodyChunks.Length; i++)
                {
                    tris.MoveVertice(0 + i * 2, Vector2.Lerp(owner.bodyChunks[i].lastPos, owner.bodyChunks[i].pos, timeStacker) + Vector2.Perpendicular(owner.bodyChunks[i].Rotation) * owner.bodyChunks[i].rad *((i==2) ? -1 : 1)* (1/2f) - camPos);
                    tris.MoveVertice(1 + i * 2, Vector2.Lerp(owner.bodyChunks[i].lastPos, owner.bodyChunks[i].pos, timeStacker) + Vector2.Perpendicular(owner.bodyChunks[i].Rotation) * owner.bodyChunks[i].rad * ((i == 2) ? -1 : 1)*(-1/2f) - camPos);
                }
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
                foreach(var s in sLeaser.sprites)
                {
                    s.RemoveFromContainer();
                    rCam.ReturnFContainer("HUD").AddChild(s);
                }
                sLeaser.sprites[sLeaser.sprites.Length-1].MoveToBack();
                rCam.ReturnFContainer("HUD").RemoveChild(debugLabel);
                rCam.ReturnFContainer("HUD").AddChild(debugLabel);
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < drawPositions.GetLength(0); i++)
            {
                drawPositions[i, 1] = drawPositions[i, 0];
                drawPositions[i, 0] = parasite.bodyChunks[i].pos;
            }
            //TODO : 危机情况
            lastFlip = flip;

            lastVibrate = vibrate;
            vibrate = Custom.LerpAndTick(vibrate, (parasite.charging > 0f || parasite.attemptBite > 0f) ? 1f : 0f, 0.2f, 0.05f);



            Vector2 bodyDir = Custom.DirVec(parasite.bodyChunks[1].pos, parasite.bodyChunks[0].pos);
            BodyChunk bodyChunk = (parasite.grasps[0] != null) ? parasite.grasps[0].grabbedChunk : null;
            float bodyDeg = Custom.AimFromOneVectorToAnother(parasite.bodyChunks[1].pos, parasite.bodyChunks[0].pos);

            for (int m = 0; m < pinchers.Length; m++)
            {
                pinchers[m].Update();
                Vector2 PincherDir = (bodyDir + Custom.PerpendicularVector(parasite.bodyChunks[0].pos, parasite.bodyChunks[1].pos) * ((m == 0) ? -1f : 1f) * (1f - Mathf.Abs(flip)) * Mathf.Lerp(0.2f, 3f, vibrate)).normalized;
                pinchers[m].ConnectToPoint(parasite.mainBodyChunk.pos + PincherDir * Mathf.Lerp(pincherLength, pincherLength*1.25f, parasite.attemptBite), Mathf.Lerp(pincherLength, pincherLength * 1.25f, parasite.attemptBite), false, 0, parasite.mainBodyChunk.vel, 0.1f, 0);
                pinchers[m].vel += PincherDir * (1f + 19f * parasite.attemptBite);

                if (parasite.isFemale)
                {
                    needles[m].Update();
                    var infectingTime = Mathf.Min(Mathf.Pow(parasite.infectingCounter / 20f, 2),1.0f);
                    var curLength = Mathf.Lerp(needleLength * 0.5f, needleLength * 2f, infectingTime);
                    Vector2 needlesDir = (Custom.DirVec(parasite.bodyChunks[1].pos, parasite.bodyChunks[2].pos) + Custom.PerpendicularVector(parasite.bodyChunks[2].pos, parasite.bodyChunks[1].pos) * ((m == 0) ? -1f : 1f) * (1f - Mathf.Abs(flip)) * Mathf.Lerp(0.2f, 3f, vibrate)).normalized;

                    if (parasite.grasps[0] != null && parasite.injectedChunk != null && parasite.grasps[0].grabbedChunk != null)
                    {
                        //获取最终点 但是长度限制为30
                        var toInfectedPos = (parasite.injectedChunk.pos + parasite.grasps[0].grabbedChunk.pos) / 2;
                        toInfectedPos = parasite.bodyChunks[2].pos + Custom.DirVec(parasite.bodyChunks[2].pos,toInfectedPos) * Mathf.Min(Custom.Dist(toInfectedPos, parasite.bodyChunks[2].pos), 30);
                        needles[m].ConnectToPoint(Vector2.Lerp(parasite.bodyChunks[2].pos + needlesDir * curLength,toInfectedPos, infectingTime),curLength, false, 0.03f, parasite.bodyChunks[2].vel, 0.2f, 0);

                        //位置未到 战斗仍将继续
                        if (!Custom.DistLess(needles[m].pos, (parasite.injectedChunk.pos + parasite.grasps[0].grabbedChunk.pos) / 2, 15))
                            parasite.infectingCounter = Mathf.Min(parasite.infectingCounter, 38);
                    }
                    else
                        needles[m].ConnectToPoint(parasite.bodyChunks[2].pos + needlesDir * curLength, curLength, false, 0, parasite.bodyChunks[2].vel, 0.1f, 0);
                }

                if (bodyChunk != null)
                {
                    pinchers[m].pos = Vector2.Lerp(pinchers[m].pos, bodyChunk.pos, 0.5f);
                    pinchers[m].vel *= 0.7f;
                }
                //蓄力
                if (vibrate > 0f)
                {
                    pinchers[m].vel += Custom.RNV() * vibrate * Random.value * 2f;
                    pinchers[m].pos += Custom.RNV() * vibrate * Random.value * 2f;
                }
            }

            //根据腿判断方向？
            float num3 = 0f;
            for (int i = 0; i < legs.GetLength(0); i++)
                for (int j = 0; j < legs.GetLength(1); j++)
                    num3 += Custom.DistanceToLine(legs[i, j].pos, parasite.bodyChunks[1].pos, parasite.bodyChunks[0].pos);
            flip = Custom.LerpAndTick(flip, Mathf.Clamp(num3 / 40f, -1f, 1f), 0.07f, 0.1f);

            float legFloorAjust = 0;
            if (parasite.Consious)
            {
                //头顶有东西
                if (parasite.room.GetTile(parasite.mainBodyChunk.pos + Custom.PerpendicularVector(parasite.mainBodyChunk.pos, parasite.bodyChunks[1].pos) * 20f).Solid)
                    legFloorAjust += 1f;

                //下面有东西
                if (parasite.room.GetTile(parasite.mainBodyChunk.pos - Custom.PerpendicularVector(parasite.mainBodyChunk.pos, parasite.bodyChunks[1].pos) * 20f).Solid)
                    legFloorAjust -= 1f;
            }
            if (legFloorAjust != 0f)
                flip = Custom.LerpAndTick(flip, legFloorAjust, 0.07f, 0.05f);

            int totIndex = 0;//?
            for (int j = 0; j < legs.GetLength(1); j++)
            {
                for (int i = 0; i < legs.GetLength(0); i++)
                {
                    //第几条腿
                    float t = Mathf.InverseLerp(0f, legs.GetLength(0) - 1, i);
                    Vector2 mainPos = parasite.mainBodyChunk.pos - bodyDir * parasite.bodyChunkConnections[0].distance * t;
                    //腿的步进值
                    float indexStep = 0.5f + 0.5f * Mathf.Sin((parasite.runCycle + totIndex * 0.25f) * 3.1415927f);//?
                    legsTravelDirs[i, j] = Vector2.Lerp(legsTravelDirs[i, j], parasite.travelDir, Mathf.Pow(Random.value, 1f - 0.9f * indexStep));

                    if (parasite.charging > 0f)
                        legsTravelDirs[i, j] *= 0f;

                    legs[i, j].Update();
                    //if (this.legs[num9, num8].mode == Limb.Mode.HuntRelativePosition || this.legsDangleCounter > 0)
                    //{
                    //    this.legs[num9, num8].mode = Limb.Mode.Dangle;
                    //}

                    //slerp是a -> b的旋转插值
                    Vector2 toLegDir = Custom.DegToVec(bodyDeg + Mathf.Lerp(40f, 160f, t) * ((legFloorAjust != 0f) ? (-legFloorAjust) : ((i == 0) ? 1f : -1f)));
                    var slerp = Vector3.Slerp(legsTravelDirs[i, j], toLegDir, 0.1f) * legLength * 0.85f * Mathf.Pow(indexStep, 0.5f);

                    Vector2 connectPoint = mainPos + new Vector2(slerp.x, slerp.y);
                    legs[i, j].ConnectToPoint(connectPoint, legLength, false, 0f, parasite.mainBodyChunk.vel, 0.1f, 0f);
                    legs[i, j].ConnectToPoint(mainPos, legLength, false, 0f, parasite.mainBodyChunk.vel, 0.1f, 0f);
                    knees[i, j].Update();

                    //向连接点移动
                    knees[i, j].vel += Custom.DirVec(connectPoint, knees[i, j].pos) * (legLength * 0.55f - Vector2.Distance(knees[i, j].pos, connectPoint)) * 0.6f;
                    knees[i, j].pos += Custom.DirVec(connectPoint, knees[i, j].pos) * (legLength * 0.55f - Vector2.Distance(knees[i, j].pos, connectPoint)) * 0.6f;

                    //向腿移动
                    knees[i, j].vel += Custom.DirVec(legs[i, j].pos, knees[i, j].pos) * (legLength * 0.55f - Vector2.Distance(knees[i, j].pos, legs[i, j].pos)) * 0.6f;
                    knees[i, j].pos += Custom.DirVec(legs[i, j].pos, knees[i, j].pos) * (legLength * 0.55f - Vector2.Distance(knees[i, j].pos, legs[i, j].pos)) * 0.6f;

                    //push out
                    if (Custom.DistLess(knees[i, j].pos, mainPos, 15f))
                    {
                        knees[i, j].vel += Custom.DirVec(mainPos, knees[i, j].pos) * (15f - Vector2.Distance(knees[i, j].pos, mainPos));
                        knees[i, j].pos += Custom.DirVec(mainPos, knees[i, j].pos) * (15f - Vector2.Distance(knees[i, j].pos, mainPos));
                    }

                    //速度回调，纵向下调，增加蓄力颤抖
                    knees[i, j].vel = Vector2.Lerp(knees[i, j].vel, parasite.mainBodyChunk.vel, 0.8f);
                    knees[i, j].vel += Custom.PerpendicularVector(parasite.bodyChunks[1].pos, mainPos) * Mathf.Lerp((i == 0) ? -1f : 1f, Mathf.Sign(flip), Mathf.Abs(flip)) * 9f;
                    knees[i, j].vel += Custom.RNV() * 4f * vibrate * Random.value;

                    if (parasite.Consious)
                    {
                        drawPositions[0, 0] += Custom.DirVec(legs[i, j].pos, drawPositions[0, 0]) * 4f * indexStep;
                        drawPositions[1, 0] += Custom.DirVec(legs[i, j].pos, drawPositions[1, 0]) * 3f * indexStep;
                        drawPositions[2, 0] += Custom.DirVec(knees[i, j].pos, drawPositions[2, 0]) * 1f * (1f - indexStep);
                    }
                    if (!Custom.DistLess(knees[i, j].pos, connectPoint, 200f))
                    {
                        knees[i, j].pos = connectPoint + Custom.RNV() * Random.value;
                    }
                    //if (legsDangleCounter > 0 || num10 < 0.1f)
                    //{
                    //    Vector2 a2 = connectPoint + vector4 * legLength * 0.5f;
                    //    if (!parasite.Consious)
                    //    {
                    //        a2 = vector5 + legsTravelDirs[i, j] * legLength * 0.5f;
                    //    }
                    //    legs[i, j].vel = Vector2.Lerp(legs[i, j].vel, a2 - legs[i, j].pos, parasite.swimming ? 0.5f : 0.05f);
                    //    Limb limb = legs[i, j];
                    //    limb.vel.y = limb.vel.y - 0.4f;
                    //}
                    //else
                    {
                        Vector2 gripPoint = connectPoint + toLegDir * legLength;
                        for (int k = 0; k < legs.GetLength(0); k++)
                        {
                            for (int l = 0; l < legs.GetLength(1); l++)
                            {
                                if (k != i && l != j && Custom.DistLess(gripPoint, legs[k, l].absoluteHuntPos, legLength * 0.1f))
                                {
                                    gripPoint = legs[k, l].absoluteHuntPos + Custom.DirVec(legs[k, l].absoluteHuntPos, gripPoint) * legLength * 0.1f;
                                }
                            }
                        }
                        float num13 = 1.2f;

                        //如果没找到落点则获取落点
                        if (!legs[i, j].reachedSnapPosition)
                        {
                            legs[i, j].FindGrip(parasite.room, connectPoint, connectPoint, legLength * num13, gripPoint, -2, -2, true);
                        }
                        else if (!Custom.DistLess(connectPoint, legs[i, j].absoluteHuntPos, legLength * num13 * Mathf.Pow(1f - indexStep, 0.2f)))
                        {
                            legs[i, j].mode = Limb.Mode.Dangle;
                        }

                    }
                    totIndex++;
                }
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
        
        //影响钳子高度
        float flip = 1;
        float lastFlip;
        float legLength = 15f;

        float vibrate;
        float lastVibrate;


        float pincherLength = 0;
        float needleLength = 0;

        GenericBodyPart[] pinchers;

        GenericBodyPart[] needles;

        Limb[,] legs;
        GenericBodyPart[,] knees;

        Vector2[,] legsTravelDirs;

        Vector2[,] drawPositions;
    }
}
