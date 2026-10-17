using UnityEngine;
namespace StarPaws
{
    [RequireComponent(typeof(Camera))]
    public sealed class StarPawsBloom : MonoBehaviour
    {
        public Shader bloomShader;
        Material material;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!bloomShader || !bloomShader.isSupported) { Graphics.Blit(source, destination); return; }
            if (!material) material = new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave };
            int w = Mathf.Max(1, source.width / 3), h = Mathf.Max(1, source.height / 3);
            var a = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.DefaultHDR);
            var b = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.DefaultHDR);
            Graphics.Blit(source, a, material, 0);
            for (int n = 0; n < 2; n++)
            {
                material.SetVector("_Direction", new Vector4(1.5f, 0, 0, 0)); Graphics.Blit(a, b, material, 1);
                material.SetVector("_Direction", new Vector4(0, 1.5f, 0, 0)); Graphics.Blit(b, a, material, 1);
            }
            material.SetTexture("_GlowTex", a); Graphics.Blit(source, destination, material, 2);
            RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(b);
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
