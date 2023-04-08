using BepInEx.Logging;
using RWCustom;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
    class WandererBodyFront : CosmeticBase
    {
        public WandererBodyFront(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            numberOfSprites = 2;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics owner = null;
            if (!iGraphicsRef.TryGetTarget(out owner))
                return;
            for (int i = 0; i < 2; i++)
            {

                var bodyFront = new CustomFSprite("Wanderer_Front");
                sLeaser.sprites[startSprite + i] = bodyFront;

                bodyFront.MoveBehindOtherNode(sLeaser.sprites[3]);
            }
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            PlayerGraphics owner = null;
            if (!iGraphicsRef.TryGetTarget(out owner))
                return;
            var player = (owner.owner as Player);
            Vector2 drawPos0 = Vector2.Lerp(owner.drawPositions[0, 1], owner.drawPositions[0, 0], timeStacker);
            Vector2 drawPos1 = Vector2.Lerp(owner.drawPositions[1, 1], owner.drawPositions[1, 0], timeStacker);
            Vector2 headPos = Vector2.Lerp(owner.head.lastPos, owner.head.pos, timeStacker);

            //肺活量不足相关
            float breath = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(owner.lastBreath, owner.breath, timeStacker) * 3.1415927f * 2f);
            if (player.aerobicLevel > 0.5f)
            {
                drawPos0 += Custom.DirVec(drawPos1, drawPos0) * Mathf.Lerp(-1f, 1f, breath) * Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel) * 0.5f;
                headPos -= Custom.DirVec(drawPos1, drawPos0) * Mathf.Lerp(-1f, 1f, breath) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel), 1.5f) * 0.75f;
            }

            //通过身体角度判断移动
            var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, (headPos - drawPos1).normalized), -22.5f, 22.5f);

            var bodyPos = (sLeaser.sprites[1].GetPosition() + headPos - rCam.pos) / 2f;
            var bodyDir = Custom.DirVec(drawPos0, drawPos1);
            var bodyW = sLeaser.sprites[1].width;
            var bodyH = sLeaser.sprites[1].height;

            for (int i = 0; i < 2; i++)
            {
                var bodyFront = (sLeaser.sprites[startSprite + i] as CustomFSprite);

                var isLR = ((i == 0) ? -1 : 1);
                var progress = ((moveDeg > 0) ? 1 : -1) * Mathf.InverseLerp(0, 22.5f, Mathf.Abs(moveDeg)); /*中心点偏移系数*/
                var bodySideOffest = bodyW * 0.48f * Vector2.Perpendicular(bodyDir); /*中心点偏移基准量*/
                var SideUpSize = 1 - (((isLR * progress) < 0) ? (isLR * progress) * 0.5f : (isLR * progress));/*边缘上下移动量*/

                bodyFront.MoveVertice(0, bodyPos + (bodySideOffest * progress) - bodyDir * bodyH * StartHeight);
                bodyFront.MoveVertice(1, bodyPos + (bodySideOffest * progress) - bodyDir * bodyH * (HeightSize + StartHeight));
                bodyFront.MoveVertice(2, bodyPos + (bodySideOffest * isLR) - bodyDir * bodyH * ((SideHeight * SideUpSize) + StartHeight));
                bodyFront.MoveVertice(3, bodyPos + (bodySideOffest * isLR) - bodyDir * bodyH * ((SideHeight * SideUpSize) + HeightSize * 0.95f/*透视*/ + StartHeight));


            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < 2; i++)
            {
                var bodyFront = (sLeaser.sprites[startSprite + i] as CustomFSprite);
                PlayerGraphics owner = null;

                if (iGraphicsRef.TryGetTarget(out owner))
                {
                    var bodyColor = GetBodyColor(owner);
                    var faceColor = GetFaceColor(owner);

                    bodyFront.verticeColors[0] = Color.Lerp(bodyColor, faceColor, 1);
                    bodyFront.verticeColors[1] = Color.Lerp(bodyColor, faceColor, 1);
                    bodyFront.verticeColors[2] = Color.Lerp(bodyColor, faceColor, 0.5f);
                    bodyFront.verticeColors[3] = Color.Lerp(bodyColor, faceColor, 0.5f);


                }
            }
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        private readonly float HeightSize = 0.5f;
        private readonly float SideHeight = 0.1f;
        private readonly float StartHeight = -0.35f;
    }

}
