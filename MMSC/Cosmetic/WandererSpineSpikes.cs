using BepInEx.Logging;
using RWCustom;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Cosmetic
{
	public class WandererSpineSpikes : CosmeticBase
	{
		public WandererSpineSpikes(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
		{
			//this.spritesOverlap = Template.SpritesOverlap.BehindHead;
			float num = Mathf.Lerp(5f, 8f, Mathf.Pow(Random.value, 0.7f));

			this.spineLength = 0;
			foreach (var tail in graphics.tail)
				this.spineLength += tail.connectionRad;
			//this.spineLength += graphics.head.rad;
			this.spineLength *= Custom.ClampedRandomVariation(0.3f, 0.17f, 0.5f);

			this.sizeMin = 0.3f;
			this.sizeMax = 1f;
			
	
			this.sizeMin = Mathf.Min(this.sizeMin, 0.3f);
			this.sizeMax = Mathf.Min(this.sizeMax, 0.6f);
			
			this.sizeExponent = 0.6f;
			this.bumps = 7;
			if (graphics.RenderAsPup)
			{
				bumps = 3;
			}

			this.scaleX = 0.7f*2;
			this.graphic = 0;
			this.numberOfSprites = ((this.colored) ? (this.bumps * 2) : this.bumps);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
			{
				float num = Mathf.InverseLerp(this.startSprite, (this.startSprite + this.bumps - 1), (float)i);
				PlayerGraphics.PlayerSpineData SpineData = this.iGraphics.SpinePosition(Mathf.Lerp(0, 1 , num), timeStacker);


				sLeaser.sprites[i].x = SpineData.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = SpineData.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.VecToDeg(SpineData.perp);
				float num2 = Mathf.Lerp(this.sizeMin, sizeMax, Mathf.Sin(Mathf.Pow(num, this.sizeExponent) * 3.1415927f));
				sLeaser.sprites[i].scaleX = Mathf.Sign(this.iGraphicsDepthRotation) * this.scaleX * num2;
				sLeaser.sprites[i].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphicsDepthRotation)))*1;
				if (colored)
				{
					sLeaser.sprites[i + this.bumps].x = SpineData.outerPos.x - camPos.x;
					sLeaser.sprites[i + this.bumps].y = SpineData.outerPos.y - camPos.y;
					sLeaser.sprites[i + this.bumps].rotation = Custom.AimFromOneVectorToAnother(-SpineData.perp * SpineData.depthRotation, SpineData.perp * SpineData.depthRotation);
					sLeaser.sprites[i + this.bumps].scaleX = Mathf.Sign(this.iGraphicsDepthRotation) * this.scaleX * num2 ;
					sLeaser.sprites[i + this.bumps].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphicsDepthRotation))) * 1;
				}
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic.ToString(), true);
				sLeaser.sprites[i].anchorY = 0.15f;
				if (colored)
				{
					sLeaser.sprites[i + this.bumps] = new FSprite("LizardScaleB" + this.graphic.ToString(), true);
					sLeaser.sprites[i + this.bumps].anchorY = 0.15f;
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = this.startSprite; i < this.startSprite + this.bumps; i++)
			{
				if (this.colored)
				{
					float f = Mathf.InverseLerp(startSprite, (startSprite + this.bumps - 1), i);
					sLeaser.sprites[i + this.bumps].color = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics, rCam), Mathf.Pow(f, 0.5f));
				}
			}
		}


	
		private bool colored = true;
		public int bumps;
		public int graphic;

		float sizeExponent = 0.6f;
		float spineLength = 0.6f;
		float iGraphicsDepthRotation = 1f;

		float sizeMin;
		float sizeMax;
		float scaleX;

	}
}
