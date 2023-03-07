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
			//spritesOverlap = Template.SpritesOverlap.BehindHead;
			float num = Mathf.Lerp(5f, 8f, Mathf.Pow(Random.value, 0.7f));

			spineLength = 0;
			foreach (var tail in graphics.tail)
				spineLength += tail.connectionRad;
			//spineLength += graphics.head.rad;
			spineLength *= Custom.ClampedRandomVariation(0.3f, 0.17f, 0.5f);

			sizeMin = 0.3f;
			sizeMax = 1f;
			
	
			sizeMin = Mathf.Min(sizeMin, 0.3f);
			sizeMax = Mathf.Min(sizeMax, 0.6f);
			
			sizeExponent = 0.6f;
			bumps = 7;
			if (graphics.RenderAsPup)
			{
				bumps = 3;
			}

			scaleX = 0.7f*2;
			graphic = 0;
			numberOfSprites = ((colored) ? (bumps * 2) : bumps);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			PlayerGraphics iGraphics = null;
			if (!iGraphicsRef.TryGetTarget(out iGraphics))
				return;

			for (int i = startSprite + bumps - 1; i >= startSprite; i--)
			{
				float num = Mathf.InverseLerp(startSprite, (startSprite + bumps - 1), (float)i);
				PlayerGraphics.PlayerSpineData SpineData = iGraphics.SpinePosition(Mathf.Lerp(0, 1 , num), timeStacker);


				sLeaser.sprites[i].x = SpineData.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = SpineData.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.VecToDeg(SpineData.perp);
				float num2 = Mathf.Lerp(sizeMin, sizeMax, Mathf.Sin(Mathf.Pow(num, sizeExponent) * 3.1415927f));
				sLeaser.sprites[i].scaleX = Mathf.Sign(iGraphicsDepthRotation) * scaleX * num2;
				sLeaser.sprites[i].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(iGraphicsDepthRotation)))*1;
				if (colored)
				{
					sLeaser.sprites[i + bumps].x = SpineData.outerPos.x - camPos.x;
					sLeaser.sprites[i + bumps].y = SpineData.outerPos.y - camPos.y;
					sLeaser.sprites[i + bumps].rotation = Custom.AimFromOneVectorToAnother(-SpineData.perp * SpineData.depthRotation, SpineData.perp * SpineData.depthRotation);
					sLeaser.sprites[i + bumps].scaleX = Mathf.Sign(iGraphicsDepthRotation) * scaleX * num2 ;
					sLeaser.sprites[i + bumps].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(iGraphicsDepthRotation))) * 1;
				}
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = startSprite + bumps - 1; i >= startSprite; i--)
			{
				sLeaser.sprites[i] = new FSprite("LizardScaleA" + graphic.ToString(), true);
				sLeaser.sprites[i].anchorY = 0.15f;
				if (colored)
				{
					sLeaser.sprites[i + bumps] = new FSprite("LizardScaleB" + graphic.ToString(), true);
					sLeaser.sprites[i + bumps].anchorY = 0.15f;
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			PlayerGraphics iGraphics = null;
			if (!iGraphicsRef.TryGetTarget(out iGraphics))
				return;

			for (int i = startSprite; i < startSprite + bumps; i++)
			{
				if (colored)
				{
					float f = Mathf.InverseLerp(startSprite, (startSprite + bumps - 1), i);
					sLeaser.sprites[i + bumps].color = Color.Lerp(GetBodyColor(iGraphics), GetFaceColor(iGraphics, rCam), Mathf.Pow(f, 0.5f));
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
