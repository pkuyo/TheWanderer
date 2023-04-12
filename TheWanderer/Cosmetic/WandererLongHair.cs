using BepInEx.Logging;
using RWCustom;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
    class WandererLongHair : CosmeticBase
    {
        public WandererLongHair(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            numberOfSprites = 2;
            Hairs = new TailSegment[8];
            if (graphics.RenderAsPup)
            {
                HairSpacing = 4.5f;
                //MaxLength = 5f;
            }
            for (int i = 0; i < 2; i++)
            {
                var player = graphics.owner as Player;
                Hairs[i * 4 + 0] = new TailSegment(graphics, 6f, 4f, null, 0.85f, 1f, 3f, true);
                Hairs[i * 4 + 1] = new TailSegment(graphics, 4f, 7f, Hairs[i * 4 + 0], 0.85f, 1f, 0.5f, true);
                Hairs[i * 4 + 2] = new TailSegment(graphics, 2.5f, 7f, Hairs[i * 4 + 1], 0.85f, 1f, 0.5f, true);
                Hairs[i * 4 + 3] = new TailSegment(graphics, 1f, 7f, Hairs[i * 4 + 2], 0.85f, 1f, 0.5f, true);
            }

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;

            Vector2 drawPos1 = Vector2.Lerp(iGraphics.drawPositions[1, 1], iGraphics.drawPositions[1, 0], timeStacker);
            Vector2 headPos = Vector2.Lerp(iGraphics.head.lastPos, iGraphics.head.pos, timeStacker);

            //通过身体角度判断移动
            var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, (headPos - drawPos1).normalized), -22.5f, 22.5f);

            //实际头发偏移
            var nowHairSpacing = HairSpacing * ((Mathf.Abs(moveDeg) > 10) ? 0.3f : 1f);

            //还原图层
            for (int i = 0; i < 2; i++)
                sLeaser.sprites[startSprite + i].MoveInFrontOfOtherNode(sLeaser.sprites[startSprite - 1 + i]);

            //设置图层
            if (moveDeg > 10f)
                sLeaser.sprites[startSprite].MoveBehindOtherNode(sLeaser.sprites[0]);
            else if (moveDeg < -10f)
                sLeaser.sprites[startSprite + 1].MoveBehindOtherNode(sLeaser.sprites[0]);

            for (int k = 0; k < 2; k++)
            {
                var dir = Custom.DirVec(iGraphics.drawPositions[0, 0], iGraphics.drawPositions[1, 0]).normalized;
                var rootPos = iGraphics.head.pos + (k == 0 ? -1 : 1) * Custom.PerpendicularVector(dir).normalized * nowHairSpacing + dir * -0.2f;


                var lastDir = Custom.DirVec(iGraphics.drawPositions[0, 1], iGraphics.drawPositions[1, 1]).normalized;
                Vector2 vector2 = Vector2.Lerp(iGraphics.head.lastPos + (k == 0 ? -1 : 1) * Custom.PerpendicularVector(lastDir).normalized * nowHairSpacing + lastDir * 5f, rootPos, timeStacker);
                Vector2 vector4 = (vector2 * 3f + rootPos) / 4f;
                float HairWidth = 0.2f;
                float d2 = 6f;

                bool OutLength = false;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector5 = Vector2.Lerp(Hairs[k * 4 + i].lastPos, Hairs[k * 4 + i].pos, timeStacker);
                    Vector2 normalized = (vector5 - vector4).normalized;
                    Vector2 widthDir = Custom.PerpendicularVector(normalized);
                    float d3 = Vector2.Distance(vector5, vector4) / 5f;
                    if (i == 0)
                    {
                        d3 = 0f;
                    }
                    
                    if (i!=0 && !Custom.DistLess((sLeaser.sprites[startSprite + k] as TriangleMesh).vertices[i*4], (sLeaser.sprites[startSprite + k] as TriangleMesh).vertices[i*4-4],35))
                        OutLength = true;
                    //设置坐标
                    (sLeaser.sprites[startSprite + k] as TriangleMesh).MoveVertice(i * 4, vector4 - widthDir * d2 * HairWidth + normalized * d3 - camPos);
                    (sLeaser.sprites[startSprite + k] as TriangleMesh).MoveVertice(i * 4 + 1, vector4 + widthDir * d2 * HairWidth + normalized * d3 - camPos);
                    if (i < 3)
                    {
                        (sLeaser.sprites[startSprite + k] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - widthDir * Hairs[k * 4 + i].StretchedRad * HairWidth - normalized * d3 - camPos);
                        (sLeaser.sprites[startSprite + k] as TriangleMesh).MoveVertice(i * 4 + 3, vector5 + widthDir * Hairs[k * 4 + i].StretchedRad * HairWidth - normalized * d3 - camPos);
                    }
                    else
                    {
                        (sLeaser.sprites[startSprite + k] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - camPos);
                    }
                    d2 = Hairs[k * 4 + i].StretchedRad;
                    vector4 = vector5;
                }

                if (OutLength && sLeaser.sprites[startSprite + k].isVisible)
                    sLeaser.sprites[startSprite + k].isVisible = false;
                else if (!OutLength && !sLeaser.sprites[startSprite + k].isVisible)
                    sLeaser.sprites[startSprite + k].isVisible = true;
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

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
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;

            var faceColor = GetFaceColor(iGraphics);
            var bodyColor = GetBodyColor(iGraphics);
            var fadeLength = 2;
            for (int i = 0; i < 2; i++)
            {
                var mesh = (sLeaser.sprites[startSprite + i] as TriangleMesh);
                for (int j = 0; j < fadeLength; j++)
                    mesh.verticeColors[j] = Color.Lerp(bodyColor, faceColor, Mathf.Pow(j / (float)(fadeLength - 1), 0.5f));
                for (int j = fadeLength; j < mesh.verticeColors.Length; j++)
                    mesh.verticeColors[j] = faceColor;
            }
            base.ApplyPalette(sLeaser, rCam, palette);

        }

        public override void Update()
        {
            PlayerGraphics iGraphics = null;
            if (!iGraphicsRef.TryGetTarget(out iGraphics))
                return;


            var player = (iGraphics.owner as Player);
            Vector2 drawPos1 = iGraphics.drawPositions[1, 1];
            Vector2 headPos = iGraphics.head.pos;

            //通过身体角度判断移动
            var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, (headPos - drawPos1).normalized), -22.5f, 22.5f);
            //实际头发偏移
            var nowHairSpacing = HairSpacing * ((Mathf.Abs(moveDeg) > 10) ? 0.3f : 1f);



            for (int i = 0; i < 2; i++)
            {
                var dir = Custom.DirVec(iGraphics.owner.bodyChunks[0].pos, iGraphics.owner.bodyChunks[1].pos).normalized;
                var rootPos = iGraphics.head.pos + (i == 0 ? -1 : 1) * Custom.PerpendicularVector(dir).normalized * nowHairSpacing + dir * -0.2f;

                var num3 = 1f - Mathf.Clamp((Mathf.Abs(iGraphics.owner.bodyChunks[1].vel.x) - 1f) * 0.5f, 0f, 1f);

                Vector2 vector2 = rootPos;
                Vector2 pos = rootPos;
                float num9 = 28f;

                Hairs[i * 4].connectedPoint = new Vector2?(rootPos);
                for (int k = 0; k < 4; k++)
                {
                    Hairs[k + i * 4].Update();
                    Hairs[k + i * 4].vel *= Mathf.Lerp(0.75f, 0.95f, num3 * (1f - iGraphics.owner.bodyChunks[1].submersion));//水中减少速度
                    TailSegment tailSegment7 = Hairs[k + i * 4];
                    tailSegment7.vel.y = tailSegment7.vel.y - Mathf.Lerp(0.1f, 0.5f, num3) * (1f - iGraphics.owner.bodyChunks[1].submersion) * iGraphics.owner.EffectiveRoomGravity;
                    num3 = (num3 * 10f + 1f) / 11f;

                    Hairs[k + i * 4].vel += Custom.DirVec(vector2, Hairs[k + i * 4].pos) * num9 / Vector2.Distance(vector2, Hairs[k + i * 4].pos);
                    num9 *= 0.5f;
                    vector2 = pos;
                    pos = Hairs[k + i * 4].pos;
                }
            }
        }

        readonly TailSegment[] Hairs;
        readonly float HairSpacing = 6f;
        //readonly float MaxLength = 9f;
    }

}
