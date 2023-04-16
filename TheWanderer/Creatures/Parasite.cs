using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Properties;
using Fisobs.Sandbox;
using MoreSlugcats;
using DevInterface;

namespace Pkuyo.Wanderer.Creatures
{


    class Parasite : InsectoidCreature
    {
        public Parasite(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            float rad = 5;
            float mass = 0.5f;
            float connectLength = 15;
            if (abstractCreature.creatureTemplate.type == WandererEnum.Creatures.FemaleParasite)
            {
                kindSpeed = 0.7f;
                rad = 6;
                mass = 0.7f;
                isFemale = true;
            }
            else if (abstractCreature.creatureTemplate.type == WandererEnum.Creatures.MaleParasite)
            {
                kindSpeed = 1.0f;
                isMale = true;
            }
            else if (abstractCreature.creatureTemplate.type == WandererEnum.Creatures.ChildParasite)
            {
                isChild = true;
                rad = 3;
                mass = 0.25f;
                connectLength = 10;
                kindSpeed = 0.6f;
            }
            bodyChunks = new BodyChunk[2];
            bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, rad, mass);
            bodyChunks[1] = new BodyChunk(this, 0, Vector2.zero, rad, mass);
            airFriction = 0.999f;
            bounce = 0.1f;
            surfaceFriction = 0.4f;
            collisionLayer = 1;
            gravity = 0.9f;
            waterFriction = 0.96f;
            buoyancy = 0.95f;
            bodyChunkConnections = new BodyChunkConnection[1];
            bodyChunkConnections[0] = new BodyChunkConnection(bodyChunks[0], bodyChunks[1], connectLength, BodyChunkConnection.Type.Normal, 1f, 0.5f);

          
        }


        public override void Update(bool eu)
        {
            if (room == null)
            {
                return;
            }

            if (room.game.devToolsActive && Input.GetKey("b") && room.game.cameras[0].room == room)
            {
                bodyChunks[0].vel += Custom.DirVec(bodyChunks[0].pos, new Vector2(Futile.mousePosition.x, Futile.mousePosition.y) + room.game.cameras[0].pos) * 14f;
                Stun(12);
            }
            if (!dead)
            {
                if (State.health < 0f && Random.value < -State.health && Random.value < 1f / (Consious ? 80 : 800))
                    Die();

                if (Random.value < 0.033333335f && (Random.value * 0.2f > State.health || Random.value < -State.health))
                    Stun(Random.Range(1, Random.Range(1, 27 - Custom.IntClamp((int)(20f * State.health), 0, 10))));

                //尸体抽搐
                if (!Consious && Random.value < 0.05f)
                    bodyChunks[1].vel += Custom.RNV() * Random.value * 3f;
            }
            base.Update(eu);
            mainBodyChunk.vel += Custom.DirVec(bodyChunks[1].pos, mainBodyChunk.pos) * 0.5f;
            sitting = false;

            if (Consious)
            {
                if (room.aimap.TileAccessibleToCreature(bodyChunks[0].pos, Template) || room.aimap.TileAccessibleToCreature(bodyChunks[1].pos, Template))
                    footingCounter++;

                var cost = room.aimap.TileCostForCreature(abstractCreature.pos, Template);
                if(cost.legality<=PathCost.Legality.Unwanted)
                    diffSpeed = Custom.LerpAndTick(diffSpeed, 1f / Mathf.Pow(cost.resistance, 0.5f), 0.01f, 0.1f);

                Act();
            }
            else
            {
                footingCounter = 0;
            }

            if (Footing)
            {
                for (int k = 0; k < 2; k++)
                {
                    bodyChunks[k].vel *= 0.8f;
                    BodyChunk bodyChunk = bodyChunks[k];
                    bodyChunk.vel.y = bodyChunk.vel.y +gravity;
                }
                if (MoveBackwards)
                {
                    BodyChunk bodyChunk2 = bodyChunks[1];
                    bodyChunk2.vel.y = bodyChunk2.vel.y + gravity;
                }
                else
                {
                    BodyChunk bodyChunk3 = bodyChunks[1];
                    bodyChunk3.vel.y = bodyChunk3.vel.y + gravity * Mathf.Lerp(0.5f, 1f, AI.stuckTracker.Utility());
                }
            }
            travelDir *= (sitting ? 0.5f : 0.9995f);
            if (grasps[0] != null)
            {
                CarryObject(eu);
                return;
            }
        }
        public void InitiateJump(BodyChunk target)
        {
            if (charging > 0f || jumping || grasps[0] != null)
            {
                return;
            }
            charging = 0.01f;
            jumpAtChunk = target;
            room.PlaySound(SoundID.Drop_Bug_Prepare_Jump, mainBodyChunk);
        }

        private void Attack()
        {
            if (grasps[0] != null || jumpAtChunk == null || (jumpAtChunk != null && (jumpAtChunk.owner.room != room || !room.VisualContact(mainBodyChunk.pos, jumpAtChunk.pos))))
            {
                charging = 0f;
                jumpAtChunk = null;
                return;
            }
            Vector2? vector = null;
            if (jumpAtChunk != null)
                vector = new Vector2?(jumpAtChunk.pos);
            
            Vector2 p = new Vector2(vector.Value.x, vector.Value.y);
            if (!room.GetTile(vector.Value + new Vector2(0f, 20f)).Solid)
            {
                Vector2? vector2 = vector;
                Vector2 b = new Vector2(0f, Mathf.InverseLerp(40f, 200f, Vector2.Distance(mainBodyChunk.pos, vector.Value)) * 20f);
                vector = vector2 + b;
            }
            Vector2 vector3 = Custom.DirVec(mainBodyChunk.pos, vector.Value);
            if (!Custom.DistLess(mainBodyChunk.pos, p, Custom.LerpMap(Vector2.Dot(vector3, Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos)), -0.1f, 0.8f, 0f, 300f, 0.4f)))
            {
                charging = 0f;
                jumpAtChunk = null;
                return;
            }
            if (!room.GetTile(mainBodyChunk.pos + new Vector2(0f, 20f)).Solid && !room.GetTile(bodyChunks[1].pos + new Vector2(0f, 20f)).Solid)
            {
                vector3 = Vector3.Slerp(vector3, new Vector2(0f, 1f), Custom.LerpMap(Vector2.Distance(mainBodyChunk.pos, vector.Value), 40f, 200f, 0.05f, 0.2f));
            }
            room.PlaySound(SoundID.Drop_Bug_Jump, mainBodyChunk);
            Jump(vector3);
        }

        private void Jump(Vector2 jumpDir)
        {
            float d = Custom.LerpMap(jumpDir.y, -1f, 1f, 0.7f, 1.2f, 1.1f);
            footingCounter = 0;
            mainBodyChunk.vel *= 0.5f;
            bodyChunks[1].vel *= 0.5f;
            mainBodyChunk.vel += jumpDir * 21f * d;
            bodyChunks[1].vel += jumpDir * 16f * d;
            attemptBite = 1f;
            charging = 0f;
            jumping = true;
        }

        public void CarryObject(bool eu)
        {
            if (!safariControlled && Random.value < 0.025f && (!(grasps[0].grabbed is Creature) || 
                (AI.DynamicRelationship((grasps[0].grabbed as Creature).abstractCreature).type != CreatureTemplate.Relationship.Type.Eats) &&
                AI.DynamicRelationship((grasps[0].grabbed as Creature).abstractCreature).type != CreatureTemplate.Relationship.Type.Attacks))
            {
                Debug.Log("Parasite lose grasp, not except creature");
                LoseAllGrasps();
                return;
            }
            PhysicalObject grabbed = grasps[0].grabbed;
            carryObjectMass = grabbed.bodyChunks[grasps[0].chunkGrabbed].owner.TotalMass;
            if (carryObjectMass <= TotalMass * 1.4f)
            {
                carryObjectMass /= 2f;
            }
            else if (carryObjectMass <= TotalMass / 5f)
            {
                carryObjectMass = 0f;
            }
            float num = mainBodyChunk.rad + grasps[0].grabbed.bodyChunks[grasps[0].chunkGrabbed].rad;
            Vector2 a = -Custom.DirVec(mainBodyChunk.pos, grabbed.bodyChunks[grasps[0].chunkGrabbed].pos) * (num - Vector2.Distance(mainBodyChunk.pos, grabbed.bodyChunks[grasps[0].chunkGrabbed].pos));
            float num2 = grabbed.bodyChunks[grasps[0].chunkGrabbed].mass / (mainBodyChunk.mass + grabbed.bodyChunks[grasps[0].chunkGrabbed].mass);
            num2 *= 0.2f * (1f - AI.stuckTracker.Utility());
            mainBodyChunk.pos += a * num2;
            mainBodyChunk.vel += a * num2;
            grabbed.bodyChunks[grasps[0].chunkGrabbed].pos -= a * (1f - num2);
            grabbed.bodyChunks[grasps[0].chunkGrabbed].vel -= a * (1f - num2);
            Vector2 vector = mainBodyChunk.pos + Custom.DirVec(bodyChunks[1].pos, mainBodyChunk.pos) * num;
            Vector2 vector2 = grabbed.bodyChunks[grasps[0].chunkGrabbed].vel - mainBodyChunk.vel;
            grabbed.bodyChunks[grasps[0].chunkGrabbed].vel = mainBodyChunk.vel;
            if (enteringShortCut == null && (vector2.magnitude * grabbed.bodyChunks[grasps[0].chunkGrabbed].mass > 30f || !Custom.DistLess(vector, grabbed.bodyChunks[grasps[0].chunkGrabbed].pos, 70f + grabbed.bodyChunks[grasps[0].chunkGrabbed].rad)))
            {
                LoseAllGrasps();
            }
            else
            {
                grabbed.bodyChunks[grasps[0].chunkGrabbed].MoveFromOutsideMyUpdate(eu, vector);
            }
            if (grasps[0] != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    grasps[0].grabbed.PushOutOf(bodyChunks[i].pos, bodyChunks[i].rad, grasps[0].chunkGrabbed);
                }
            }
        }

        public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
            Vector2 a = Custom.IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].pos = newRoom.MiddleOfTile(pos) - a * (-1.5f + (float)i) * 15f;
                bodyChunks[i].lastPos = newRoom.MiddleOfTile(pos);
                bodyChunks[i].vel = a * 2f;
            }
            if (graphicsModule != null)
            {
                graphicsModule.Reset();
            }
        }
        public override void InitiateGraphicsModule()
        {
            if (graphicsModule == null)
            {
                graphicsModule = new ParasiteGraphics(this);
            }
            graphicsModule.Reset();
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            if (otherObject is Creature && Consious && !(otherObject as Creature).dead)
            {
                if ((otherObject as Creature) is Parasite)
                {
                    AI.CollideWithKin(otherObject as Parasite);
                    return;
                }
                AI.tracker.SeeCreature((otherObject as Creature).abstractCreature);
                if(isFemale && grasps[0] == null && Vector2.Dot(Custom.DirVec(bodyChunks[1].pos, mainBodyChunk.pos), Custom.DirVec(mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos)) > 0f &&
                    (AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Attacks && AI.behavior == ParasiteAI.Behavior.Hunt))
                {
                    for (int i = 0; i < 4; i++)
                        room.AddObject(new WaterDrip(Vector2.Lerp(mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos, Random.value), Custom.RNV() * Random.value * 14f, false));

                    if (Grab(otherObject, 0, otherChunk, Grasp.Shareability.CanNotShare, 0.5f, false, true))
                    {
                        infectingCounter = 0;
                        Debug.Log("Female Parasite grab");
                        (otherObject as Creature).LoseAllGrasps();
                        room.PlaySound(SoundID.Drop_Bug_Grab_Creature, mainBodyChunk);
                    }
                }
                if(isMale && grasps[0] == null && Vector2.Dot(Custom.DirVec(bodyChunks[1].pos, mainBodyChunk.pos), Custom.DirVec(mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos)) > Mathf.Lerp(0.7f, -0.2f, attemptBite))
                {
                    if (AI.DynamicRelationship((otherObject as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats && AI.behavior == ParasiteAI.Behavior.Hunt)
                    {
                        for (int i = 0; i < 4; i++)
                            room.AddObject(new WaterDrip(Vector2.Lerp(mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos, Random.value), Custom.RNV() * Random.value * 14f, false));

                        if (Grab(otherObject, 0, otherChunk, Grasp.Shareability.CanNotShare, 0.5f, false, true))
                        {
                            eattingCounter = 0;
                            (otherObject as Creature).Violence(mainBodyChunk, new Vector2?(Custom.DirVec(mainBodyChunk.pos, otherObject.bodyChunks[otherChunk].pos) * 4f), otherObject.bodyChunks[otherChunk], null, DamageType.Bite, Mathf.Lerp(0.5f, 1.5f, attemptBite), 0f);
                            Debug.Log("Male Parasite grab");
                            room.PlaySound(SoundID.Drop_Bug_Grab_Creature, mainBodyChunk);
                        }
                    }
                    attemptBite = 0f;
                    
                }
            }
        }

        private void Act()
        {
            AI.Update();

            attemptBite = Mathf.Max(0f, attemptBite - 0.025f);

            if (AI.behavior == ParasiteAI.Behavior.Infect)
            {
                infectingCounter++;
                if (infectingCounter == 40)
                {
                    infectingCounter = 0;
                    if (grasps[0] != null && grasps[0].grabbed is Creature)
                    {
                        Creature creature = grasps[0].grabbed as Creature;

                        if (ParasiteHook.Instance().parasiteData.TryGetValue(creature, out var data))
                            data.isParasite = true;
                        
                        room.PlaySound(SoundID.Drop_Bug_Grab_Creature, mainBodyChunk);
                        Debug.Log("Parasite infected " + (grasps[0].grabbed as Creature).Template.type);
                    }
                    LoseAllGrasps();
                }
            }
            else
                infectingCounter = 0;

            if (AI.behavior == ParasiteAI.Behavior.Eat)
            {
                eattingCounter++;
                if (grasps[0] != null && grasps[0].grabbed is Creature)
                {
                    Creature creature = grasps[0].grabbed as Creature;

                    if (!creature.dead && eattingCounter %20 == 0)
                        creature.Violence(mainBodyChunk, new Vector2?(Custom.DirVec(mainBodyChunk.pos, grasps[0].grabbedChunk.pos) * 4f), grasps[0].grabbedChunk, null, DamageType.Bite, Random.Range(0.5F,0.85F), 0f);

                    //进行一个抖动的模拟
                    if (eattingCounter % 10 == 0)
                    {
                        Vector2 vector = grasps[0].grabbedChunk.pos * grasps[0].grabbedChunk.mass;
                        float num = grasps[0].grabbedChunk.mass;
                        for (int j = 0; j < grasps[0].grabbed.bodyChunkConnections.Length; j++)
                        {
                            if (grasps[0].grabbed.bodyChunkConnections[j].chunk1 == grasps[0].grabbedChunk)
                            {
                                vector += grasps[0].grabbed.bodyChunkConnections[j].chunk2.pos * grasps[0].grabbed.bodyChunkConnections[j].chunk2.mass;
                                num += grasps[0].grabbed.bodyChunkConnections[j].chunk2.mass;
                            }
                            else if (grasps[0].grabbed.bodyChunkConnections[j].chunk2 == grasps[0].grabbedChunk)
                            {
                                vector += grasps[0].grabbed.bodyChunkConnections[j].chunk1.pos * grasps[0].grabbed.bodyChunkConnections[j].chunk1.mass;
                                num += grasps[0].grabbed.bodyChunkConnections[j].chunk1.mass;
                            }
                        }
                        vector /= num;
                        mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, Random.value)) * 4f;
                        grasps[0].grabbedChunk.vel += Custom.DirVec(vector, mainBodyChunk.pos) * 0.9f / grasps[0].grabbedChunk.mass;
                    }
                    if (Random.value > 0.97f)
                    {
                        room.PlaySound(SoundID.Slugcat_Eat_Meat_B, mainBodyChunk);
                        room.PlaySound(SoundID.Drop_Bug_Grab_Creature, mainBodyChunk, false, 1f, 0.76f);
                    }
                    if (eattingCounter == 120)
                    {
                        eattingCounter=0;
                        room.PlaySound(SoundID.Drop_Bug_Grab_Creature, mainBodyChunk);
                        Debug.Log("Parasite eaten " + (grasps[0].grabbed as Creature).Template.type);
                        LoseAllGrasps();
                    }
                    
                }
            }

            if (Submersion > 0.3f)
            {
                Die();
                //TODO : 跟细节的死法
                return;
            }

            if (jumping)
            {
                bool flag = false;
                for (int j = 0; j < bodyChunks.Length; j++)
                    if ((bodyChunks[j].ContactPoint.x != 0 || bodyChunks[j].ContactPoint.y != 0) && room.aimap.TileAccessibleToCreature(bodyChunks[j].pos, Template))
                        flag = true;

                if (flag)
                    footingCounter++;
                else
                    footingCounter = 0;
                
                if (jumpAtChunk != null && room.VisualContact(mainBodyChunk.pos, jumpAtChunk.pos))
                {
                    bodyChunks[0].vel += Custom.DirVec(bodyChunks[0].pos, jumpAtChunk.pos) * 1.0f;
                    bodyChunks[1].vel -= Custom.DirVec(bodyChunks[0].pos, jumpAtChunk.pos) * 0.4f;
  
                }

                if (Footing)
                {
                    jumping = false;
                    jumpAtChunk = null;
                }
                return;
            }
            if (AI.stuckTracker.Utility() > 0.9f)
            {
                stuckShake = Custom.LerpAndTick(stuckShake, 1f, 0.07f, 0.014285714f);
            }
            else if (AI.stuckTracker.Utility() < 0.2f)
            {
                stuckShake = Custom.LerpAndTick(stuckShake, 0f, 0.07f, 0.05f);
            }
            //尝试不被卡住
            if (stuckShake > 0f)
            {
                for (int k = 0; k <bodyChunks.Length; k++)
                {
                    bodyChunks[k].vel += Custom.RNV() * Random.value * 5f * stuckShake;
                    bodyChunks[k].pos += Custom.RNV() * Random.value * 5f * stuckShake;
                }
            }
            //?什么特殊运动
            if (specialMoveCounter > 0)
            {
                specialMoveCounter--;
                MoveTowards(room.MiddleOfTile(specialMoveDestination));
                travelDir = Vector2.Lerp(travelDir, Custom.DirVec(mainBodyChunk.pos, room.MiddleOfTile(specialMoveDestination)), 0.4f);
                if (Custom.DistLess(mainBodyChunk.pos, room.MiddleOfTile(specialMoveDestination), 5f))
                    specialMoveCounter = 0;
            }
            else
            {
                if (!room.aimap.TileAccessibleToCreature(mainBodyChunk.pos, Template) && !room.aimap.TileAccessibleToCreature(bodyChunks[1].pos, Template))
                {
                    footingCounter = Custom.IntClamp(footingCounter - 3, 0, 35);
                }
                if (Footing && charging > 0f)
                {
                    sitting = true;
                    GoThroughFloors = false;
                    charging += 0.06666667f;
                    Vector2? vector = null;
                    if (jumpAtChunk != null)
                        vector = new Vector2?(Custom.DirVec(mainBodyChunk.pos, jumpAtChunk.pos));
                    
                    if (vector != null)
                    {
                        bodyChunks[0].vel += vector.Value * Mathf.Pow(charging, 2f);
                        bodyChunks[1].vel -= vector.Value * 4f * charging;
                    }
                    if (charging >= 1f)
                        Attack();
                    
                }
                else if ((room.GetWorldCoordinate(mainBodyChunk.pos) == AI.pathFinder.GetDestination || room.GetWorldCoordinate(bodyChunks[1].pos) == AI.pathFinder.GetDestination) 
                    && AI.threatTracker.Utility() < 0.5f)
                {
                    sitting = true;
                    GoThroughFloors = false;
                }
                else
                {
                    MovementConnection movementConnection = (AI.pathFinder as StandardPather).FollowPath(room.GetWorldCoordinate(bodyChunks[(!this.MoveBackwards) ? 0 : 1].pos), true);
                    
                    if (movementConnection == null)
                        movementConnection = (AI.pathFinder as StandardPather).FollowPath(room.GetWorldCoordinate(bodyChunks[(!MoveBackwards) ? 1 : 0].pos), true);
                  
                    if (movementConnection != null)
                        Run(movementConnection);
                    else
                    {
                        GoThroughFloors = false;
                    }
                    
                }
                //行走声音
            }
        }

   

        private void Run(MovementConnection followingConnection)
        {
            //到了 清除路径点
            if (followingConnection.type == MovementConnection.MovementType.ReachUp)
                (AI.pathFinder as StandardPather).pastConnections.Clear();

            //进入通道
            if (followingConnection.type == MovementConnection.MovementType.ShortCut ||
                followingConnection.type == MovementConnection.MovementType.NPCTransportation)
            {
                enteringShortCut = new IntVector2?(followingConnection.StartTile);
                if (followingConnection.type == MovementConnection.MovementType.NPCTransportation)
                    NPCTransportationDestination = followingConnection.destinationCoord;
            }
            else if (followingConnection.type == MovementConnection.MovementType.OpenDiagonal ||
                followingConnection.type == MovementConnection.MovementType.ReachOverGap ||
                followingConnection.type == MovementConnection.MovementType.ReachUp ||
                followingConnection.type == MovementConnection.MovementType.ReachDown ||
                followingConnection.type == MovementConnection.MovementType.SemiDiagonalReach)
            {
                //?
                specialMoveCounter = 30;
                specialMoveDestination = followingConnection.DestTile;
            }
            else
            {
                MovementConnection movementConnection = followingConnection;

                //没被卡住的话
                if (AI.stuckTracker.Utility() < 0.2f)
                {
                    MovementConnection tendMovement = (AI.pathFinder as StandardPather).FollowPath(movementConnection.destinationCoord, false);
                    if (tendMovement != null)
                    {
                        if (tendMovement.destinationCoord == followingConnection.startCoord)
                        {
                            sitting = true;
                            return;
                        }
                        if (tendMovement.destinationCoord.TileDefined && room.aimap.getAItile(tendMovement.DestTile).acc < AItile.Accessibility.Ceiling)
                        {
                            bool pathUnreached = false;
                            int num = Math.Min(followingConnection.StartTile.x, tendMovement.DestTile.x);
                            while (num < Math.Max(followingConnection.StartTile.x, tendMovement.DestTile.x) && !pathUnreached)
                            {
                                for (int j = Math.Min(followingConnection.StartTile.y, tendMovement.DestTile.y); j < Math.Max(followingConnection.StartTile.y, tendMovement.DestTile.y); j++)
                                {
                                    if (!room.aimap.TileAccessibleToCreature(num, j, Template))
                                    {
                                        pathUnreached = true;
                                        break;
                                    }
                                }
                                num++;
                            }
                            if (!pathUnreached)
                            {
                                movementConnection = tendMovement;
                            }
                        }
                    }
                }
                Vector2 toPos = room.MiddleOfTile(movementConnection.DestTile);
                travelDir = Vector2.Lerp(travelDir, Custom.DirVec(bodyChunks[(!MoveBackwards) ? 0 : 1].pos, toPos), 0.4f);
                if (lastFollowedConnection != null && lastFollowedConnection.type == MovementConnection.MovementType.ReachUp)
                {
                    bodyChunks[(!MoveBackwards) ? 0 : 1].vel += Custom.DirVec(bodyChunks[(!MoveBackwards) ? 0 : 1].pos, toPos) * 4f * CurrentSpeed;
                }

                //转方向
                if (lastFollowedConnection != null && (Footing || room.aimap.TileAccessibleToCreature(bodyChunks[(!MoveBackwards) ? 0 : 2].pos, Template)) &&
                    ((followingConnection.startCoord.x != followingConnection.destinationCoord.x && lastFollowedConnection.startCoord.x == lastFollowedConnection.destinationCoord.x) ||
                     (followingConnection.startCoord.y != followingConnection.destinationCoord.y && lastFollowedConnection.startCoord.y == lastFollowedConnection.destinationCoord.y)))
                {
                    bodyChunks[(!MoveBackwards) ? 0 : 1].vel *= 0.7f;
                    bodyChunks[(!MoveBackwards) ? 1 : 0].vel *= 0.5f;
                }
                MoveTowards(toPos);

            }
            lastFollowedConnection = followingConnection;
        }

        private void MoveTowards(Vector2 moveTo)
        {
            Vector2 vector = Custom.DirVec(mainBodyChunk.pos, moveTo) *0.7f;
            if (!Footing)
            {
                vector *= 0.3f;
            }
            if (!MoveBackwards && IsTileSolid(1, 0, -1) && ((vector.x < -0.5f && mainBodyChunk.pos.x > bodyChunks[1].pos.x + 5f) || (vector.x > 0.5f && mainBodyChunk.pos.x < bodyChunks[1].pos.x - 5f)))
            {
                mainBodyChunk.vel.x -= ((vector.x < 0f) ? -1f : 1f) * 1.3f * CurrentSpeed;
                bodyChunks[1].vel.x = ((vector.x < 0f) ? -1f : 1f) * 0.5f * CurrentSpeed;
                if (!IsTileSolid(0, 0, 1))
                    mainBodyChunk.vel.y += 3.2f;

            }
            float d = 1 * Mathf.Lerp(1f, 1.5f, stuckShake) * CurrentSpeed;
            if (MoveBackwards)
            {
                bodyChunks[1].vel += vector * 7.5f * d;
                mainBodyChunk.vel -= vector * 0.45f * d;
                GoThroughFloors = (moveTo.y < bodyChunks[1].pos.y - 5f);
                return;
            }
            mainBodyChunk.vel += vector * 4.5f * d;
            bodyChunks[1].vel -= vector * 0.2f * d;
            GoThroughFloors = (moveTo.y < mainBodyChunk.pos.y - 5f);
        }

        public override void Stun(int st)
        {
            base.Stun(st);
            infectingCounter = 0;
        }
        public bool Footing
        {
            get => footingCounter >= 10;
        }

        public new HealthState State
        {
            get
            {
                return abstractCreature.state as HealthState;
            }
        }

        int specialMoveCounter;
        IntVector2 specialMoveDestination;
        public Vector2 travelDir;
        MovementConnection lastFollowedConnection;

        int footingCounter = 0;

        float stuckShake;

        float carryObjectMass;

        int infectingCounter = 0;

        int eattingCounter = 0;

        public float attemptBite = 0f;
        public float charging = 0f;
        public bool jumping = false;

        BodyChunk jumpAtChunk;

        public bool isMale;
        public bool isFemale;
        public bool isChild;
       

        public ParasiteAI AI;

        public bool sitting;
        public bool MoveBackwards = false;
        
        public float runSpeed = 1f;
        public float diffSpeed = 1f;

        public float kindSpeed = 1f;
        

        public float CurrentSpeed
        {
            get => runSpeed * diffSpeed * kindSpeed;
        }

    }
    sealed class ParasiteCritob : Critob
    {
        public ParasiteCritob(CreatureTemplate.Type type) : base(type)
        {
         
            CreatureName = type.value;
            LoadedPerformanceCost = 30f;
            SandboxPerformanceCost = new SandboxPerformanceCost(0.5f, 0.5f);
            if (type == WandererEnum.Creatures.FemaleParasite)
            {
                Icon = new SimpleIcon("Kill_DropBug", new Color(1f, 0.9f, 0.9f));
                RegisterUnlock(KillScore.Configurable(5), WandererEnum.Sandbox.FemaleParasite, MultiplayerUnlocks.SandboxUnlockID.BigSpider, 0);
            }
            else if (type == WandererEnum.Creatures.MaleParasite)
            {
                Icon = new SimpleIcon("Kill_DropBug", new Color(0.2f, 0.1f, 0.1f));
                RegisterUnlock(KillScore.Configurable(5), WandererEnum.Sandbox.MaleParasite, MultiplayerUnlocks.SandboxUnlockID.DropBug, 0);
            }
            else
            {
                //Icon = new SimpleIcon("Kill_Spider", new Color(1f, 0.9f, 0.9f));
                RegisterUnlock(KillScore.Configurable(5), WandererEnum.Sandbox.ChildParasite, MultiplayerUnlocks.SandboxUnlockID.Spider, 0);
            }
        }
        public override int ExpeditionScore()
        {
            return 5;
        }

        public override CreatureState CreateState(AbstractCreature acrit)
        {
            return new HealthState(acrit);
        }
        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new ParasiteAI(acrit, acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new Parasite(acrit, acrit.world);
        }

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate t = new CreatureFormula(this)
            {
                DefaultRelationship = new(CreatureTemplate.Relationship.Type.Eats, 0.2f),
                HasAI = true,
                InstantDeathDamage = 2,
                Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.BigSpider),
                TileResistances = new()
                {
                    OffScreen = new(2, PathCost.Legality.Allowed),
                    Floor = new(1, PathCost.Legality.Allowed),
                    Corridor = new(1, PathCost.Legality.Allowed),
                    Climb = new(2f, PathCost.Legality.Allowed),
                    Wall = new(6, PathCost.Legality.Allowed),
                    Ceiling = new(8, PathCost.Legality.Allowed),
                },
                ConnectionResistances = new()
                {
                    Standard = new(1, PathCost.Legality.Allowed),
                    OpenDiagonal = new(3, PathCost.Legality.Allowed),
                    ReachOverGap = new(3, PathCost.Legality.Allowed),
                    ReachUp = new(3, PathCost.Legality.Allowed),
                    SemiDiagonalReach = new(2f, PathCost.Legality.Allowed),
                    DropToFloor = new(10f, PathCost.Legality.Allowed),
                    DropToWater = new(100f, PathCost.Legality.Unwanted),
                    DropToClimb = new(10f, PathCost.Legality.Unwanted),
                    ShortCut = new(1.5f, PathCost.Legality.Allowed),
                    NPCTransportation = new(3f, PathCost.Legality.Allowed),
                    OffScreenMovement = new(1, PathCost.Legality.Allowed),
                    BetweenRooms = new(5f, PathCost.Legality.Allowed),
                    Slope = new(1.5f, PathCost.Legality.Allowed),
                    CeilingSlope = new(1.5f, PathCost.Legality.Allowed),
                },
                DamageResistances = new()
                {
                    Base = 1.1f,
                },
                StunResistances = new()
                {
                    Base = 0.6f,
                }
            }.IntoTemplate();
            t.instantDeathDamageLimit = 1f;
            t.offScreenSpeed = 0.3f;
            t.abstractedLaziness = 50;
            t.AI = true;
            t.requireAImap = true;
            t.bodySize = 1f;
            t.stowFoodInDen = true;
            t.shortcutSegments = 2;
            t.grasps = 1;
            t.visualRadius = 800f;
            t.throughSurfaceVision = 0.8f;
            t.communityInfluence = 0.1f;
            t.dangerousToPlayer = 0.3f;
            t.waterRelationship = CreatureTemplate.WaterRelationship.AirOnly;
            t.canSwim = false;
            t.meatPoints = 3;
            t.interestInOtherAncestorsCatches = 0f;
            t.interestInOtherCreaturesCatches = 2f;

            if(Type == WandererEnum.Creatures.MaleParasite)
            {
                t.dangerousToPlayer = 0.45f;
                t.visualRadius = 1000f;
            }
            else if(Type == WandererEnum.Creatures.ChildParasite)
            {
                t.dangerousToPlayer = 0.1f;
                t.visualRadius = 600f;
                t.bodySize = 0.7f;
                t.baseDamageResistance = 0.7f;
            }
            return t;
        }
        public override string DevtoolsMapName(AbstractCreature acrit)
        {
            if (Type == WandererEnum.Creatures.FemaleParasite)
                return "PAF";
            else if (Type == WandererEnum.Creatures.MaleParasite)
                return "PAM";
            else
                return "PAC";
        }

   
        public override Color DevtoolsMapColor(AbstractCreature acrit)
        {
            return new Color(1f, 0.9f, 0.9f);
        }

        public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction()
        {
            return new RoomAttractivenessPanel.Category[]
            {
                RoomAttractivenessPanel.Category.LikesInside
            };
        }

        public override CreatureTemplate.Type ArenaFallback()
        {
            if (Type == WandererEnum.Creatures.FemaleParasite)
                return CreatureTemplate.Type.BigSpider;
            else if (Type == WandererEnum.Creatures.MaleParasite)
                return CreatureTemplate.Type.DropBug;
            return CreatureTemplate.Type.Spider;

        }

        public override void EstablishRelationships()
        {

            Relationships relationships = new Relationships(Type);
            relationships.Eats(CreatureTemplate.Type.Slugcat, 1.0f);

            relationships.Eats(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, 0.8f);
            relationships.EatenBy(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, 0.3f);

            relationships.EatenBy(CreatureTemplate.Type.LizardTemplate, 0.2f);

            relationships.Fears(CreatureTemplate.Type.RedLizard, 0.8f);
            relationships.EatenBy(CreatureTemplate.Type.RedLizard, 0.3f);

            relationships.Fears(CreatureTemplate.Type.DaddyLongLegs, 1.0f);
            relationships.EatenBy(CreatureTemplate.Type.DaddyLongLegs, 0.5f);

            relationships.Fears(CreatureTemplate.Type.BrotherLongLegs, 1.0f);
            relationships.EatenBy(CreatureTemplate.Type.BrotherLongLegs, 0.5f);

            relationships.Fears(CreatureTemplate.Type.Vulture, 0.5f);
            relationships.EatenBy(CreatureTemplate.Type.Vulture, 0.2f);

            relationships.Fears(CreatureTemplate.Type.KingVulture, 0.6f);
            relationships.EatenBy(CreatureTemplate.Type.KingVulture, 0.2f);

            relationships.Fears(CreatureTemplate.Type.MirosBird, 1.0f);
            relationships.EatenBy(CreatureTemplate.Type.MirosBird, 0.2f);

            relationships.Fears(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 1.0f);
            relationships.EatenBy(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 0.2f);

            relationships.Ignores(CreatureTemplate.Type.DropBug);
            relationships.IgnoredBy(CreatureTemplate.Type.DropBug);

            relationships.Ignores(CreatureTemplate.Type.BigSpider);
            relationships.IgnoredBy(CreatureTemplate.Type.BigSpider);

            relationships.Ignores(CreatureTemplate.Type.Spider);
            relationships.IgnoredBy(CreatureTemplate.Type.Spider);

            relationships.Ignores(CreatureTemplate.Type.SpitterSpider);
            relationships.IgnoredBy(CreatureTemplate.Type.SpitterSpider);

            relationships.Ignores(WandererEnum.Creatures.ToxicSpider);
            relationships.IgnoredBy(WandererEnum.Creatures.ToxicSpider);

            relationships.Ignores(CreatureTemplate.Type.GarbageWorm);
            relationships.IgnoredBy(CreatureTemplate.Type.GarbageWorm);

            relationships.Eats(CreatureTemplate.Type.SmallCentipede,0.4f);
            relationships.FearedBy(CreatureTemplate.Type.SmallCentipede, 0.4f);

            relationships.Eats(CreatureTemplate.Type.LanternMouse, 0.5f);
            relationships.FearedBy(CreatureTemplate.Type.LanternMouse, 0.5f);

            relationships.Eats(CreatureTemplate.Type.Scavenger, 0.6f);
            relationships.FearedBy(CreatureTemplate.Type.Scavenger, 0.3f);

            relationships.Ignores(WandererEnum.Creatures.ChildParasite);
            relationships.Ignores(WandererEnum.Creatures.FemaleParasite);
            relationships.Ignores(WandererEnum.Creatures.MaleParasite);
        }
        public override IEnumerable<string> WorldFileAliases()
        {
            if (Type == WandererEnum.Creatures.FemaleParasite)
            {
                return new string[]
                {
                "Female Parasite",
                "FemaleParasite",
                "female parasite"
                };
            }
            else if (Type == WandererEnum.Creatures.MaleParasite)
            {
                return new string[]
                {
                "Male Parasite",
                "MaleParasite",
                "male parasite"
                };
            }
            else
                return new string[]
                {
                "Child Parasite",
                "ChildParasite",
                "child parasite"
                };
        }

        public override ItemProperties Properties(Creature crit)
        {
            Parasite parasite = crit as Parasite;
            ItemProperties result = null;
            if (parasite != null)
            {
                result = new ParasiteProperties(parasite);
            }
            return result;
        }

        internal sealed class ParasiteProperties : ItemProperties
        {

            public ParasiteProperties(Parasite parasite)
            {
                this.parasite = parasite;
            }

            private readonly Parasite parasite;
        }
    }


}
