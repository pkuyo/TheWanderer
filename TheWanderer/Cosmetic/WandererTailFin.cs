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
	public class WandererTailFin : CosmeticBase
	{
		public WandererTailFin(PlayerGraphics graphics, ManualLogSource log) : base(graphics, log)
		{
			this.graphic = 1;
			this.bumps = 2;//count
			this.colored = true;

			this.spineLength = 0;
				this.spineLength += graphics.tail[graphics.tail.Length-1].connectionRad;
			this.spineLength *= Custom.ClampedRandomVariation(0.3f, 0.17f, 0.5f);

			this.numberOfSprites = ((!this.colored) ? this.bumps : (this.bumps * 2)) * 2;
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
				{
					float num2 = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)j);
					PlayerGraphics.PlayerSpineData spineData = this.iGraphics.SpinePosition(Mathf.Lerp(0.8f, 1f, num2), timeStacker);


					if (i == 0)
					{
						sLeaser.sprites[j + num].x = spineData.outerPos.x - camPos.x;
						sLeaser.sprites[j + num].y = spineData.outerPos.y - camPos.y;
					}
					else if (i == 1)
					{
						sLeaser.sprites[j + num].x = spineData.pos.x + (spineData.pos.x - spineData.outerPos.x) * 0.85f - camPos.x;
						sLeaser.sprites[j + num].y = spineData.pos.y + (spineData.pos.y - spineData.outerPos.y) * 0.85f - camPos.y;
					}
					sLeaser.sprites[j + num].rotation = Custom.VecToDeg(Vector2.Lerp(spineData.perp * spineData.depthRotation, spineData.dir * (float)((i != 1) ? 1 : -1), num2));
					float num3 = this.scale * Mathf.Lerp(this.sizeMin, 1f, Mathf.Sin(Mathf.Pow(num2, this.sizeExponent) * 3.1415927f));
					sLeaser.sprites[j + num].scaleX = Mathf.Sign(this.iGraphicsDepthRotation) * this.thickness * num3;
					sLeaser.sprites[j + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphicsDepthRotation))) * ((i != 1) ? 1f : (-this.undersideSize)) * ScaleY;
					if (this.colored)
					{
						if (i == 0)
						{
							sLeaser.sprites[j + this.bumps + num].x = spineData.outerPos.x - camPos.x;
							sLeaser.sprites[j + this.bumps + num].y = spineData.outerPos.y - camPos.y;
						}
						else if (i == 1)
						{
							sLeaser.sprites[j + this.bumps + num].x = spineData.pos.x + (spineData.pos.x - spineData.outerPos.x) * 0.85f - camPos.x;
							sLeaser.sprites[j + this.bumps + num].y = spineData.pos.y + (spineData.pos.y - spineData.outerPos.y) * 0.85f - camPos.y;
						}
						sLeaser.sprites[j + this.bumps + num].rotation = Custom.VecToDeg(Vector2.Lerp(spineData.perp * spineData.depthRotation, spineData.dir * (float)((i != 1) ? 1 : -1), num2));
						sLeaser.sprites[j + this.bumps + num].scaleX = Mathf.Sign(this.iGraphicsDepthRotation) * this.thickness * num3;
						sLeaser.sprites[j + this.bumps + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphicsDepthRotation))) * ((i != 1) ? 1f : (-this.undersideSize)) * ScaleY;
						if (i == 1)
						{
							sLeaser.sprites[j + this.bumps + num].alpha = Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(this.iGraphicsDepthRotation)), 0.2f);
						}
					}
				}
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
				{
					Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)j);
					sLeaser.sprites[j + num] = new FSprite("LizardScaleA" + this.graphic.ToString(), true);
					sLeaser.sprites[j + num].anchorY = 0.15f;
					if (this.colored)
					{
						sLeaser.sprites[j + this.bumps + num] = new FSprite("LizardScaleB" + this.graphic.ToString(), true);
						sLeaser.sprites[j + this.bumps + num].anchorY = 0.15f;
					}
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = base.startSprite; j < base.startSprite + this.bumps; j++)
				{
					//sLeaser.sprites[j + num].color = MainColor;
					if (this.colored)
					{
						float f = Mathf.InverseLerp(startSprite, (startSprite + this.bumps - 1), j);
						sLeaser.sprites[j + this.bumps + num].color = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics,rCam), Mathf.Pow(f, 0.5f));
					}
				}

			}
		}

		private bool colored = true;
		public int bumps;
		public int graphic;

		float scale = 0.66f;
		float sizeMin = 0.3f;
		float thickness = 0.7f*3f;
		float sizeExponent = 0.6f;
		float spineLength = 0.6f;
		float undersideSize = 0.6f;
		float ScaleY = 3f;
		float iGraphicsDepthRotation = 1f;

	}
}
