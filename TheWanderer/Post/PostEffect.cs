﻿using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pkuyo.Wanderer.Post
{
    public class PostData
    {
        public PostData(PlayerGraphics self)
        {
            graphicsRef = new WeakReference<PlayerGraphics>(self);
        }

        public bool IsVaild
        {
            get => graphicsRef.TryGetTarget(out var a) && a !=null;
        }

        public void NewRoom()
        {
            roomLerpCount = 80;
        }
        public void DrawSprite(RoomCamera.SpriteLeaser leaser)
        {
            if (roomLerpCount == -1)
                center = oldRoomPos = leaser.sprites[3].GetPosition() / (Custom.rainWorld.screenSize);
            else
            {
                center = Vector2.Lerp((leaser.sprites[3].GetPosition() / (Custom.rainWorld.screenSize)), oldRoomPos, Mathf.Pow(Mathf.InverseLerp(0, 80, roomLerpCount), 3));
                roomLerpCount--;
            }
        }
        WeakReference<PlayerGraphics> graphicsRef;
        public Vector2 center;
        Vector2 oldRoomPos;
        int roomLerpCount = -1;
    }
    public class PostEffect : MonoBehaviour
    {

        private readonly float blurFactor = 0.006f;
        private readonly float lerpFactor = 0.8f;
        private readonly float spit = 0.05f;
        private readonly float scale = 0.025f;
        private readonly int downSampleFactor = 1;

        Vector2? center;


        public int LoungeCounter
        {
            get => loungeCounter;
            set
            {
                //为了--
                if (loungeCounter < value+2 && loungeMat)
                {
                    loungeCounter = value;
                    loungeTimer = Mathf.Pow(Mathf.InverseLerp(0, 15, loungeCounter), 0.7f);
                    loungeMat.SetFloat("_LerpFactor", Mathf.Lerp(0, lerpFactor, loungeTimer));
                    loungeMat.SetFloat("_Spit", Mathf.Lerp(0, spit, loungeTimer));
                    loungeMat.SetFloat("_Scale", Mathf.Lerp(0, scale, loungeTimer));
                    loungeMat.SetFloat("_BlurFactor", Mathf.Lerp(0, blurFactor, loungeTimer));
                }
            }
        }


        int loungeCounter = 0;
        

        float loungeTimer = 0;

        Material loungeMat;
            
        bool IsSupported = true;

        float timeStacker = 0.0f;

        public void Start()
        {
            try
            {
                loungeMat = new Material(WandererAssetManager.Instance().PostShaders["LoungePost"]);
                
                loungeMat.SetFloat("_VRadius", 1.2f);
            }
            catch (Exception e)
            {

                Debug.LogError("Sorry. Shader not support");
                IsSupported = false;
            }
        }

        protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            timeStacker += Time.deltaTime;
            if (timeStacker > 1 / 40f)
            {
                if (LoungeCounter != 0)
                    LoungeCounter--;
                timeStacker -= 1/40f;
            }

            if (IsSupported && LoungeCounter != 0 && loungeMat)
            {
                RenderTexture rt1 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
                RenderTexture rt2 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
                Graphics.Blit(source, rt1);


                List<Vector4> centers = new List<Vector4>();
                for (int i = 0; i < WandererAssetManager.Instance().PlayerPos.Count; i++)
                {
                    if(i==0)
                    {
                        if (!center.HasValue) center = WandererAssetManager.Instance().PlayerPos[0];
                        else
                            center = Vector2.Lerp(center.Value, WandererAssetManager.Instance().PlayerPos[0], 0.01f);
                        centers.Add(new Vector4(center.Value.x, center.Value.y));
                    }
                    else
                        centers.Add(new Vector4(WandererAssetManager.Instance().PlayerPos[i].x, WandererAssetManager.Instance().PlayerPos[i].y));
                }
                if(centers.Count!=0)
                    loungeMat.SetVectorArray("_BlurCenter", centers);

                loungeMat.SetFloat("_BlurCenterLength", centers.Count);

                Graphics.Blit(rt1, rt2, loungeMat, 0);
                loungeMat.SetTexture("_BlurTex", rt2);
                Graphics.Blit(source, destination, loungeMat, 1);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }
            else
                Graphics.Blit(source, destination);



        }

    }
}
