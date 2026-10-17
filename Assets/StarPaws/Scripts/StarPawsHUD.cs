using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarPaws
{
    public sealed class StarPawsHUD
    {
        readonly Transform root;
        readonly Font font;
        readonly Sprite rounded, starSprite;
        readonly Text score, best, stars, distance, ribbonLabel, toast, runLabel, finalScore, finalBest, modalTitle, modalKicker, buttonLabel;
        readonly GameObject intro, modal;
        readonly Image ribbonFill;
        readonly Button modalButton;
        readonly Color white=new Color(0.94f,0.94f,1), muted=new Color(0.57f,0.61f,0.76f), mint=new Color(0.38f,1,0.82f);
        readonly Action start, resume;
        readonly Texture2D roundTexture, starTexture;
        float toastUntil;
        public StarPawsHUD(Transform parent, Action start, Action resume, Action mute)
        {
            this.start=start;this.resume=resume;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var names=Font.GetOSInstalledFontNames();
            foreach(var preferred in new[]{"Hiragino Sans","Hiragino Kaku Gothic ProN","Yu Gothic","Meiryo","Noto Sans CJK JP"})
                if(Array.IndexOf(names,preferred)>=0){font=Font.CreateDynamicFontFromOSFont(preferred,28);break;}
            roundTexture=RoundedTexture();rounded=Sprite.Create(roundTexture,new Rect(0,0,64,64),Vector2.one*0.5f,100,0,SpriteMeshType.FullRect,new Vector4(20,20,20,20));
            starTexture=StarTexture();starSprite=Sprite.Create(starTexture,new Rect(0,0,64,64),Vector2.one*0.5f);
            var canvas=new GameObject("StarPaws • flight display",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(parent,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=0.5f;
            root=canvas.transform;
            if(!UnityEngine.Object.FindObjectOfType<EventSystem>()) {var es=new GameObject("UI input",typeof(EventSystem),typeof(StandaloneInputModule));es.transform.SetParent(parent,false);}
            var brand=Panel("Brand",root,new Vector2(46,-34),new Vector2(360,90),new Color(0,0,0,0),new Vector2(0,1));
            Icon(brand,new Vector2(0,-6),new Vector2(29,29),mint);
            Label(brand,"STARPAWS",new Vector2(42,0),new Vector2(270,38),27,white,FontStyle.Bold);
            runLabel=Label(brand,"NEON ORBIT  /  01",new Vector2(43,-42),new Vector2(300,24),11,muted);
            var stats=Panel("Score card",root,new Vector2(-44,-30),new Vector2(304,132),new Color(0.055f,0.065f,0.14f,0.9f),new Vector2(1,1));
            Label(stats,"S C O R E",new Vector2(24,-17),new Vector2(150,20),11,muted);
            score=Label(stats,"00000",new Vector2(22,-40),new Vector2(200,51),37,white,FontStyle.Bold);
            best=Label(stats,"BEST  00000",new Vector2(24,-99),new Vector2(190,22),11,muted);
            Icon(stats,new Vector2(224,-25),new Vector2(22,22),new Color(1,0.77f,0.35f));
            stars=Label(stats,"0",new Vector2(254,-24),new Vector2(50,28),18,white,FontStyle.Bold);
            distance=Label(stats,"0 m",new Vector2(207,-99),new Vector2(77,22),11,mint);distance.alignment=TextAnchor.UpperRight;
            intro=Panel("Welcome",root,new Vector2(70,115),new Vector2(650,360),Color.clear,new Vector2(0,0.5f)).gameObject;
            Label(intro.transform,"CATCH STARS. LEAVE RAINBOWS.",new Vector2(4,0),new Vector2(610,28),13,mint,FontStyle.Bold);
            Label(intro.transform,"STARPAWS",new Vector2(-4,-39),new Vector2(670,111),82,white,FontStyle.Bold);
            Label(intro.transform,"小さな一歩で、宇宙の果てへ。",new Vector2(4,-163),new Vector2(580,40),22,white);
            Label(intro.transform,"星をあつめて、虹をつなごう。\n光る足場は、崩れる前にひとっ飛び。",new Vector2(4,-212),new Vector2(600,70),17,muted);
            Button(intro.transform,"さあ、出発！     ›",new Vector2(4,-310),new Vector2(286,62),mint,start,out _);
            Label(intro.transform,"SPACE でスタート",new Vector2(314,-329),new Vector2(250,28),13,muted);
            var help=Panel("Keyboard guide",root,new Vector2(46,94),new Vector2(514,66),new Color(0.055f,0.065f,0.14f,0.86f),new Vector2(0,0));
            Key(help,"←  →",new Vector2(16,-15),new Vector2(76,35));Label(help,"移動",new Vector2(104,-24),new Vector2(54,25),13,white);
            Key(help,"SPACE",new Vector2(171,-15),new Vector2(81,35));Label(help,"ジャンプ",new Vector2(265,-24),new Vector2(84,25),13,white);
            Label(help,"ESC  一時停止",new Vector2(372,-24),new Vector2(135,25),11,muted);
            var rainbow=Panel("Rainbow meter",root,new Vector2(0,98),new Vector2(350,72),Color.clear,new Vector2(0.5f,0));
            ribbonLabel=Label(rainbow,"RAINBOW TRAIL  /  星をあつめよう",new Vector2(0,0),new Vector2(350,24),11,muted);ribbonLabel.alignment=TextAnchor.UpperCenter;
            Panel("Meter track",rainbow,new Vector2(0,-35),new Vector2(350,5),new Color(0.2f,0.22f,0.34f,0.7f));
            ribbonFill=Panel("Meter fill",rainbow,new Vector2(0,-35),new Vector2(0,5),mint).GetComponent<Image>();
            var note=Label(root,"MADE OF STARDUST  /  R  リトライ",new Vector2(-44,42),new Vector2(330,28),11,muted);Anchor(note.rectTransform,new Vector2(1,0));note.alignment=TextAnchor.MiddleRight;
            Button(root,"音 ♪",new Vector2(-44,-177),new Vector2(77,34),new Color(0.11f,0.12f,0.22f,0.85f),mute,out var soundLabel,new Vector2(1,1));soundLabel.color=muted;soundLabel.fontSize=11;
            toast=Label(root,"",new Vector2(0,-175),new Vector2(640,50),21,mint,FontStyle.Bold);Anchor(toast.rectTransform,new Vector2(0.5f,1));toast.alignment=TextAnchor.MiddleCenter;
            modal=Panel("Pause and results veil",root,Vector2.zero,new Vector2(1600,900),new Color(0.015f,0.02f,0.065f,0.78f),new Vector2(0.5f,0.5f)).gameObject;
            var veil=modal.GetComponent<RectTransform>();veil.anchorMin=Vector2.zero;veil.anchorMax=Vector2.one;veil.offsetMin=Vector2.zero;veil.offsetMax=Vector2.zero;
            var card=Panel("Flight report",modal.transform,new Vector2(0,255),new Vector2(510,510),new Color(0.065f,0.075f,0.16f,0.98f),new Vector2(0.5f,0.5f));
            Icon(card,new Vector2(226,-32),new Vector2(58,58),new Color(1,0.73f,0.32f));
            modalKicker=Label(card,"FLIGHT REPORT",new Vector2(35,-112),new Vector2(440,25),12,mint);modalKicker.alignment=TextAnchor.MiddleCenter;
            modalTitle=Label(card,"また、星を追いかけよう。",new Vector2(20,-153),new Vector2(470,46),25,white,FontStyle.Bold);modalTitle.alignment=TextAnchor.MiddleCenter;
            finalScore=Label(card,"00000",new Vector2(30,-214),new Vector2(450,65),52,white,FontStyle.Bold);finalScore.alignment=TextAnchor.MiddleCenter;
            finalBest=Label(card,"",new Vector2(30,-291),new Vector2(450,34),14,muted);finalBest.alignment=TextAnchor.MiddleCenter;
            modalButton=Button(card,"もう一度    /    SPACE",new Vector2(55,-357),new Vector2(400,64),mint,start,out buttonLabel);
            var hint=Label(card,"R ですぐリトライ  ·  星の数だけ、虹は長くなる",new Vector2(24,-451),new Vector2(462,25),11,muted);hint.alignment=TextAnchor.MiddleCenter;
            modal.SetActive(false);
        }
        RectTransform Panel(string name,Transform p,Vector2 pos,Vector2 size,Color c,Vector2? anchor=null)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(p,false);var rt=o.GetComponent<RectTransform>();Anchor(rt,anchor??new Vector2(0,1));rt.anchoredPosition=pos;rt.sizeDelta=size;
            var im=o.GetComponent<Image>();im.sprite=rounded;im.type=Image.Type.Sliced;im.color=c;im.raycastTarget=false;return rt;
        }
        static void Anchor(RectTransform rt,Vector2 a) {rt.anchorMin=rt.anchorMax=a;rt.pivot=new Vector2(a.x,1);}
        Text Label(Transform p,string value,Vector2 pos,Vector2 size,int fontSize,Color color,FontStyle style=FontStyle.Normal)
        {
            var o=new GameObject(value.Length>25?value.Substring(0,25):value,typeof(RectTransform),typeof(Text));o.transform.SetParent(p,false);
            var rt=o.GetComponent<RectTransform>();Anchor(rt,new Vector2(0,1));rt.anchoredPosition=pos;rt.sizeDelta=size;
            var t=o.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=fontSize;t.color=color;t.fontStyle=style;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
        }
        void Icon(Transform p,Vector2 pos,Vector2 size,Color color){var r=Panel("Star icon",p,pos,size,color);var im=r.GetComponent<Image>();im.sprite=starSprite;im.type=Image.Type.Simple;}
        void Key(Transform p,string text,Vector2 pos,Vector2 size){var r=Panel("Key",p,pos,size,new Color(0.18f,0.2f,0.31f));var t=Label(r,text,Vector2.zero,size,13,white,FontStyle.Bold);t.alignment=TextAnchor.MiddleCenter;}
        Button Button(Transform p,string value,Vector2 pos,Vector2 size,Color c,Action action,out Text label,Vector2? anchor=null)
        {
            var r=Panel(value,p,pos,size,c,anchor);r.GetComponent<Image>().raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>action());
            var colors=b.colors;colors.highlightedColor=new Color(1.1f,1.1f,1.1f);colors.pressedColor=new Color(0.8f,0.85f,0.9f);b.colors=colors;
            label=Label(r,value,Vector2.zero,size,18,new Color(0.025f,0.09f,0.13f),FontStyle.Bold);label.alignment=TextAnchor.MiddleCenter;return b;
        }
        public void Display(FlightState state,int value,int record,int count,float meters,float time)
        {
            score.text=value.ToString("D5");best.text="BEST  "+record.ToString("D5");stars.text=count.ToString();distance.text=Mathf.FloorToInt(meters)+" m";
            intro.SetActive(state==FlightState.Ready);modal.SetActive(state==FlightState.Fallen||state==FlightState.Paused);
            runLabel.text=state==FlightState.Ready?"NEON ORBIT  /  01":state==FlightState.Running?"●  EXPLORING THE COSMOS":"NEON ORBIT  /  REST STOP";
            ribbonFill.rectTransform.sizeDelta=new Vector2(350*Mathf.Clamp01(count/20f),5);ribbonFill.color=Color.HSVToRGB(Mathf.Repeat(time*0.09f,1),0.5f,1);
            ribbonLabel.text=count==0?"RAINBOW TRAIL  /  星をあつめよう":"RAINBOW TRAIL  /  "+Mathf.Min(3.6f,0.55f+count*0.16f).ToString("0.0")+" s";
            if(Time.unscaledTime>toastUntil)toast.text="";
        }
        public void ShowResults(bool paused,int value,int record,int count,float meters,bool newBest)
        {
            modalKicker.text=paused?"TAKE A LITTLE BREATHER":"FLIGHT REPORT";
            modalTitle.text=paused?"宇宙で、ひと休み。":"また、星を追いかけよう。";
            finalScore.text=value.ToString("D5");finalBest.text=paused?"ESC / SPACE でつづける":(newBest?"NEW BEST!   ·   ":"")+count+" STARS   ·   "+Mathf.FloorToInt(meters)+" m";
            buttonLabel.text=paused?"つづける    /    SPACE":"もう一度    /    SPACE";
            modalButton.onClick.RemoveAllListeners();modalButton.onClick.AddListener(()=>{if(paused)resume();else start();});
        }
        public void Toast(string message){toast.text=message;toastUntil=Time.unscaledTime+1.5f;}
        public void ClearToast(){toast.text="";}
        static Texture2D RoundedTexture()
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var p=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(20-x,x-43),dy=Mathf.Max(20-y,y-43);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;p[y*64+x]=new Color(1,1,1,Mathf.Clamp01(20-d));}t.SetPixels(p);t.Apply();return t;
        }
        static Texture2D StarTexture()
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var p=new Color[4096];var vertices=new Vector2[10];for(int i=0;i<10;i++){float a=i*Mathf.PI/5;vertices[i]=new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(i%2==0?29:13);}
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){var pt=new Vector2(x-31.5f,y-31.5f);bool inside=false;for(int i=0,j=9;i<10;j=i++)if((vertices[i].y>pt.y)!=(vertices[j].y>pt.y)&&pt.x<(vertices[j].x-vertices[i].x)*(pt.y-vertices[i].y)/(vertices[j].y-vertices[i].y)+vertices[i].x)inside=!inside;p[y*64+x]=new Color(1,1,1,inside?1:0);}t.SetPixels(p);t.Apply();return t;
        }
        public void Dispose(){UnityEngine.Object.Destroy(rounded);UnityEngine.Object.Destroy(starSprite);UnityEngine.Object.Destroy(roundTexture);UnityEngine.Object.Destroy(starTexture);}
    }
}
