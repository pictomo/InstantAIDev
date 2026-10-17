using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarPaws
{
    /// <summary>All shapes, materials and sound in StarPaws are generated locally.</summary>
    public sealed class CosmicArt
    {
        public readonly Color Mint = new Color(0.25f, 1f, 0.85f);
        public readonly Color Pink = new Color(1f, 0.32f, 0.68f);
        public readonly Color Lilac = new Color(0.63f, 0.47f, 1f);
        public Material Cream, Blush, Ink, Purple, Floor, Gold, White, MintGlow, PinkGlow, LilacGlow, Glow, Trail;
        readonly List<Object> owned = new List<Object>();
        public Mesh StarMesh;
        public CosmicArt(Shader glowShader)
        {
            Cream = Mat(new Color(0.91f, 0.88f, 1f));
            Blush = Mat(new Color(1f, 0.43f, 0.65f), 0.2f);
            Ink = Mat(new Color(0.025f, 0.035f, 0.09f));
            Purple = Mat(new Color(0.13f, 0.085f, 0.26f));
            Floor = Mat(new Color(0.12f, 0.19f, 0.28f), 0.18f);
            Gold = Mat(new Color(1f, 0.68f, 0.18f), 1.4f);
            White = Mat(Color.white, 0.65f);
            MintGlow = Mat(Mint, 1.25f); PinkGlow = Mat(Pink, 1.1f); LilacGlow = Mat(Lilac, 1.2f);
            Glow = new Material(glowShader); owned.Add(Glow);
            var ribbonTemplate=Resources.Load<Material>("StarPaws/Ribbon");
            Trail = ribbonTemplate ? new Material(ribbonTemplate) : new Material(Shader.Find("Sprites/Default")); owned.Add(Trail);
            StarMesh = MakeStar(); owned.Add(StarMesh);
        }
        public Material Mat(Color color, float glow = 0)
        {
            var template=Resources.Load<Material>(glow>0?"StarPaws/Neon":"StarPaws/Surface");
            var m = template ? new Material(template) : new Material(Shader.Find("Standard"));
            m.color = color; m.SetFloat("_Glossiness", 0.32f); m.SetFloat("_Metallic", 0.06f);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * glow); }
            owned.Add(m); return m;
        }
        public GameObject Shape(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var o = GameObject.CreatePrimitive(shape); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = position; o.transform.localScale = scale;
            Object.Destroy(o.GetComponent<Collider>());
            var r = o.GetComponent<Renderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off;
            return o;
        }
        public GameObject MeshObject(string name, Mesh mesh, Transform parent, Material material)
        {
            var o = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); o.transform.SetParent(parent, false);
            o.GetComponent<MeshFilter>().sharedMesh = mesh; o.GetComponent<MeshRenderer>().sharedMaterial = material;
            o.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; return o;
        }
        public LineRenderer Line(string name, Transform parent, Vector3[] points, Material material, float width, bool loop = false)
        {
            var o = new GameObject(name); o.transform.SetParent(parent, false); var l = o.AddComponent<LineRenderer>();
            l.sharedMaterial = material; l.useWorldSpace = false; l.positionCount = points.Length; l.SetPositions(points);
            l.startWidth = l.endWidth = width; l.loop = loop; l.numCornerVertices = 3; l.numCapVertices = 3;
            l.shadowCastingMode = ShadowCastingMode.Off; return l;
        }
        public LineRenderer Ring(string name, Transform parent, float radius, Material material, float width, int count = 80)
        {
            var p = new Vector3[count]; for (int i = 0; i < count; i++) { float a = i * Mathf.PI * 2 / count; p[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius; }
            return Line(name, parent, p, material, width, true);
        }
        public static Vector3[] Outline(float w, float l, float y, float bevel = 0.35f)
        {
            float x = w / 2, z = l / 2, b = Mathf.Min(bevel, Mathf.Min(x,z)*0.5f);
            return new[] {new Vector3(-x+b,y,-z),new Vector3(x-b,y,-z),new Vector3(x,y,-z+b),new Vector3(x,y,z-b),new Vector3(x-b,y,z),new Vector3(-x+b,y,z),new Vector3(-x,y,z-b),new Vector3(-x,y,-z+b)};
        }
        public Mesh PlatformMesh(float width, float length)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var top = Outline(width,length,0); var middle = Outline(width,length,-0.28f); var bottom = Outline(width*0.72f,length*0.82f,-0.8f);
            for (int i=0;i<8;i++)
            {
                int n=(i+1)%8; Triangle(v,t,Vector3.zero,top[n],top[i]);
                Triangle(v,t,top[i],top[n],middle[n]); Triangle(v,t,top[i],middle[n],middle[i]);
                Triangle(v,t,middle[i],middle[n],bottom[n]); Triangle(v,t,middle[i],bottom[n],bottom[i]);
                Triangle(v,t,new Vector3(0,-0.8f,0),bottom[i],bottom[n]);
            }
            var m=new Mesh {name="Faceted floating tile"}; m.SetVertices(v); m.SetTriangles(t,0); m.RecalculateNormals(); owned.Add(m); return m;
        }
        static void Triangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c) { int n=v.Count; v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2); }
        static Mesh MakeStar()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/5, b=(i+1)*Mathf.PI/5; float r=i%2==0?0.5f:0.24f, s=(i+1)%2==0?0.5f:0.24f;
                var p=new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0); var q=new Vector3(Mathf.Sin(b)*s,Mathf.Cos(b)*s,0);
                Triangle(v,t,new Vector3(0,0,-0.15f),p,q); Triangle(v,t,new Vector3(0,0,0.15f),q,p);
            }
            var mesh=new Mesh {name="Little five-point star"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        public Mesh EarMesh()
        {
            var v=new List<Vector3>();var t=new List<int>();
            var a=new Vector3(-0.25f,0,-0.18f);var b=new Vector3(0.25f,0,-0.18f);var c=new Vector3(0,0.63f,0);var d=new Vector3(0,0,0.23f);
            Triangle(v,t,a,c,b);Triangle(v,t,b,c,d);Triangle(v,t,d,c,a);Triangle(v,t,a,b,d);
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();owned.Add(m);return m;
        }
        public void Burst(Transform parent, Vector3 position, Color color, int count = 16, float speed = 2.6f)
        {
            var o=new GameObject("Stardust burst");o.transform.SetParent(parent,false);o.transform.position=position;
            var ps=o.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main=ps.main;
            main.loop=false;main.duration=0.1f;main.startLifetime=new ParticleSystem.MinMaxCurve(0.3f,0.85f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*0.4f,speed);main.startSize=new ParticleSystem.MinMaxCurve(0.12f,0.32f);
            main.startColor=color;main.gravityModifier=0.12f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=0.25f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=Glow;ps.Play();Object.Destroy(o,1.3f);
        }
        public void Dispose() { foreach(var o in owned) if(o) Object.Destroy(o); owned.Clear(); }
    }
}
