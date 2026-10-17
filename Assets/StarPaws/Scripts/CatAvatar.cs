using UnityEngine;
namespace StarPaws
{
    public sealed class CatAvatar
    {
        public readonly Transform Root;
        readonly Transform model, head, tail;
        readonly Transform[] paws = new Transform[4];
        readonly TrailRenderer[] ribbons = new TrailRenderer[7];
        readonly CosmicArt art;
        float faceAngle = 180;
        public CatAvatar(Transform parent, CosmicArt art)
        {
            this.art=art;
            Root=new GameObject("Mochi • the star kitten").transform;Root.SetParent(parent,false);
            model=new GameObject("Squash and stretch").transform;model.SetParent(Root,false);
            art.Shape("Marshmallow body",PrimitiveType.Sphere,model,new Vector3(0,0.66f,0),new Vector3(0.9f,0.94f,1.18f),art.Cream);
            art.Shape("Lilac space harness",PrimitiveType.Sphere,model,new Vector3(0,0.8f,-0.12f),new Vector3(0.94f,0.63f,0.66f),art.LilacGlow);
            var badge=art.MeshObject("Star on backpack",art.StarMesh,model,art.Gold);badge.transform.localPosition=new Vector3(0,1.12f,-0.2f);badge.transform.localScale=Vector3.one*0.38f;badge.transform.localRotation=Quaternion.Euler(65,0,0);
            head=new GameObject("Curious kitten head").transform;head.SetParent(model,false);head.localPosition=new Vector3(0,1.21f,0.35f);
            art.Shape("Big round head",PrimitiveType.Sphere,head,Vector3.zero,new Vector3(1.16f,0.98f,0.91f),art.Cream);
            var earMesh=art.EarMesh();
            foreach(float side in new[]{-1f,1f})
            {
                var ear=art.MeshObject("Pointy ear",earMesh,head,art.Cream);ear.transform.localPosition=new Vector3(side*0.36f,0.3f,0);ear.transform.localRotation=Quaternion.Euler(0,180,-side*13);
                var inner=art.MeshObject("Pink inner ear",earMesh,head,art.Blush);inner.transform.localPosition=new Vector3(side*0.36f,0.37f,0.11f);inner.transform.localScale=new Vector3(0.57f,0.64f,0.65f);inner.transform.localRotation=ear.transform.localRotation;
                art.Shape("Velvet eye",PrimitiveType.Sphere,head,new Vector3(side*0.245f,0.02f,0.402f),new Vector3(0.21f,0.27f,0.095f),art.Ink);
                art.Shape("Mint iris",PrimitiveType.Sphere,head,new Vector3(side*0.245f,0.005f,0.45f),new Vector3(0.105f,0.15f,0.035f),art.MintGlow);
                art.Shape("Eye sparkle",PrimitiveType.Sphere,head,new Vector3(side*0.245f-0.028f,0.068f,0.466f),new Vector3(0.058f,0.073f,0.025f),art.White);
                art.Shape("Rosy cheek",PrimitiveType.Sphere,head,new Vector3(side*0.38f,-0.15f,0.37f),new Vector3(0.18f,0.085f,0.035f),art.Blush);
                art.Line("Little whiskers",head,new[]{new Vector3(side*0.35f,-0.06f,0.43f),new Vector3(side*0.63f,-0.01f,0.37f)},art.Cream,0.018f);
                art.Line("Smile",head,new[]{new Vector3(0,-0.15f,0.462f),new Vector3(side*0.065f,-0.20f,0.455f),new Vector3(side*0.13f,-0.16f,0.443f)},art.Ink,0.018f);
            }
            art.Shape("Pink button nose",PrimitiveType.Sphere,head,new Vector3(0,-0.09f,0.456f),new Vector3(0.12f,0.075f,0.06f),art.Blush);
            for(int i=0;i<4;i++) paws[i]=art.Shape("Soft little paw",PrimitiveType.Sphere,model,new Vector3(i%2==0?-0.31f:0.31f,0.23f,i<2?0.38f:-0.38f),new Vector3(0.32f,0.38f,0.43f),art.Cream).transform;
            tail=new GameObject("Curled tail").transform;tail.SetParent(model,false);tail.localPosition=new Vector3(0,0.68f,-0.45f);
            art.Line("Tail",tail,new[]{Vector3.zero,new Vector3(0,0.1f,-0.3f),new Vector3(0.08f,0.48f,-0.52f),new Vector3(0.19f,0.76f,-0.44f),new Vector3(0.30f,0.82f,-0.25f)},art.Cream,0.19f);
            art.Shape("Pink tail tip",PrimitiveType.Sphere,tail,new Vector3(0.30f,0.82f,-0.25f),Vector3.one*0.2f,art.Blush);
            for(int i=0;i<7;i++)
            {
                var o=new GameObject("Rainbow ribbon "+i);o.transform.SetParent(Root,false);o.transform.localPosition=new Vector3((i-3)*0.105f,0.52f,-0.55f);
                var tr=o.AddComponent<TrailRenderer>();ribbons[i]=tr;tr.sharedMaterial=art.Trail;tr.time=0;tr.minVertexDistance=0.08f;
                tr.widthCurve=new AnimationCurve(new Keyframe(0,0.12f),new Keyframe(0.6f,0.11f),new Keyframe(1,0));
                Color c=Color.HSVToRGB(i/7f,0.65f,1);tr.colorGradient=new Gradient {colorKeys=new[]{new GradientColorKey(c,0),new GradientColorKey(c,1)},alphaKeys=new[]{new GradientAlphaKey(0.88f,0),new GradientAlphaKey(0.8f,0.55f),new GradientAlphaKey(0,1)}};
                tr.numCapVertices=3;tr.emitting=false;
            }
        }
        public void Animate(float time,float direction,bool running,bool airborne,float verticalSpeed)
        {
            faceAngle=Mathf.LerpAngle(faceAngle,running?direction*12:160,Time.deltaTime*7);
            model.localRotation=Quaternion.Euler(airborne?Mathf.Clamp(-verticalSpeed*1.3f,-13,16):0,faceAngle,-direction*8);
            float bob=running&&!airborne?Mathf.Abs(Mathf.Sin(time*15))*0.065f:Mathf.Sin(time*2.5f)*0.045f;
            model.localPosition=new Vector3(0,bob,0);model.localScale=new Vector3(1,airborne?1.05f:1,1);
            for(int i=0;i<4;i++) { var p=paws[i].localPosition;p.y=0.23f+(running?Mathf.Sin(time*15+(i==0||i==3?0:Mathf.PI))*0.15f:0);paws[i].localPosition=p; }
            tail.localRotation=Quaternion.Euler(0,Mathf.Sin(time*4)*14,Mathf.Sin(time*3)*8);
            head.localRotation=Quaternion.Euler(0,running?Mathf.Sin(time*1.2f)*14:Mathf.Sin(time)*9,Mathf.Sin(time*1.8f)*4);
        }
        public void SetStars(int count) { foreach(var r in ribbons) { r.time=count==0?0:Mathf.Min(3.6f,0.55f+count*0.16f);r.emitting=count>0; } }
        public void Reset() { faceAngle=160;Root.position=Vector3.zero;foreach(var r in ribbons) {r.Clear();r.emitting=false;r.time=0;} }
    }
}
