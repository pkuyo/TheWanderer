using BepInEx.Logging;
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

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (sLeaser.sprites[3].element.name.StartsWith("HeadA"))
            {
                sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[3].element.name.Replace("HeadA", "WandererHeadA"));
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
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
                mat.SetColor("_EffectColor_2", GetLoungeColor(owner));
                return true;
            }
            return false;
        }
    }

    class WandererClimbShow : CosmeticBase
    {
        public WandererClimbShow(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
        {
            numberOfSprites = 1;
        }

        
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[startSprite] = new FSprite("Futile_White");
            sLeaser.sprites[startSprite].shader = rCam.game.rainWorld.Shaders["ShowWall"];
            sLeaser.sprites[startSprite].width = 1000;
            sLeaser.sprites[startSprite].height = 1000;
            sLeaser.sprites[startSprite].MoveToBack();
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            PlayerGraphics owner;
            if (!iGraphicsRef.TryGetTarget(out owner))
                return;

            sLeaser.sprites[startSprite].x = owner.head.pos.x - camPos.x;
            sLeaser.sprites[startSprite].y = owner.head.pos.y - camPos.y;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override bool UpdateDirtyShader(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics owner;
            if (!iGraphicsRef.TryGetTarget(out owner))
                return false;
            Material mat;
            if (TryGetMaterial(sLeaser.sprites[startSprite], out mat))
            {
                mat.SetColor("_EffectColor", GetFaceColor(owner));
                return true;
            }
            return false;
        }
    }
}
