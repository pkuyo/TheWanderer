using BepInEx.Logging;
using MMSC.Cosmetic;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMSC.Characher
{
    class WandererGraphics : FeatureBase
	{
		public WandererGraphics(ManualLogSource log) :base(log)
        {
			Cosmetics = new Dictionary<PlayerGraphics, List<CosmeticBase>>();
			EndSprites = new Dictionary<PlayerGraphics, int>();
			OriginSprites = new Dictionary<PlayerGraphics, int>();
			TailEffect = new Dictionary<PlayerGraphics, WandererTailEffect>();
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


        public void AddCosmetic(PlayerGraphics graphics,CosmeticBase cosmetic)
        {
			if(!Cosmetics.ContainsKey(graphics))
				Cosmetics.Add(graphics, new List<CosmeticBase>());
			Cosmetics[graphics].Add(cosmetic);
		}

        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
			orig(self, ow);

			foreach (var a in Cosmetics)
				if (a.Key == null)
				{
					Cosmetics.Remove(a.Key);
					TailEffect.Remove(a.Key);
				}


			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;
			if (!TailEffect.ContainsKey(self))
				TailEffect.Add(self, new WandererTailEffect(self, _log));//必须放在第一个初始化！
			else
				TailEffect[self] = new WandererTailEffect(self, _log);

			AddCosmetic(self,new WandererLongHair(self, _log));
			AddCosmetic(self, new WandererTailFin(self,_log));
			AddCosmetic(self, new WandererSpineSpikes(self, _log));
			AddCosmetic(self, new WandererBumpHawk(self, _log));
		}

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			foreach (var cosmetic in Cosmetics[self])
				cosmetic.DrawSprites(sLeaser, rCam, timeStacker, camPos);
				
		
        }

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
	
			orig(self, sLeaser, rCam);
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			TailEffect[self].InitiateSprites(sLeaser, rCam);

			if (!OriginSprites.ContainsKey(self))
			{
				OriginSprites.Add(self, sLeaser.sprites.Length);
				var startLength = sLeaser.sprites.Length;
				
				foreach (var cosmetic in Cosmetics[self])
				{
					cosmetic.startSprite = startLength;
					startLength += cosmetic.numberOfSprites;
				}
				Array.Resize(ref sLeaser.sprites, startLength);
				EndSprites.Add(self, startLength);
			}
			else
				Array.Resize(ref sLeaser.sprites, EndSprites[self]);

			foreach (var cosmetic in Cosmetics[self])
				cosmetic.InitiateSprites(sLeaser, rCam);

			FContainer newContatiner=rCam.ReturnFContainer("Midground");
			for (int i = OriginSprites[self]; i < EndSprites[self]; i++)
				newContatiner.AddChild(sLeaser.sprites[i]);
			

		}

		private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			TailEffect[self].ApplyPalette(sLeaser, rCam,palette);
			foreach (var cosmetic in Cosmetics[self])
				cosmetic.ApplyPalette(sLeaser, rCam, palette);
		}

		private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
		{
			orig(self);
			if ((self.owner as Player).slugcatStats.name.value != "wanderer")
				return;

			foreach (var cosmetic in Cosmetics[self])
				cosmetic.Update();
		}



		Dictionary<PlayerGraphics, WandererTailEffect> TailEffect;

		Dictionary<PlayerGraphics, int> OriginSprites;

		Dictionary<PlayerGraphics, List<CosmeticBase>> Cosmetics;

		Dictionary<PlayerGraphics, int> EndSprites;

	}

}
