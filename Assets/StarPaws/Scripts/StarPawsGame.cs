using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarPaws
{
    public enum FlightState { Ready, Running, Paused, Fallen }

    /// <summary>Self-contained, asset-free endless runner. The kitten's feet are the collision origin.</summary>
    public sealed class StarPawsGame : MonoBehaviour
    {
        public Shader skyShader, glowShader, bloomShader;
        public FlightState State {get;private set;}
        public int Stars {get;private set;}
        public int Score => Mathf.FloorToInt(distance*3)+Stars*100;
        public int Best {get;private set;}
        public float Distance => distance;
        public float Speed => Mathf.Min(11.4f,7.2f+distance*0.004f);
        public Vector3 PlayerPosition => position;
        public bool Grounded => grounded;
        public int JumpCount {get;private set;}
        public int LandingCount {get;private set;}
        public int CollapseCount {get;private set;}
        public const float Gravity=24f, JumpVelocity=10.4f, LateralSpeed=7.8f;
        public const float CoyoteTime=0.13f, JumpBuffer=0.15f;
        readonly List<StarPlatform> platforms=new List<StarPlatform>();
        readonly Dictionary<string,Mesh> tileMeshes=new Dictionary<string,Mesh>();
        CosmicArt art;
        CatAvatar cat;
        CosmicBackdrop backdrop;
        StarPawsHUD hud;
        CosmicAudio audioFx;
        Transform track, effects;
        Camera cam;
        Material sky;
        Vector3 position, cameraVelocity;
        float verticalSpeed, horizontalSpeed, distance, runTime, visualTime, coyote, jumpBuffer;
        float nextRow, shake;
        int row, lane;
        bool grounded, newBest;
        System.Random layout;

        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            Best=PlayerPrefs.GetInt("StarPaws.Best",0);
            art=new CosmicArt(glowShader?glowShader:Shader.Find("StarPaws/SoftGlow"));
            foreach(var c in FindObjectsOfType<Camera>())if(c.gameObject!=gameObject)c.gameObject.SetActive(false);
            foreach(var light in FindObjectsOfType<Light>())light.enabled=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(0.42f,0.43f,0.61f);RenderSettings.fog=false;
            sky=new Material(skyShader?skyShader:Shader.Find("StarPaws/CosmicSky"));RenderSettings.skybox=sky;
            var key=new GameObject("Soft starlight").AddComponent<Light>();key.transform.SetParent(transform,false);key.type=LightType.Directional;key.color=new Color(0.80f,0.86f,1);key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(38,-25,0);key.shadows=LightShadows.None;
            var fill=new GameObject("Pink rim light").AddComponent<Light>();fill.transform.SetParent(transform,false);fill.type=LightType.Directional;fill.color=new Color(1,0.45f,0.69f);fill.intensity=0.65f;fill.transform.rotation=Quaternion.Euler(24,155,0);
            cam=new GameObject("StarPaws camera").AddComponent<Camera>();cam.transform.SetParent(transform,false);cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=52;cam.nearClipPlane=0.1f;cam.farClipPlane=300;cam.allowHDR=true;cam.allowMSAA=true;cam.gameObject.AddComponent<AudioListener>();
            cam.gameObject.AddComponent<StarPawsBloom>().bloomShader=bloomShader?bloomShader:Shader.Find("Hidden/StarPaws/Bloom");
            track=new GameObject("Endless stepping stones").transform;track.SetParent(transform,false);
            effects=new GameObject("Transient stardust").transform;effects.SetParent(transform,false);
            backdrop=new CosmicBackdrop(transform,art);cat=new CatAvatar(transform,art);audioFx=new CosmicAudio(transform);
            hud=new StarPawsHUD(transform,StartRun,Resume,ToggleAudio);
            ResetFlight();State=FlightState.Ready;UpdateCamera(true);
        }
        void ResetFlight()
        {
            foreach(var p in platforms)if(p.Root)Destroy(p.Root.gameObject);platforms.Clear();
            for(int i=effects.childCount-1;i>=0;i--)Destroy(effects.GetChild(i).gameObject);
            layout=new System.Random(817);position=new Vector3(0,0,0);verticalSpeed=horizontalSpeed=distance=runTime=0;
            Stars=0;JumpCount=LandingCount=CollapseCount=0;grounded=true;coyote=CoyoteTime;jumpBuffer=0;shake=0;row=0;lane=0;nextRow=12;
            cat.Reset();cat.Root.position=position;newBest=false;
            AddTile(0,0,9.3f,14,0,false);
            GenerateAhead();hud.ClearToast();
        }
        public void StartRun()
        {
            ResetFlight();State=FlightState.Running;UpdateCamera(true);hud.Toast("SPACE でジャンプ！  星に向かって進もう");
        }
        public void Pause()
        {
            if(State!=FlightState.Running)return;State=FlightState.Paused;hud.ShowResults(true,Score,Best,Stars,distance,false);
        }
        public void Resume(){if(State==FlightState.Paused)State=FlightState.Running;}
        void ToggleAudio(){audioFx.Toggle();hud.Toast(audioFx.Muted?"音を OFF にしました":"音を ON にしました");}
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,0.05f);
            if(Input.GetKeyDown(KeyCode.M))ToggleAudio();
            if(Input.GetKeyDown(KeyCode.R)){StartRun();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){if(State==FlightState.Paused)Resume();else Pause();}
            bool jump=Input.GetKeyDown(KeyCode.Space);
            if(State==FlightState.Ready||State==FlightState.Fallen){if(jump){StartRun();return;}}
            else if(State==FlightState.Paused){if(jump)Resume();}
            else
            {
                float axis=(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A)?1:0);
                // Small substeps prevent thin edge tunnelling during a slow rendered frame.
                int steps=Mathf.Max(1,Mathf.CeilToInt(dt/(1f/120)));for(int i=0;i<steps&&State==FlightState.Running;i++)Simulate(dt/steps,axis,jump&&i==0);
            }
            if(State!=FlightState.Paused)
            {
                visualTime+=dt;
                foreach(var p in platforms)p.Tick(runTime,visualTime);
                cat.Animate(visualTime,horizontalSpeed/LateralSpeed,State==FlightState.Running,!grounded,verticalSpeed);
                backdrop.Tick(position.z,visualTime);
            }
            cat.Root.position=position;
            cat.Root.localScale=Vector3.one*Mathf.Lerp(cat.Root.localScale.x,State==FlightState.Ready?1.45f:1.16f,dt*5);
            UpdateCamera(false);hud.Display(State,Score,Best,Stars,distance,visualTime);
        }
        public void Simulate(float dt,float axis,bool jumpPressed)
        {
            if(State!=FlightState.Running)return;
            runTime+=dt;distance+=Speed*dt;
            if(jumpPressed)jumpBuffer=JumpBuffer;else jumpBuffer=Mathf.Max(0,jumpBuffer-dt);
            StarPlatform before=SupportAt(position.x,position.z);
            if(grounded&&before==null)grounded=false;
            coyote=grounded?CoyoteTime:Mathf.Max(0,coyote-dt);
            if(jumpBuffer>0&&coyote>0){verticalSpeed=JumpVelocity;grounded=false;coyote=0;jumpBuffer=0;JumpCount++;audioFx.Jump();art.Burst(effects,position+Vector3.up*0.1f,art.Mint,8,1.3f);}
            horizontalSpeed=Mathf.MoveTowards(horizontalSpeed,Mathf.Clamp(axis,-1,1)*LateralSpeed,46*dt);
            Vector3 previous=position;position.x+=horizontalSpeed*dt;position.z+=Speed*dt;
            // Keep drift controllable, but never clamp the kitten onto a platform.
            position.x=Mathf.Clamp(position.x,-9,9);
            if(!grounded){verticalSpeed-=Gravity*dt;position.y+=verticalSpeed*dt;}
            var support=SupportAt(position.x,position.z);
            if(grounded&&support==null){grounded=false;verticalSpeed=0;}
            if(!grounded&&verticalSpeed<=0&&previous.y>=-0.08f&&position.y<=0&&support!=null)
            {grounded=true;position.y=0;verticalSpeed=0;LandingCount++;audioFx.Land();shake=0.04f;}
            if(grounded&&support!=null){position.y=0;support.Touch(runTime);}
            foreach(var p in platforms)
            {
                bool old=p.Collapsed;p.Tick(runTime,visualTime);if(!old&&p.Collapsed)CollapseCount++;
                if(!p.Collected&&p.Root&&Mathf.Abs(p.Z-position.z)<2.2f&&p.Z>1)
                {
                    Vector3 delta=p.StarPosition-(position+Vector3.up*0.9f);
                    if(delta.sqrMagnitude<1.15f*1.15f){p.Collect();Stars++;cat.SetStars(Stars);audioFx.Collect(Stars);art.Burst(effects,p.StarPosition,new Color(1,0.76f,0.25f),20,3);if(Stars==1)hud.Toast("虹のしっぽができた！  +100");else if(Stars%5==0)hud.Toast("RAINBOW +   "+Stars+" STARS!");}
                }
            }
            GenerateAhead();
            for(int i=platforms.Count-1;i>=0;i--)if(platforms[i].Z<position.z-25){Destroy(platforms[i].Root.gameObject);platforms.RemoveAt(i);}
            if(position.y<-3.5f)Fall();
        }
        void Fall()
        {
            State=FlightState.Fallen;newBest=Score>Best;if(newBest){Best=Score;PlayerPrefs.SetInt("StarPaws.Best",Best);PlayerPrefs.Save();}
            audioFx.Fall();hud.ClearToast();hud.ShowResults(false,Score,Best,Stars,distance,newBest);
        }
        StarPlatform SupportAt(float x,float z){foreach(var p in platforms)if(p.Supports(x,z))return p;return null;}
        void AddTile(float x,float z,float width,float length,int palette,bool star)
        {
            string key=width+"x"+length;if(!tileMeshes.TryGetValue(key,out var mesh)){mesh=art.PlatformMesh(width,length);tileMeshes.Add(key,mesh);}
            platforms.Add(new StarPlatform(track,effects,art,mesh,x,z,width,length,palette,star));
        }
        void GenerateAhead()
        {
            while(nextRow<position.z+160)
            {
                // Every transition keeps at least one reachable lane; no impossible random gaps.
                if(row<3)lane=0;else if(row%4!=0)lane=Mathf.Clamp(lane+layout.Next(-1,2),-1,1);
                bool broad=row%4==0;int extra=lane==0?(layout.Next(0,2)==0?-1:1):0;
                for(int l=-1;l<=1;l++)if(broad||l==lane||(row%3!=2&&l==extra))AddTile(l*3.15f,nextRow,3.05f,8,row+l+3,true);
                row++;nextRow+=10;
            }
        }
        void UpdateCamera(bool snap)
        {
            bool ready=State==FlightState.Ready;
            Vector3 desired=new Vector3(ready?7.3f:position.x*0.4f+4.5f,ready?7.3f:7.7f,position.z-(ready?12.0f:12.7f));
            if(snap){cam.transform.position=desired;cameraVelocity=Vector3.zero;}else if(State!=FlightState.Paused)cam.transform.position=Vector3.SmoothDamp(cam.transform.position,desired,ref cameraVelocity,0.20f);
            Vector3 look=new Vector3(ready?-0.5f:position.x*0.25f,0.4f,position.z+(ready?4.3f:6.2f));
            cam.transform.rotation=Quaternion.LookRotation(look-cam.transform.position);
            if(shake>0&&State==FlightState.Running){cam.transform.position+=Random.insideUnitSphere*shake;shake=Mathf.Max(0,shake-Time.deltaTime*0.4f);}
        }
        void OnApplicationFocus(bool focused){if(!focused&&State==FlightState.Running)Pause();}
        void OnDestroy(){audioFx?.Dispose();hud?.Dispose();art?.Dispose();if(sky)Destroy(sky);}
    }
}
