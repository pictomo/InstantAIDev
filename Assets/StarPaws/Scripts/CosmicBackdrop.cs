using UnityEngine;
namespace StarPaws
{
    public sealed class CosmicBackdrop
    {
        readonly Transform root, saturn, smallPlanet;
        readonly LineRenderer orbit;
        public CosmicBackdrop(Transform parent, CosmicArt art)
        {
            root=new GameObject("A tiny corner of the cosmos").transform;root.SetParent(parent,false);
            var random=new System.Random(542);
            var stars=new CombineInstance[520];
            // Combine all distant stars into one mesh to keep the endless sky cheap.
            var quad=new Mesh();quad.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,2,1,0,3,2};quad.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            for(int i=0;i<stars.Length;i++)
            {
                var p=new Vector3((float)random.NextDouble()*220-110,(float)random.NextDouble()*100-25,(float)random.NextDouble()*100+65);
                float s=i%13==0?0.32f:0.07f+(float)random.NextDouble()*0.14f;
                stars[i]=new CombineInstance {mesh=quad,transform=Matrix4x4.TRS(p,Quaternion.identity,new Vector3(s,s,1))};
            }
            var mesh=new Mesh {name="520 procedural stars"};mesh.CombineMeshes(stars);Object.Destroy(quad);
            var starField=art.MeshObject("Star field",mesh,root,art.Glow);starField.AddComponent<OwnedMesh>().mesh=mesh;
            saturn=new GameObject("Lavender Saturn").transform;saturn.SetParent(root,false);saturn.localPosition=new Vector3(-1,12,55);saturn.localRotation=Quaternion.Euler(-18,0,22);
            art.Shape("Lavender planet",PrimitiveType.Sphere,saturn,Vector3.zero,Vector3.one*12,art.Mat(new Color(0.30f,0.20f,0.53f),0.12f));
            for(int i=0;i<5;i++) art.Ring("Saturn ring",saturn,8.1f+i*0.34f,i%2==0?art.LilacGlow:art.PinkGlow,i==2?0.24f:0.065f,128);
            var belt=art.Ring("Orbital trace",saturn,13.8f,art.Mat(new Color(0.24f,0.18f,0.40f),0.25f),0.028f,128);belt.transform.localRotation=Quaternion.Euler(8,12,0);
            smallPlanet=art.Shape("Mint moon",PrimitiveType.Sphere,root,new Vector3(-26,9,47),Vector3.one*4.8f,art.Mat(new Color(0.10f,0.35f,0.38f),0.16f)).transform;
            art.Ring("Moon orbit",smallPlanet,0.8f,art.MintGlow,0.007f).transform.localRotation=Quaternion.Euler(25,0,18);
            var far=art.Shape("Distant peach moon",PrimitiveType.Sphere,root,new Vector3(-13,24,100),Vector3.one*3,art.Mat(new Color(0.75f,0.35f,0.44f),0.3f));
            orbit=art.Ring("Wayfinder halo",root,12,art.Mat(new Color(0.17f,0.25f,0.39f),0.6f),0.045f,128);orbit.transform.localPosition=new Vector3(0,6,135);orbit.transform.localRotation=Quaternion.Euler(90,0,0);
            var points=new[]{new Vector3(-36,23,85),new Vector3(-28,27,86),new Vector3(-23,23,87),new Vector3(-19,30,88),new Vector3(-13,28,89)};
            art.Line("Sleeping cat constellation",root,points,art.Mat(new Color(0.18f,0.25f,0.42f),0.35f),0.035f);
            foreach(var p in points)art.Shape("Constellation star",PrimitiveType.Sphere,root,p,Vector3.one*0.3f,art.MintGlow);
            for(int i=0;i<25;i++)
            {
                float side=i%2==0?-1:1;var p=new Vector3(side*(12+(float)random.NextDouble()*32),-5-(float)random.NextDouble()*16,15+(float)random.NextDouble()*95);
                var rock=art.Shape("Drifting amethyst",PrimitiveType.Cube,root,p,Vector3.one*(0.3f+(float)random.NextDouble()*1.3f),i%4==0?art.LilacGlow:art.Purple);rock.transform.localRotation=Random.rotation;
            }
        }
        public void Tick(float z,float time) { root.position=new Vector3(0,0,z*0.98f);saturn.Rotate(0,Time.deltaTime*0.8f,0);smallPlanet.Rotate(0,Time.deltaTime*2,0); }
    }
    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy() { if(mesh)Destroy(mesh); }
    }
}
