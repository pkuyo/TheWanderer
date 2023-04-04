using BepInEx.Logging;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pkuyo.Wanderer
{
    class ShitRegionMergeFix : HookBase
    {
        int a = 0;
        ShitRegionMergeFix(ManualLogSource log) : base(log)
        {
            On.ModManager.ModMerger.WriteMergedFile += ModMerger_WriteMergedFile;
            //On.Player.Update += Player_Update;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers;
            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
        }

        private void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);
            if(self.room.abstractRoom.name == "GATE_MS_IW")
            {
                self.karmaRequirements[0] = MoreSlugcatsEnums.GateRequirement.OELock;
                self.karmaRequirements[1] = MoreSlugcatsEnums.GateRequirement.OELock;
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            a++;
            if (a == 60 && self.room != null)
            {
                _log.LogDebug(self.room.GetWorldCoordinate(self.firstChunk.pos).ToString());
                a = 0;
            }
            orig(self, eu);
        }

        static public ShitRegionMergeFix Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new ShitRegionMergeFix(log);
            return _instance;
        }



        private AbstractCreature RainWorldGame_SpawnPlayers(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
        {
            //角色生成位置修正
            if (self.session.characterStats.name.value == WandererCharacterMod.WandererName && self.world.GetAbstractRoom(location.room).name.ToUpper() == "SB_INTROROOM1")
            {
                location.x = 8;
                location.y = 4;
                location.abstractNode = -1;
            }
            return orig(self, player1, player2, player3, player4, location);
        }

        private void ModMerger_WriteMergedFile(On.ModManager.ModMerger.orig_WriteMergedFile orig, ModManager.Mod sourceMod, string sourcePath, string[] mergeLines)
        {
            if (sourceMod.id == WandererCharacterMod.ModID)
            {
                List<string> mergedlist = new List<string>(mergeLines);
                try
                {
                    var fileName = sourcePath.Substring(sourcePath.LastIndexOf("\\") + 1);
                    if (fileName.Contains("world"))
                    {

                        var path = AssetManager.ResolveDirectory("modify/world") + "/" + fileName.Substring(fileName.LastIndexOf("_") + 1, fileName.LastIndexOf(".") - fileName.LastIndexOf("_") - 1) + "/" + fileName;
                        var text = new List<string>(File.ReadAllLines(path));

						_log.LogMessage("[Merger Fix] World Creature Spawn Fixing (" + fileName + ")");
                        ModManager.ModMerger.WorldFile worldFile = new ModManager.ModMerger.WorldFile(File.ReadAllLines(path));
                        ModManager.ModMerger.WorldFile worldFile2 = new ModManager.ModMerger.WorldFile(mergeLines);

						List<ModManager.ModMerger.WorldRoomSpawn> list2 = new List<ModManager.ModMerger.WorldRoomSpawn>();
						List<ModManager.ModMerger.WorldRoomSpawn> list3 = new List<ModManager.ModMerger.WorldRoomSpawn>();
						for (int num9 = 0; num9 < worldFile2.creatures.Count; num9++)
						{
							if (worldFile2.creatures[num9].excludeMode)
							{
								list2.Add(worldFile2.creatures[num9]);
							}
							else
							{
								list3.Add(worldFile2.creatures[num9]);
							}
						}
						for (int num10 = 0; num10 < list2.Count; num10++)
						{
							ModManager.ModMerger.WorldRoomSpawn worldRoomSpawn = list2[num10];
							bool flag5 = false;
							string[] source = worldRoomSpawn.character.Split(new char[]{','});
							for (int num11 = worldFile.creatures.Count - 1; num11 >= 0; num11--)
							{
								if (worldFile.creatures[num11].roomName == worldRoomSpawn.roomName && worldFile.creatures[num11].lineageDen == worldRoomSpawn.lineageDen)
								{
									if (worldFile.creatures[num11].excludeMode)
									{
										flag5 = true;
										worldFile.creatures[num11] = worldRoomSpawn;
									}
									else if (worldFile.creatures[num11].character == "" || !source.Contains(worldFile.creatures[num11].character))
									{
										worldFile.creatures.RemoveAt(num11);
									}
								}
							}
							if (!flag5)
							{
								worldFile.creatures.Add(worldRoomSpawn);
							}
						}
						for (int num12 = 0; num12 < list3.Count; num12++)
						{
							ModManager.ModMerger.WorldRoomSpawn worldRoomSpawn2 = list3[num12];
							if (worldRoomSpawn2.lineageDen >= 0 && worldRoomSpawn2.roomName == "OFFSCREEN")
							{
								worldFile.creatures.Add(worldRoomSpawn2);
							}
							else
							{
								bool flag6 = false;
								for (int num13 = worldFile.creatures.Count - 1; num13 >= 0; num13--)
								{
									if (worldFile.creatures[num13].roomName == worldRoomSpawn2.roomName && worldFile.creatures[num13].lineageDen == worldRoomSpawn2.lineageDen)
									{
										if (!worldFile.creatures[num13].excludeMode && worldFile.creatures[num13].character == worldRoomSpawn2.character)
										{
											worldFile.creatures[num13] = worldRoomSpawn2;
											flag6 = true;
										}
										if (worldFile.creatures[num13].excludeMode && worldRoomSpawn2.character != "" && !worldFile.creatures[num13].character.Split(new char[]{','}).Contains(worldRoomSpawn2.character))
										{
											ModManager.ModMerger.WorldRoomSpawn worldRoomSpawn3 = worldFile.creatures[num13];
											worldRoomSpawn3.character = worldRoomSpawn3.character + "," + worldRoomSpawn2.character;
										}
									}
								}
								if (!flag6)
								{
									worldFile.creatures.Add(worldRoomSpawn2);
								}
							}
						}
						foreach (string item in worldFile2.migrationBlockages)
						{
							if (!worldFile.migrationBlockages.Contains(item))
							{
								worldFile.migrationBlockages.Add(item);
							}
						}
						foreach (string item2 in worldFile2.unknownContextLines)
						{
							if (!worldFile.unknownContextLines.Contains(item2))
							{
								worldFile.unknownContextLines.Add(item2);
							}
						}
					}
				}
                catch (Exception e)
                {
                    _log.LogError("[Merger Fix] Error" + e.Message + "\n" + e.StackTrace);
                }
            }
            orig(sourceMod, sourcePath, mergeLines);
        }

        static ShitRegionMergeFix _instance;
    }
}
