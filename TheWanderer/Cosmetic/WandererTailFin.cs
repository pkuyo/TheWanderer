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
			graphic = 1;
			bumps = 2;//count
			colored = true;

			spineLength = graphics.tail[graphics.tail.Length-1].connectionRad;
			spineLength *= Custom.ClampedRandomVariation(0.3f, 0.17f, 0.5f);

			numberOfSprites = ((!colored) ? bumps : (bumps * 2)) * 2;
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			PlayerGraphics iGraphics = null;
			if (!iGraphicsRef.TryGetTarget(out iGraphics))
				return;

			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!colored) ? bumps : (bumps * 2));
				for (int j = startSprite + bumps - 1; j >= startSprite; j--)
				{
					float num2 = Mathf.InverseLerp((float)startSprite, (float)(startSprite + bumps - 1), (float)j);
					PlayerGraphics.PlayerSpineData spineData = iGraphics.SpinePosition(Mathf.Lerp(0.8f, 1f, num2), timeStacker);


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
					float num3 = scale * Mathf.Lerp(sizeMin, 1f, Mathf.Sin(Mathf.Pow(num2, sizeExponent) * 3.1415927f));
					sLeaser.sprites[j + num].scaleX = Mathf.Sign(iGraphicsDepthRotation) * thickness * num3 * ScaleX;
					sLeaser.sprites[j + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(iGraphicsDepthRotation))) * ((i != 1) ? 1f : (-undersideSize)) * ScaleY;
					if (colored)
					{
						if (i == 0)
						{
							sLeaser.sprites[j + bumps + num].x = spineData.outerPos.x - camPos.x;
							sLeaser.sprites[j + bumps + num].y = spineData.outerPos.y - camPos.y;
						}
						else if (i == 1)
						{
							sLeaser.sprites[j + bumps + num].x = spineData.pos.x + (spineData.pos.x - spineData.outerPos.x) * 0.85f - camPos.x;
							sLeaser.sprites[j + bumps + num].y = spineData.pos.y + (spineData.pos.y - spineData.outerPos.y) * 0.85f - camPos.y;
						}
						sLeaser.sprites[j + bumps + num].rotation = Custom.VecToDeg(Vector2.Lerp(spineData.perp * spineData.depthRotation, spineData.dir * (float)((i != 1) ? 1 : -1), num2));
						sLeaser.sprites[j + bumps + num].scaleX = Mathf.Sign(iGraphicsDepthRotation) * thickness * num3 * ScaleX;
						sLeaser.sprites[j + bumps + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(iGraphicsDepthRotation))) * ((i != 1) ? 1f : (-undersideSize)) * ScaleY;
						if (i == 1)
						{
							sLeaser.sprites[j + bumps + num].alpha = Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(iGraphicsDepthRotation)), 0.2f);
						}
					}
				}
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!colored) ? bumps : (bumps * 2));
				for (int j = startSprite + bumps - 1; j >= startSprite; j--)
				{
					Mathf.InverseLerp((float)startSprite, (float)(startSprite + bumps - 1), (float)j);
					sLeaser.sprites[j + num] = new FSprite("LizardScaleA" + graphic.ToString(), true);
					sLeaser.sprites[j + num].anchorY = 0.15f;
					if (colored)
					{
						sLeaser.sprites[j + bumps + num] = new FSprite("LizardScaleB" + graphic.ToString(), true);
						sLeaser.sprites[j + bumps + num].anchorY = 0.15f;
					}
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			PlayerGraphics iGraphics = null;
			if (!iGraphicsRef.TryGetTarget(out iGraphics))
				return;

			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!colored) ? bumps : (bumps * 2));
				for (int j = startSprite; j < startSprite + bumps; j++)
				{
					//sLeaser.sprites[j + num].color = MainColor;
					if (colored)
					{
						float f = Mathf.InverseLerp(startSprite, (startSprite + bumps - 1), j);
						sLeaser.sprites[j + bumps + num].color = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics), Mathf.Pow(f, 0.5f));
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
		float ScaleX = 1f;
		float iGraphicsDepthRotation = 1f;

	}
}
