using BepInEx.Logging;
using CoralBrain;
using Fisobs.Core;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System.Reflection;
using UnityEngine;

namespace Pkuyo.Wanderer
{
    class CoolObjectHook : HookBase
    {
        CoolObjectHook(ManualLogSource log) : base(log)
        {

        }
        static public CoolObjectHook Instance(ManualLogSource log = null)
        {
            if (_instance == null)
                _instance = new CoolObjectHook(log);
            return _instance;
        }

        public override void OnModsInit(RainWorld rainWorld)
        {
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.ReleaseGrasp += Player_ReleaseGrasp;

            On.RegionGate.Update += RegionGate_Update;
            On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;
            On.GateKarmaGlyph.ShouldPlayCitizensIDAnimation += GateKarmaGlyph_ShouldPlayCitizensIDAnimation;

            On.AbstractPhysicalObject.UsesAPersistantTracker += AbstractPhysicalObject_UsesAPersistantTracker;

            BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
            BindingFlags myMethodFlags = BindingFlags.Static | BindingFlags.Public;

            Hook overseerColourHook = new Hook(
                typeof(GateKarmaGlyph).GetProperty("GetToColor", propFlags).GetGetMethod(),
                typeof(CoolObjectHook).GetMethod("GateKarmaGlyph_GetToColor_get", myMethodFlags)
            );
            Content.Register(new CoolObjectFisob());
        }

        private bool AbstractPhysicalObject_UsesAPersistantTracker(On.AbstractPhysicalObject.orig_UsesAPersistantTracker orig, AbstractPhysicalObject abs)
        {
            var re = orig(abs);
            if (abs.type == CoolObjectFisob.CoolObject)
                return true;
            return re;
        }

        private void Player_ReleaseGrasp(On.Player.orig_ReleaseGrasp orig, Player self, int grasp)
        {
            if (WandererCharacterMod.WandererOptions.PreventToolFalling.Value && self.grasps[grasp] != null && self.grasps[grasp].grabbed != null && self.grasps[grasp].grabbed is CoolObject && self.bodyMode == WandererModEnum.PlayerBodyModeIndex.ClimbBackWall)
                return;
            orig(self,grasp);
        }

        private int GateKarmaGlyph_ShouldPlayCitizensIDAnimation(On.GateKarmaGlyph.orig_ShouldPlayCitizensIDAnimation orig, GateKarmaGlyph self)
        {
            var re = orig(self);
            CoolObject coolObject;
            if (TryGetObject(self.gate, out coolObject))
            {
                return 0;
            }
            return re;
        }


        private void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            CoolObject coolObject;
            if (TryGetObject(self.gate, out coolObject) && coolObject.UnlockingGate && coolObject.unlockAnim != null)
            {
                if (self.gate.karmaGlyphs[self.gate.letThroughDir ? 0 : 1] == self)
                {
                    var anim = coolObject.unlockAnim;
                    if (anim.counter > 100 && anim.counter <= 200)
                    {
                        sLeaser.sprites[1].color = Color.Lerp(self.GetToColor, Color.white, ((float)anim.RandCounter / anim.RandCounterTot));
                        if (anim.RandCounter == 0)
                        {
                            sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + Random.Range(1, 5));
                            coolObject.unlockAnim.RandCounter = Random.Range(8, 15);
                            self.room.PlaySound(SoundID.SS_AI_Text, 0f, 3f, 1f);
                        }
                        if (anim.counter == 200)
                        {
                            sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol0");
                            self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Data_Bit, 0f, 1f, 0.5f + Random.value * 2f);
                        }
                    }
                }
            }
        }

        public static Color GateKarmaGlyph_GetToColor_get(orig_GetToColor orig, GateKarmaGlyph self)
        {
            var color = orig(self);
            CoolObject coolObject;
            if (TryGetObject(self.gate, out coolObject) && coolObject.UnlockingGate)
            {
                if (coolObject.unlockAnim.counter >= 100)
                    return Color.Lerp(self.myDefaultColor, new Color(0f, 0f, 1f), 0.4f + 0.5f * Mathf.Sin((coolObject.unlockAnim.counter - 100) / 6f));
            }
            return color;
        }
        private void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            orig(self, eu);
            if (self.mode == RegionGate.Mode.MiddleClosed)
            {
                int num = self.PlayersInZone();
                if (num > 0 && num < 3)
                {
                    CoolObject coolObject;
                    if (TryGetObject(self, out coolObject) && self.MeetRequirement == false && !coolObject.UnlockingGate)
                    {
                        UnlockGateAnimation unlockGate = new UnlockGateAnimation(coolObject, self);
                        self.room.AddObject(unlockGate);
                        unlockGate.AddCirleAndLabel();
                    }
                }
            }
        }

        private static bool TryGetObject(RegionGate self, out CoolObject cool)
        {
            cool = null;
            if (self.room != null)
            {
                foreach (var player in self.room.PlayersInRoom)
                    if (player != null && player.grasps != null && player.grasps[0] != null  && player.grasps[0].grabbed is CoolObject)
                    {
                        cool = player.grasps[0].grabbed as CoolObject;
                        return true;
                    }
            }
            return false;
        }
        private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.wantToPickUp > 0)
            {
                if (self.grasps[0] != null && self.grasps[0].grabbed is CoolObject && self.canJump <= 0 &&
                    self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != global::Player.BodyModeIndex.CorridorClimb &&
                    self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam &&
                    self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb &&
                    self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                    (self.grasps[0].grabbed as CoolObject).Open();
            }
        }

        public delegate Color orig_GetToColor(GateKarmaGlyph self);

        static private CoolObjectHook _instance;
    }
    public class UnlockGateAnimation : CosmeticSprite, IOwnProjectedCircles
    {
        public UnlockGateAnimation(CoolObject origin, RegionGate gate)
        {
            this.origin = origin;
            this.gate = gate;
            origin.unlockAnim = this;
            lastPos = pos = origin.firstChunk.pos;
            toPos = gate.karmaGlyphs[gate.letThroughDir ? 0 : 1].pos;
            origin.AddSmallProjectedCircle();


        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("Futile_White", true);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLight"];
            sLeaser.sprites[0].color = new Color(0, 162 / 255f, 232 / 255f);
            sLeaser.sprites[0].alpha = 0.2f;
            sLeaser.sprites[1] = new FSprite("Futile_White", true);
            sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["ToolHoloGird"];
            sLeaser.sprites[1].color = new Color(104 / 255f, 195 / 255f, 232 / 255f);

            rCam.ReturnFContainer("Shortcuts").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Shortcuts").AddChild(sLeaser.sprites[1]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (circle != null)
            {
                sLeaser.sprites[0].scale = Custom.LerpMap(Mathf.Min(circle.rad, 60f), 0, 60, 5, 42);
                sLeaser.sprites[1].scale = circle.Rad(timeStacker) / 7;
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].x = pos.x - camPos.x;
                sLeaser.sprites[i].y = pos.y - camPos.y;
            }
            if (room != rCam.room)
            {
                Destroy();
            }
        }

        public override void Destroy()
        {
            gate = null;
            origin = null;
            base.Destroy();
        }

        public void CircleMovement()
        {
            //简单寻路
            Vector2 vector = Custom.DirVec(pos, toPos);
            if (!room.readyForAI || !Custom.DistLess(pos, toPos, 2000f) || room.VisualContact(pos, toPos))
            {
                path = null;
                quickPather = null;
            }
            //查找路径中
            else if (path == null)
            {
                if (quickPather == null)
                    quickPather = new QuickPathFinder(room.GetTilePosition(pos), room.GetTilePosition(toPos), room.aimap, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly));

                for (int i = 0; i < 100; i++)
                {
                    quickPather.Update();
                    if (quickPather.status != 0)
                    {
                        path = quickPather.ReturnPath();
                        quickPather = null;
                        break;
                    }
                }
            }
            else
            {
                bool flag = false;
                IntVector2 intVector = new IntVector2(-1, -1);
                for (int j = path.tiles.Length - 1; j >= 0; j--)
                {
                    if (intVector.x == -1 && intVector.y == -1 && room.VisualContact(pos, room.MiddleOfTile(path.tiles[j])))
                        intVector = path.tiles[j];

                    if (room.VisualContact(toPos, room.MiddleOfTile(path.tiles[j])))
                        flag = true;

                    if ((intVector.x != -1 || intVector.y != -1) && flag)
                        break;

                }
                if (!flag || (intVector.x == -1 && intVector.y == -1))
                    path = null;
                else
                    vector = Custom.DirVec(pos, room.MiddleOfTile(intVector));

            }
            bool solid = room.GetTile(pos + (pos - lastPos) * 7f + direction * 30f).Solid;
            if (solid)
            {
                vel *= 0.7f;
                if (room.readyForAI)
                {
                    IntVector2 tilePosition = room.GetTilePosition(pos);
                    for (int k = 0; k < 8; k++)
                    {
                        if (room.aimap.getAItile(tilePosition + Custom.eightDirections[k]).terrainProximity > room.aimap.getAItile(tilePosition).terrainProximity)
                        {
                            vector += 0.2f * Custom.eightDirections[k].ToVector2();
                        }
                    }
                    vector.Normalize();
                }
            }
            else if (lastPos != pos)
                vel = vel * Custom.LerpMap(Vector2.Dot((pos - lastPos).normalized, vector), -1f, 1f, 0.85f, 0.97f);

            direction = Vector3.Slerp(direction, vector, (!solid) ? Custom.LerpMap(Vector2.Distance(pos, toPos), 20f, 200f, 1f, 0.3f) : 1f);
            if (Vector2.Distance(pos, toPos) < 2f)
                direction.Scale(new Vector2(0.2f, 0.2f));

            vel += direction;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            CircleMovement();
            counter++;
            vel = Vector2.Lerp(vel, vel + origin.firstChunk.vel, 0.2f);

            if (counter >= 100)
            {
                if (randCounter > 0)
                    randCounter--;
            }
            if (counter == 200)
            {
                ChangeGate();
                circle.getToRad = 0;
                circle.baseRad = 0;
                if (origin != null && !origin.IsOpen && origin.SmallCircle != null && !origin.SmallCircle.slatedForDeletetion)
                {
                    origin.SmallCircle.getToRad = 0;
                    origin.SmallCircle.baseRad = 0;
                }
            }
            else if (circle.rad == 0)
            {
                circle.Destroy();
                slatedForDeletetion = true;
            }
        }

        private void ChangeGate()
        {
            gate.unlocked = true;
        }
        public bool CanHostCircle()
        {
            return true;
        }

        public Vector2 CircleCenter(int index, float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public Room HostingCircleFromRoom()
        {
            return room;
        }

        public void AddCirleAndLabel()
        {
            circle = new ToolProjectedCircle(room, this, 0, 60f);
            room.AddObject(circle);
        }


        ProjectedCircle circle;

        QuickPathFinder quickPather = null;
        QuickPath path = null;
        Vector2 direction;

        Vector2 toPos;
        CoolObject origin;
        RegionGate gate;
        public int counter = 0;

        private int randCounter = 0;
        private int randCounterTot = 0;

        public int RandCounter
        {
            set
            {
                randCounter = randCounterTot = value;
            }
            get
            {
                return randCounter;
            }
        }
        public int RandCounterTot
        {
            get
            {
                return randCounterTot;
            }
        }
    }
}
