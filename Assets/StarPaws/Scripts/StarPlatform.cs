using UnityEngine;
namespace StarPaws
{
    public sealed class StarPlatform
    {
        public readonly Transform Root;
        public readonly float Width, Length, X, Z;
        public bool Collected { get; private set; }
        public bool Touched { get; private set; }
        public bool Collapsed { get; private set; }
        public Vector3 StarPosition => star ? star.position : new Vector3(X,1.1f,Z);
        readonly Transform star, starHalo;
        readonly LineRenderer border;
        readonly Renderer surface;
        readonly CosmicArt art;
        readonly Transform effects;
        readonly Color color;
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        float touchTime;
        bool burst;
        public const float CollapseDelay = 0.96f;
        public StarPlatform(Transform parent, Transform effects, CosmicArt art, Mesh mesh, float x, float z, float width, float length, int palette, bool hasStar)
        {
            this.art=art;this.effects=effects;X=x;Z=z;Width=width;Length=length;
            color=palette%3==0?art.Mint:palette%3==1?art.Pink:art.Lilac;
            var neon=palette%3==0?art.MintGlow:palette%3==1?art.PinkGlow:art.LilacGlow;
            Root=new GameObject("Drifting tile").transform;Root.SetParent(parent,false);Root.position=new Vector3(x,0,z);
            art.MeshObject("Faceted underside",mesh,Root,art.Purple);
            var floor=art.Shape("Velvet deck",PrimitiveType.Cube,Root,new Vector3(0,0.006f,0),new Vector3(width-0.48f,0.035f,length-0.48f),art.Floor);
            surface=floor.GetComponent<Renderer>();
            border=art.Line("Neon rim",Root,CosmicArt.Outline(width-0.1f,length-0.1f,0.07f),neon,0.045f,true);
            art.Line("Lower light seam",Root,CosmicArt.Outline(width*0.88f,length*0.9f,-0.5f),neon,0.025f,true);
            for(int i=0;i<3;i++)
            {
                float zz=-length*0.30f+i*0.47f;
                art.Line("Forward chevron",Root,new[]{new Vector3(-0.27f,0.034f,zz),new Vector3(0,0.034f,zz+0.21f),new Vector3(0.27f,0.034f,zz)},neon,0.025f);
            }
            for(int side=-1;side<=1;side+=2)
                art.Shape("Side light",PrimitiveType.Cube,Root,new Vector3(side*(width/2-0.05f),-0.24f,0),new Vector3(0.035f,0.08f,length*0.27f),neon);
            if(hasStar)
            {
                star=art.MeshObject("Collectible star",art.StarMesh,Root,art.Gold).transform;star.localPosition=new Vector3(0,1.2f,0.5f);star.localScale=Vector3.one*0.85f;
                starHalo=art.Ring("Star orbit",Root,0.58f,art.Gold,0.012f,36).transform;starHalo.localPosition=new Vector3(0,1.1f,0.5f);starHalo.localRotation=Quaternion.Euler(70,0,0);
            }
        }
        public bool Supports(float x,float z)
        {
            if(Collapsed || !Root) return false;
            float dx=Mathf.Abs(x-X),dz=Mathf.Abs(z-Z);
            return dx <= Width/2+0.12f && dz <= Length/2+0.12f && dx+dz <= Width/2+Length/2-0.12f;
        }
        public void Touch(float time)
        {
            if(Touched)return;Touched=true;touchTime=time;
            art.Burst(effects,new Vector3(X,0.12f,Mathf.Clamp(Z-2,Z-Length/2,Z+Length/2)),color,9,1.2f);
            // Cracks are revealed on first contact and flare before the tile breaks.
            art.Line("Fracture",Root,new[]{new Vector3(-Width*0.45f,0.05f,0.7f),new Vector3(-0.4f,0.05f,0.2f),new Vector3(0.1f,0.05f,0.5f),new Vector3(Width*0.45f,0.05f,-0.2f)},art.White,0.035f);
        }
        public void Tick(float time,float visualTime)
        {
            if(!Root)return;
            if(star&&!Collected) { star.localPosition=new Vector3(0,1.2f+Mathf.Sin(visualTime*2.4f+Z)*0.17f,0.5f);star.localRotation=Quaternion.Euler(9,visualTime*65+Z*7,Mathf.Sin(visualTime*2)*8); }
            if(!Touched)return;
            float elapsed=time-touchTime;
            float flash=0.4f+Mathf.PingPong(elapsed*3,0.65f);
            block.SetColor("_Color",Color.Lerp(new Color(0.12f,0.19f,0.28f),color,0.45f));block.SetColor("_EmissionColor",color*flash*1.2f);surface.SetPropertyBlock(block);
            border.widthMultiplier=0.055f+Mathf.Sin(elapsed*20)*0.015f;
            if(elapsed<CollapseDelay) { Root.position=new Vector3(X,Mathf.Sin(elapsed*52)*0.015f*elapsed,Z);return; }
            Collapsed=true;
            float fall=elapsed-CollapseDelay;Root.position=new Vector3(X,-fall*fall*7,Z);Root.rotation=Quaternion.Euler(fall*28,0,fall*15);
            if(!burst) {burst=true;art.Burst(effects,new Vector3(X,0,Z),color,22,3.5f);}
        }
        public void Collect() { Collected=true;if(star)star.gameObject.SetActive(false);if(starHalo)starHalo.gameObject.SetActive(false); }
    }
}
