using BepInEx.Logging;
using Pkuyo.Wanderer.Cosmetic;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Pkuyo.Wanderer.Characher
{
    class WandererGraphicsFeature : FeatureBase
	{
		public WandererGraphicsFeature(ManualLogSource log) :base(log)
        {
			wandererGraphics = new ConditionalWeakTable<PlayerGraphics, WandererGraphics>();
		}

		public override void OnModsInit(RainWorld rainWorld)
        {
			On.PlayerGraphics.ctor += PlayerGraphics_ctor;
			On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
           
			IL.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSpritesIL;
            IL.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSpritesIL;
			_log.LogDebug("WandererGraphics Init");

		}



        private void PlayerGraphics_InitiateSpritesIL(ILContext il)
        {
			//毛绒绒毛茸茸
			var c = new ILCursor(il);
			if(c.TryGotoNext(i => i.MatchLdstr("HeadA0")))
            {
				c.Remove();
				c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
				c.EmitDelegate<Func<PlayerGraphics, string>>((self) =>
				 {
					 return (self.owner as Player).slugcatStats.name.value == "wanderer" ? "HeadB0" : "HeadA0";
				 });
			}
        }

		private void PlayerGraphics_DrawSpritesIL(ILContext il)
		{
			//毛绒绒毛茸茸
			var c = new ILCursor(il);
			if (c.TryGotoNext(i => i.MatchLdstr("HeadA")))
			{
				c.Remove();
				c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
				c.EmitDelegate<Func<PlayerGraphics, string>>((self) =>
				{
					return (self.owner as Player).slugcatStats.name.value == "wanderer" ? "HeadB" : "HeadA";
				});
			}
		}



        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
			orig(self, ow);

			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			WandererGraphics graphics;
			if (!wandererGraphics.TryGetValue(self, out graphics))
				wandererGraphics.Add(self,new WandererGraphics(self, _log));

		}

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
			orig(self, sLeaser, rCam, timeStacker, camPos);

			WandererGraphics graphics;
			if (wandererGraphics.TryGetValue(self, out graphics))
				graphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);



		}

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
			orig(self, sLeaser, rCam);

			WandererGraphics graphics;
			if (wandererGraphics.TryGetValue(self, out graphics))
				graphics.InitiateSprites(sLeaser, rCam);

		}

		private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			WandererGraphics graphics;
			if (wandererGraphics.TryGetValue(self, out graphics))
				graphics.ApplyPalette(sLeaser, rCam, palette);

		}

		private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
		{
			orig(self);

			WandererGraphics graphics;
			if (wandererGraphics.TryGetValue(self, out graphics))
				graphics.Update();
		}



		ConditionalWeakTable<PlayerGraphics, WandererGraphics> wandererGraphics;



	}

	class WandererGraphics
    {

		public WandererGraphics(PlayerGraphics self, ManualLogSource log)
		{
			Cosmetics = new List<CosmeticBase>();
			this.self = self;
			_log = log;
			OriginSprites = EndSprites = -1;

			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			AddCosmetic(new WandererTailEffect(self, _log));
			AddCosmetic(new WandererLongHair(self, _log));
			AddCosmetic(new WandererTailFin(self, _log));
			AddCosmetic(new WandererSpineSpikes(self, _log));
			AddCosmetic(new WandererBumpHawk(self, _log));
		}

		private void AddCosmetic(CosmeticBase cosmetic)
		{
			if (cosmetic is WandererTailEffect)
			{
				TailEffect = (cosmetic as WandererTailEffect);
				return;
			}
			Cosmetics.Add(cosmetic);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			foreach (var cosmetic in Cosmetics)
				cosmetic.DrawSprites(sLeaser, rCam, timeStacker, camPos);


		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

	

			TailEffect.InitiateSprites(sLeaser, rCam);

			//第一次设定位置
			if (OriginSprites == -1)
			{
				OriginSprites = sLeaser.sprites.Length;
				var startLength = sLeaser.sprites.Length;

				foreach (var cosmetic in Cosmetics)
				{
					cosmetic.startSprite = startLength;
					startLength += cosmetic.numberOfSprites;
				}
				Array.Resize(ref sLeaser.sprites, startLength);
				EndSprites = startLength;
			}
			else
				Array.Resize(ref sLeaser.sprites, EndSprites);

			foreach (var cosmetic in Cosmetics)
				cosmetic.InitiateSprites(sLeaser, rCam);

			FContainer newContatiner = rCam.ReturnFContainer("Midground");
			for (int i = OriginSprites; i < EndSprites; i++)
				newContatiner.AddChild(sLeaser.sprites[i]);


		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			TailEffect.ApplyPalette(sLeaser, rCam, palette);
			foreach (var cosmetic in Cosmetics)
				cosmetic.ApplyPalette(sLeaser, rCam, palette);
		}

		public void Update()
		{

			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			foreach (var cosmetic in Cosmetics)
				cosmetic.Update();
		}


		PlayerGraphics self;
		ManualLogSource _log;
		WandererTailEffect TailEffect;
		List<CosmeticBase> Cosmetics;

		public int OriginSprites;
		public int EndSprites;
	}

}
