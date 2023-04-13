using RWCustom;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        private readonly float vignetteSoftness = 0.4f;


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

        public int VignetteCounter
        {
            get => vignetteCounter;
            set 
            {
                //为了--
                if(vignetteCounter < value+2 && loungeMat)
                {
                    vignetteCounter = value;
                    loungeMat.SetFloat("_VRadius", Mathf.Lerp(0.2f,1.2f,Mathf.Pow(Mathf.InverseLerp(40,0,vignetteCounter),0.7f)));
                }
            }
        }

        int loungeCounter = 0;
        int vignetteCounter = 0;

        float loungeTimer = 0;

        public Color[] vignetteCenters = new Color[2] { new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(0.5f, 0.5f, 0.5f, 0.5f) };

        Material loungeMat;
            
        bool IsSupported = true;

        float timeStacker = 0.0f;

        public void Start()
        {
            try
            {
                loungeMat = new Material(WandererAssetManager.Instance().PostShaders["LoungePost"]);
                loungeMat.SetFloat("_VSoft", vignetteSoftness);
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
                if (VignetteCounter != 0)
                    VignetteCounter--;

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
                foreach (var i in WandererAssetManager.Instance().postData)
                    if (i.IsVaild)
                        centers.Add(new Vector4(i.center.x,i.center.y));
                loungeMat.SetVectorArray("_BlurCenter", centers);
                loungeMat.SetFloat("_BlurCenterLength", centers.Count);

                Graphics.Blit(rt1, rt2, loungeMat, 0);
                loungeMat.SetTexture("_BlurTex", rt2);
                Graphics.Blit(source, destination, loungeMat, 1);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }
            else if(IsSupported && VignetteCounter != 0 && loungeMat)
            {
                List<Vector4> centers = new List<Vector4>();
                foreach (var i in WandererAssetManager.Instance().postData)
                    if (i.IsVaild)
                        centers.Add(new Vector4(i.center.x, i.center.y));
                loungeMat.SetVectorArray("_BlurCenter", centers);
                loungeMat.SetFloat("_BlurCenterLength", centers.Count); 
         
         
                Graphics.Blit(source, destination, loungeMat, 2);
            }
            else
                Graphics.Blit(source, destination);



        }

    }
}
