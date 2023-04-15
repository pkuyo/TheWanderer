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
        public ParasiteGraphics(PhysicalObject ow) : base(ow, false)
        {
            parasite = ow as Parasite;
            Random.State state = Random.state;
            Random.InitState(parasite.abstractCreature.ID.RandomSeed);
            Random.state = state;
            label = new FLabel(Custom.GetFont(), "");
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            sLeaser.sprites[1] = new FSprite("Futile_White");
            if (parasite.isFemale)
                sLeaser.sprites[1].color = Color.yellow;
            else if (parasite.isMale)
                sLeaser.sprites[1].color = Color.red;

            sLeaser.sprites[0].width = parasite.bodyChunks[0].rad;
            sLeaser.sprites[1].width = parasite.bodyChunks[1].rad;
            AddToContainer(sLeaser, rCam,null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //别忘了lerp
            sLeaser.sprites[0].x = Mathf.Lerp(parasite.mainBodyChunk.lastPos.x, parasite.mainBodyChunk.pos.x,timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(parasite.mainBodyChunk.lastPos.y, parasite.mainBodyChunk.pos.y, timeStacker) - camPos.y;

            sLeaser.sprites[1].x = Mathf.Lerp(parasite.bodyChunks[1].lastPos.x, parasite.bodyChunks[1].pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[1].y = Mathf.Lerp(parasite.bodyChunks[1].lastPos.y, parasite.bodyChunks[1].pos.y, timeStacker) - camPos.y;

          
            sLeaser.sprites[0].rotation = Custom.VecToDeg(parasite.mainBodyChunk.Rotation);
            sLeaser.sprites[1].rotation = Custom.VecToDeg(parasite.bodyChunks[1].Rotation);

            label.text = parasite.AI.behavior.ToString() + " " +parasite.CurrentSpeed;
            label.x = sLeaser.sprites[0].x;
            label.y = sLeaser.sprites[0].y + 20;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            rCam.ReturnFContainer("HUD").RemoveChild(label);
            rCam.ReturnFContainer("HUD").AddChild(label);
        }

        FLabel label;
        Parasite parasite;
    }
}
