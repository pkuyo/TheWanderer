using CoralBrain;
using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using MoreSlugcats;
using Noise;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pkuyo.Wanderer
{
    public class CoolObject : PlayerCarryableItem, IDrawable, IOwnProjectedCircles
    {

        public CoolObject(AbstractPhysicalObject abstractPhysicalObject, bool Vis = true) : base(abstractPhysicalObject)
        {
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, abstractPhysicalObject.Room.realizedRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile), 10f, 0.07f);
            bodyChunkConnections = new BodyChunkConnection[0];
            airFriction = 0.999f;
            gravity = 0.9f;
            bounce = 0.4f;
            surfaceFriction = 0.4f;
            collisionLayer = 2;
            waterFriction = 0.98f;
            buoyancy = 0.4f;
            firstChunk.loudness = 3f;

            _isInit = Vis;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("Wanderer_Tool" + (IsOpen ? 1 : (OpenTimer == 0 ? 0 : 2)), true);
            sLeaser.sprites[0].scale = 0.7f;
            sLeaser.sprites[1] = new FSprite("Futile_White", true);
            sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["GravityDisruptor"];
            sLeaser.sprites[1].scale = 20f;
            sLeaser.sprites[1].alpha = 0f;


            sLeaser.sprites[2] = new FSprite("illustrations/fade", true);
            sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["ToolHoloGird"];
            sLeaser.sprites[2].scale = 10f;
            sLeaser.sprites[2].alpha = 1f;
            sLeaser.sprites[2].color = new Color(0, 162 / 255f, 232 / 255f);



            AddToContainer(sLeaser, rCam, null);
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (isDirty)
            {
                sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("Wanderer_Tool" + (IsOpen ? 1 : (OpenTimer == 0 ? 0 : 2)));
                isDirty = false;
            }
            sLeaser.sprites[0].x = Mathf.Lerp(firstChunk.lastPos.x, firstChunk.pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(firstChunk.lastPos.y, firstChunk.pos.y, timeStacker) - camPos.y;

            if (grabbedBy.Count != 0 && grabbedBy[0] != null && (grabbedBy[0].grabber is Player) && (grabbedBy[0].grabber as Player).inShortcut)
                _isVis = false;
            else if (!_isVis)
                _isVis = true;

            if (sLeaser.sprites[0].isVisible != IsVis)
                foreach (var sprite in sLeaser.sprites)
                    sprite.isVisible = IsVis;

            for (int i = 1; i < 3; i++)
            {
                sLeaser.sprites[i].x = sLeaser.sprites[0].x;
                sLeaser.sprites[i].y = sLeaser.sprites[0].y;
            }

            if (IsOpen)
                sLeaser.sprites[2].alpha = sLeaser.sprites[1].alpha = Mathf.Lerp(sLeaser.sprites[1].alpha, 0.5f, 0.15f);
            else
                sLeaser.sprites[2].alpha = sLeaser.sprites[1].alpha = Mathf.Lerp(sLeaser.sprites[1].alpha, 0f, 0.15f);


        }



        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
        }
        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);

            if (SmallCircle != null && !SmallCircle.slatedForDeletetion)
                SmallCircle.slatedForDeletetion = true;
            if (IsOpen)
                AddSmallProjectedCircle();
        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            if (SmallCircle != null && !SmallCircle.slatedForDeletetion && SmallCircle.rad == 0)
            {
                SmallCircle.slatedForDeletetion = true;
                SmallCircle = null;
            }

            if (OpenTimer > 0)
            {
                if (--OpenTimer == 0)
                {
                    room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_Ready, firstChunk.pos, 1f, 1f);
                    isDirty = true;
                }
                else if (OpenTimer == 400)
                {
                    room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_Off, firstChunk.pos, 1f, 1f);
                    gravity = 0.9f;
                    if ((!UnlockingGate || unlockAnim.counter >= 200) && !SmallCircle.slatedForDeletetion)
                    {
                        SmallCircle.getToRad = 0;
                    }
                    isDirty = true;
                }
            }

            if (FlyTarget != null)
            {
                storyFlyTarget = FlyTarget.mainBodyChunk.pos;
                StoryMovement();
            }
        }

        public Vector2 CircleCenter(int index, float timeStacker)
        {
            return Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        }

        public Room HostingCircleFromRoom()
        {
            return room;
        }

        public bool CanHostCircle()
        {
            return !slatedForDeletetion;
        }

        public void AddProjectedCircle()
        {
            room.AddObject(new ProjectedCircle(room, this, 0, 0f));
            _isInit = true;
        }

        public void AddSmallProjectedCircle()
        {
            if (SmallCircle == null || SmallCircle.slatedForDeletetion)
                room.AddObject(SmallCircle = new ToolProjectedCircle(room, this, 0, 20f));
        }


        public void StoryMovement()
        {
            //简单寻路
            Vector2 vector = Custom.DirVec(firstChunk.pos, storyFlyTarget);
            if (!room.readyForAI || !Custom.DistLess(firstChunk.pos, storyFlyTarget, 2000f) || room.VisualContact(firstChunk.pos, storyFlyTarget))
            {
                path = null;
                quickPather = null;
            }
            //查找路径中
            else if (path == null)
            {
                if (quickPather == null)
                    quickPather = new QuickPathFinder(room.GetTilePosition(firstChunk.pos), room.GetTilePosition(storyFlyTarget), room.aimap, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly));

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
                    if (intVector.x == -1 && intVector.y == -1 && room.VisualContact(firstChunk.pos, room.MiddleOfTile(path.tiles[j])))
                        intVector = path.tiles[j];

                    if (room.VisualContact(storyFlyTarget, room.MiddleOfTile(path.tiles[j])))
                        flag = true;

                    if ((intVector.x != -1 || intVector.y != -1) && flag)
                        break;

                }
                if (!flag || (intVector.x == -1 && intVector.y == -1))
                    path = null;
                else
                    vector = Custom.DirVec(firstChunk.pos, room.MiddleOfTile(intVector));

            }
            bool solid = room.GetTile(firstChunk.pos + (firstChunk.pos - firstChunk.lastPos) * 7f + direction * 30f).Solid;
            if (solid)
            {
                firstChunk.vel *= 0.7f;
                if (room.readyForAI)
                {
                    IntVector2 tilePosition = room.GetTilePosition(firstChunk.pos);
                    for (int k = 0; k < 8; k++)
                    {
                        if (room.aimap.getAItile(tilePosition + Custom.eightDirections[k]).terrainProximity > room.aimap.getAItile(tilePosition).terrainProximity)
                            vector += 0.2f * Custom.eightDirections[k].ToVector2();
                    }
                    vector.Normalize();
                }
            }
            else if (firstChunk.lastPos != firstChunk.pos)
                firstChunk.vel = firstChunk.vel * Custom.LerpMap(Vector2.Dot((firstChunk.pos - firstChunk.lastPos).normalized, vector), -1f, 1f, 0.85f, 0.97f);

            direction = Vector3.Slerp(direction, vector, (!solid) ? Custom.LerpMap(Vector2.Distance(firstChunk.pos, storyFlyTarget), 20f, 200f, 1f, 0.3f) : 1f);
            if (Vector2.Distance(firstChunk.pos, storyFlyTarget) < 2f)
            {
                direction.Scale(new Vector2(0.2f, 0.2f));
            }
            firstChunk.vel += direction;
        }
        public void Open()
        {
            if (OpenTimer == 0)
            {
                room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_On, base.firstChunk.pos, 1f, 0.75f + Random.value);
                room.InGameNoise(new InGameNoise(base.firstChunk.pos, 1000f, this, 1f));
                OpenTimer = 600 + CdTime;
                gravity = 0.0f;
                AddSmallProjectedCircle();

                isDirty = true;
            }
        }

        bool _isVis = false;

        bool _isInit = true;

        QuickPathFinder quickPather = null;
        QuickPath path = null;
        public Player FlyTarget = null;
        Vector2 direction;
        Vector2 storyFlyTarget;


        public ToolProjectedCircle SmallCircle;


        public UnlockGateAnimation unlockAnim;
        public int OpenTimer = 0;
        public readonly int CdTime = 400;

        public bool IsVis
        {
            get
            { return _isVis && _isInit; }
        }

        public bool IsOpen
        {
            get { return OpenTimer > CdTime; }
        }

        public bool UnlockingGate
        {
            get => unlockAnim != null && unlockAnim.slatedForDeletetion != true;
        }

        bool isDirty = false;
    }



    public class AbstractCoolObject : AbstractPhysicalObject
    {
        public AbstractCoolObject(World world, WorldCoordinate pos, EntityID ID) : base(world, CoolObjectFisob.CoolObject, null, pos, ID)
        {
        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
            {
                realizedObject = new CoolObject(this);
            }
        }

        public void SSRealize()
        {
            base.Realize();
            if (realizedObject == null)
            {
                realizedObject = new CoolObject(this, false);
            }
        }

    }

    sealed class CoolObjectFisob : Fisob
    {
        static public readonly AbstractPhysicalObject.AbstractObjectType CoolObject = new AbstractPhysicalObject.AbstractObjectType("CoolObject", true);
        public CoolObjectFisob() : base(CoolObject)
        {
            Icon = new CoolObjectIcon();
        }

        public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
        {
            var result = new AbstractCoolObject(world, saveData.Pos, saveData.ID);

            return result;
        }


        public override ItemProperties Properties(PhysicalObject forObject)
        {
            return properties;
        }
        private static readonly CoolObjectProperties properties = new CoolObjectProperties();

        sealed class CoolObjectProperties : ItemProperties
        {
            public override void Throwable(Player player, ref bool throwable)
                => throwable = false;

            public override void ScavCollectScore(Scavenger scavenger, ref int score)
                => score = 0;

            public override void ScavWeaponPickupScore(Scavenger scav, ref int score)
                => score = 0;
            public override void ScavWeaponUseScore(Scavenger scav, ref int score)
                => score = 0;

            public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
            {
                grabability = Player.ObjectGrabability.TwoHands;

            }
        }
        sealed class CoolObjectIcon : Icon
        {

            public override int Data(AbstractPhysicalObject apo)
            {
                return 0;
            }

            public override Color SpriteColor(int data)
            {
                return Color.white;
            }

            public override string SpriteName(int data)
            {
                //TODO 更换图片
                return "icon_Tool";
            }

        }
    }
    public class ToolProjectedCircle : ProjectedCircle
    {
        public ToolProjectedCircle(Room room, IOwnProjectedCircles owner, int index, float size = 60) : base(room, owner, index, 0)
        {
            rad = 0.01f;
            getToRad = size;
            baseRad = size;
            depthOffset = 0;
            offScreenConnections = new Vector2[0];
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            isFirst = false;
            if (blinkGetTo < 0.3f)
                blinkGetTo = Random.Range(0.3f, 0.7f);
            updateDepth = false;
            depthOffset = 0;
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            for (int i = 1; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = new Color(0, 162 / 255f, 232 / 255f);
            }

            base.AddToContainer(sLeaser, rCam, newContatiner);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

            for (int i = 1; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = new Color(0, 162 / 255f, 232 / 255f);
            }
            base.ApplyPalette(sLeaser, rCam, palette);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion) return;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (isFirst)
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                    sLeaser.sprites[i].scale = 0;
            if (Rad(timeStacker) <= rad / 5)
                for (int i = 1; i < sLeaser.sprites.Length; i++)
                    sLeaser.sprites[i].color = Color.Lerp(new Color(0.003921569f, 0f, 0f), new Color(0, 162 / 255f, 232 / 255f), Rad(timeStacker) / (rad / 5));

        }
        bool isFirst = true;

    }
}
