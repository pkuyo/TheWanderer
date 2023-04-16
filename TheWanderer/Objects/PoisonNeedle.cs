using Pkuyo.Wanderer.Post;
using RWCustom;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace Pkuyo.Wanderer.Objects
{
    class AbstractPoisonNeedle : AbstractPhysicalObject
    {
        public AbstractPoisonNeedle(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, WandererEnum.Objects.PoisonNeedle, realizedObject, pos, ID)
        {
        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
            {
                realizedObject = new PoisonNeedle(this);
            }
        }
    }

    class PoisonNeedle : PhysicalObject, IDrawable
    {
        public PoisonNeedle(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            Random.State state = Random.state;
            Random.InitState(this.abstractPhysicalObject.ID.RandomSeed);
            sizeFac = Custom.ClampedRandomVariation(0.8f, 0.2f, 0.5f);
            Random.state = state;

            bodyChunks = new BodyChunk[1];
            bodyChunkConnections = new BodyChunkConnection[0];
            bodyChunks[0] = new BodyChunk(this, 0, default(Vector2), 2.6f * sizeFac, 0.1f);
            airFriction = 0.999f;
            gravity = 0.9f;
            bounce = 0.1f;
            surfaceFriction = 0.4f;
            collisionLayer = 0;
            waterFriction = 0.96f;

            buoyancy = 0.95f;

            lifeTime = Mathf.Lerp(1100f, 2200f, Random.value);
        }
        public bool Stuck
        {
            get
            {
                return mode == DartMaggot.Mode.StuckInTerrain || mode == DartMaggot.Mode.StuckInChunk;
            }
        }
        public void Shoot(Vector2 pos, Vector2 dir, Creature shotBy)
        {
            firstChunk.HardSetPosition(pos);
            firstChunk.vel = dir * 70f;
            ChangeMode(DartMaggot.Mode.Shot);
            this.shotBy = shotBy;
            needleDir = lastNeedleDir = dir;
            room.PlaySound(SoundID.Dart_Maggot_Whizz_By, firstChunk);

            for (int i = Random.Range(0, 5); i >= 0; i--)
                room.AddObject(new WaterDrip(pos, dir * Random.value * 15f + Custom.RNV() * Random.value * 5f, false));
            

        }
        public void ChangeMode(DartMaggot.Mode newMode)
        {
            CollideWithTerrain = (newMode == DartMaggot.Mode.Free);
            if (newMode == DartMaggot.Mode.StuckInChunk && stuckInChunk != null && stuckInChunk.owner is Creature && shotBy != null && shotBy is BigSpider && (shotBy as BigSpider).spitter)
            {
                blindCounter = 700;
                (shotBy as BigSpider).AI.spitModule.CreatureHitByDart((stuckInChunk.owner as Creature).abstractCreature);
            }
            mode = newMode;
        }
        public override void Update(bool eu)
        {
            lastNeedleDir = needleDir;
            canBeHitByWeapons = (mode != DartMaggot.Mode.StuckInChunk);
            if (mode != DartMaggot.Mode.Shot)
            {
                base.Update(eu);
                NormalUpdate();
            }
            if (mode == DartMaggot.Mode.Shot)
            {
                ShotUpdate();
            }
            else if (mode == DartMaggot.Mode.StuckInChunk)
            {
                needleDir = Custom.RotateAroundOrigo(stuckDir, Custom.VecToDeg(stuckInChunk.Rotation));
                firstChunk.pos = StuckInChunkPos(stuckInChunk) + Custom.RotateAroundOrigo(stuckPos, Custom.VecToDeg(stuckInChunk.Rotation));
                if (blindCounter > 0)
                {

                    float t = Mathf.InverseLerp(260f, 30f, (float)blindCounter);
                    if (stuckInChunk.owner is Creature && !(stuckInChunk.owner is Player && !(stuckInChunk.owner as Player).isNPC))
                    {
                        (stuckInChunk.owner as Creature).stun = Math.Max((stuckInChunk.owner as Creature).stun, (int)(Random.value * Mathf.Lerp(8f, 22f, t)));
                        (stuckInChunk.owner as Creature).Blind(40);
                    }
                    else
                    {
                        if (SessionHook.Instance().VignetteHud.VignetteCounter <= 120)
                            SessionHook.Instance().VignetteHud.VignetteCounter += 2;
                        (stuckInChunk.owner as Player).Blind(40);
                        (stuckInChunk.owner as Player).slowMovementStun = Math.Max((stuckInChunk.owner as Player).slowMovementStun, (int)(Random.value * 20f));
                    }
                    blindCounter--;
                    return;
                }
                else if (Random.value < 0.033333335f || abstractPhysicalObject.stuckObjects.Count < 1)
                {
                    Unstuck();
                }
            }
        }

        void NormalUpdate()
        {
            age+= 1f / lifeTime;
            if (Stuck && stuckInChunk == null && Random.Range(0.65f,0.95f) < age)
            {
                Unstuck();
            }
            if (age>=1)
            {
                Destroy();
            }
        }

        public override void Destroy()
        {
            base.Destroy();
        }
        void ShotUpdate()
        {
            firstChunk.lastLastPos = firstChunk.lastPos;
            firstChunk.lastPos = firstChunk.pos;
            Vector2 pos = firstChunk.pos;
            Vector2 toPos = firstChunk.pos + firstChunk.vel;
            needleDir = Custom.DirVec(pos, toPos);

            //撞墙检测
            FloatRect? floatRect = SharedPhysics.ExactTerrainRayTrace(room, pos, toPos);
            Vector2 colLeftButtom = default(Vector2);
            if (floatRect != null)
            {
                colLeftButtom = new Vector2(floatRect.Value.left, floatRect.Value.bottom);
            }
            Vector2 vector3 = toPos;

            //生物检测
            SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, room, pos, ref vector3, 1f, 1, shotBy, false);
            if (floatRect != null && collisionResult.chunk != null)
            {
                if (Vector2.Distance(pos, colLeftButtom) < Vector2.Distance(pos, collisionResult.collisionPoint))
                    collisionResult.chunk = null;
                else
                    floatRect = null;
            }

            //撞墙上了
            if (floatRect != null)
            {
                //夹角在-60,60间
                if (Vector2.Dot(Custom.DirVec(pos, toPos), new Vector2(floatRect.Value.right, floatRect.Value.top)) > 0.5f)
                {
                    stuckPos = colLeftButtom + Custom.DirVec(toPos, pos) * 15f;
                    stuckDir = Custom.DirVec(pos, toPos);
                    firstChunk.pos = stuckPos;
                    room.PlaySound(SoundID.Dart_Maggot_Stick_In_Wall, firstChunk);
                    ChangeMode(DartMaggot.Mode.StuckInTerrain);
                }
                else
                {
                    if (floatRect.Value.right != 0f)
                        firstChunk.vel.x = Mathf.Abs(firstChunk.vel.x) * Mathf.Sign(floatRect.Value.right) * -1f;
                    
                    if (floatRect.Value.top != 0f)
                        firstChunk.vel.y = Mathf.Abs(firstChunk.vel.y) * Mathf.Sign(floatRect.Value.top) * -1f;
                    
                    firstChunk.pos = colLeftButtom + Custom.DirVec(toPos, pos) * 15f;
                    room.PlaySound(SoundID.Dart_Maggot_Bounce_Off_Wall, firstChunk);
                    ChangeMode(DartMaggot.Mode.Free);
                }
            }
            //插到生物上了
            else if (collisionResult.chunk != null)
            {
                firstChunk.pos = collisionResult.collisionPoint + Custom.DirVec(toPos, pos) * 11f;

                //围绕chunk进行旋转
                stuckPos = Custom.RotateAroundOrigo(firstChunk.pos - StuckInChunkPos(collisionResult.chunk), -Custom.VecToDeg(collisionResult.chunk.Rotation));
                stuckDir = Custom.RotateAroundOrigo(Custom.DirVec(pos, toPos), -Custom.VecToDeg(collisionResult.chunk.Rotation));

                stuckInChunk = collisionResult.chunk;
                if (stuckInChunk.owner is Creature)
                {
                    (stuckInChunk.owner as Creature).Violence(firstChunk, new Vector2?(Custom.DirVec(pos, toPos) * 3f), stuckInChunk, null, Creature.DamageType.Stab, 0.07f, 3f);
                }
                else
                {
                    stuckInChunk.vel += Custom.DirVec(pos, toPos) * 3f / stuckInChunk.mass;
                }

                room.PlaySound(SoundID.Dart_Maggot_Stick_In_Creature, firstChunk);
                ChangeMode(DartMaggot.Mode.StuckInChunk);
            }
            else
            {
                firstChunk.pos += firstChunk.vel;
            }
            firstChunk.vel.y -= gravity;
            if (firstChunk.vel.magnitude < 30f)
            {
                ChangeMode(DartMaggot.Mode.Free);
            }
        }

        void Unstuck()
        {
            if (stuckInChunk != null)
            {
                firstChunk.vel = stuckInChunk.vel + Custom.RNV() * Random.value * 2f;
                stuckInChunk = null;
            }
            else
            {
                firstChunk.vel = Custom.RNV() * Random.value * 2f;
            }
            for (int i = abstractPhysicalObject.stuckObjects.Count - 1; i >= 0; i--)
                if (abstractPhysicalObject.stuckObjects[i] is DartMaggot.DartMaggotStick && abstractPhysicalObject.stuckObjects[i].A == abstractPhysicalObject)            
                    abstractPhysicalObject.stuckObjects[i].Deactivate();
               
            ChangeMode(DartMaggot.Mode.Free);

        }
        public Vector2 StuckInChunkPos(BodyChunk chunk)
        {
            if (chunk.owner is Player && chunk.owner.graphicsModule != null)
            {
                return (chunk.owner.graphicsModule as PlayerGraphics).drawPositions[chunk.index, 0];
            }
            return chunk.pos;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            for (int i = sLeaser.sprites.Length - 1; i >= 0; i--)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            mainColor = Color.Lerp(originColor, palette.fogColor, 0.2f);
            lightColor = Color.Lerp(lightOriginColor, palette.fogColor, 0.2f);
            sLeaser.sprites[1].color = lightColor;
            sLeaser.sprites[0].color = mainColor;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            foreach (var sprite in sLeaser.sprites)
            {
                sprite.x = Mathf.Lerp(bodyChunks[0].lastPos.x,bodyChunks[0].pos.x,timeStacker) - camPos.x;
                sprite.y = Mathf.Lerp(bodyChunks[0].lastPos.y,bodyChunks[0].pos.y,timeStacker) - camPos.y;
                sprite.scaleY = Mathf.Lerp(0.7f,0.1f, age);
                sprite.rotation = Custom.VecToDeg(Vector2.Lerp(lastNeedleDir,needleDir,timeStacker));
            }

        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[1] = new FSprite("FireBugSpear", true);
            sLeaser.sprites[0] = new FSprite("FireBugSpearColor", true);
            foreach (var sprite in sLeaser.sprites)
                sprite.scale = 0.7f;
            
            AddToContainer(sLeaser,rCam, null);
        }

        float sizeFac;
        float lifeTime;

        float age;

        Vector2 needleDir;
        Vector2 lastNeedleDir;

        Vector2 stuckPos;
        Vector2 stuckDir;

        Creature shotBy;
        BodyChunk stuckInChunk;
        DartMaggot.Mode mode;

        int blindCounter;

        Color lightColor;
        Color mainColor;

        static readonly Color originColor = Custom.hexToColor("00A2E8");
        static readonly Color lightOriginColor = Custom.hexToColor("ADDAEA");

    }
}
