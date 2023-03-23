using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers;
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
            return orig(self,player1,player2,player3,player4,location);
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
                      
                        var path = AssetManager.ResolveDirectory("modify/world") + "/" + fileName.Substring(fileName.LastIndexOf("_") + 1, fileName.LastIndexOf(".") - fileName.LastIndexOf("_") -1) + "/" + fileName;
        
                        var text = new List<string>(File.ReadAllLines(path));

                        //清理本地文件
                        bool isCreature = false;
                        int x = 0;
                        List<string> a = new List<string>();
                        foreach (var line in text)
                        {
                            if (!isCreature)
                                a.Add(line);

                            //截断除了生物生成的东西
                            if (!isCreature && line.Contains("CREATURES"))
                                isCreature = true;
                            if (isCreature && line.Contains("END CREATURES"))
                            {
                                a.Add(line);
                                isCreature = false;
                            }

                            //计数器
                            if (line.Contains(WandererCharacterMod.WandererName))
                                x++;
                        }
                        foreach (var b in a)
                            text.Remove(b);

                        //清理重复项
                        int y = 0;
                        int insertIndex = -1;
                        foreach (var line in mergedlist)
                        {
                            if (line.Contains(WandererCharacterMod.WandererName))
                            {
                                text.Remove(line);
                                y++;
                            }
                            if (line.Contains("END CREATURES"))
                            {
                                insertIndex = mergedlist.IndexOf(line);
                            }
                        }
                        if (x > y && insertIndex != -1)
                        {
                            foreach (var line in text)
                                mergedlist.Insert(insertIndex, line);
                            _log.LogDebug("[Merger] FileName :" + fileName);
                            _log.LogDebug("[Merger] different : Modded-" + x.ToString() + ", Origin-" + y.ToString());
                            mergeLines = mergedlist.ToArray();
                        }
                        
                    }
                }
                catch(Exception e)
                {
                    _log.LogError("[Merger] Error" + e.Message + "\n" + e.StackTrace);
                }
            }
            orig(sourceMod, sourcePath, mergeLines);
        }

        static ShitRegionMergeFix _instance;
    }
}
