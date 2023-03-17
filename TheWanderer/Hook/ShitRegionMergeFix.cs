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
        ShitRegionMergeFix(ManualLogSource log) : base(log)
        {
            On.ModManager.ModMerger.WriteMergedFile += ModMerger_WriteMergedFile;
        }

        static public ShitRegionMergeFix Instance(ManualLogSource log)
        {
            if (_instance == null)
                _instance = new ShitRegionMergeFix(log);
            return _instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
           
        }

        private void ModMerger_WriteMergedFile(On.ModManager.ModMerger.orig_WriteMergedFile orig, ModManager.Mod sourceMod, string sourcePath, string[] mergeLines)
        {
            if (sourceMod.id == "pkuyo.thevanguard")
            {
                List<string> mergedlist = new List<string>(mergeLines);
                try
                {
                    var fileName = sourcePath.Substring(sourcePath.LastIndexOf("\\") + 1);
                    if (fileName.Contains("world"))
                    {
                      
                        var path = AssetManager.ResolveDirectory("modify/world") + "/" + fileName.Substring(fileName.LastIndexOf("_") + 1, fileName.LastIndexOf(".") - fileName.LastIndexOf("_") -1) + "/" + fileName;
                        //if (fileName == "world_sb.txt")
                        //{
                        //    path = AssetManager.ResolveDirectory("test/1") + "/" + fileName;
                        //}
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
                            if (line.Contains("wanderer"))
                                x++;
                        }
                        foreach (var b in a)
                            text.Remove(b);

                        //清理重复项
                        int y = 0;
                        int insertIndex = -1;
                        foreach (var line in mergedlist)
                        {
                            if (line.Contains("wanderer"))
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
