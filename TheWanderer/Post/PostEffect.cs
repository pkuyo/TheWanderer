using Pkuyo.Wanderer;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pkuyo.Wanderer.Post
{
    public class PostEffect : MonoBehaviour
    {

        private float blurFactor = 0.006f;
        private float lerpFactor = 0.8f;
        private float spit = 0.05f;
        private float scale = 0.025f;
        private int downSampleFactor = 2;

        public float timeStacker = 0;
        public Vector2 blurCenter = new Vector2(0.5f, 0.5f);
        public void Start()
        {
            try
            {
                LoungeMat = new Material(WandererAssetManager.Instance(null).PostShaders["LoungePost"]);
            }
            catch(Exception e)
            {
                
                Debug.LogError("Sorry. Shader not support");
                IsSupported = false;
            }
        }

        protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (IsSupported && LoungeMat)
            {

                RenderTexture rt1 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
                RenderTexture rt2 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
                Graphics.Blit(source, rt1);

                
                LoungeMat.SetFloat("_BlurFactor", Mathf.Lerp(0,blurFactor * (IsOutSide ? 1 : 1.5f), timeStacker));
                LoungeMat.SetVector("_BlurCenter", blurCenter);
                Graphics.Blit(rt1, rt2, LoungeMat, 0);

                LoungeMat.SetTexture("_BlurTex", rt2);
                LoungeMat.SetFloat("_LerpFactor", Mathf.Lerp(0, lerpFactor* (IsOutSide ? 1 : 1.5f), timeStacker));
                LoungeMat.SetFloat("_Spit", Mathf.Lerp(0, spit, timeStacker));
                LoungeMat.SetFloat("_Scale", Mathf.Lerp(0, scale, timeStacker));
                Graphics.Blit(source, destination, LoungeMat, 1);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }
            else
            {
                Graphics.Blit(source, destination);
            }

        }
        Material LoungeMat;

        public bool IsOutSide = true;

        bool IsSupported = true;
    }
}
