using System.Collections.Generic;
using UnityEngine;
namespace StarPaws
{
    public sealed class CosmicAudio
    {
        readonly AudioSource effects, music;
        readonly AudioClip jump, collect, fall, land, loop;
        readonly List<AudioClip> clips=new List<AudioClip>();
        public bool Muted {get;private set;}
        public CosmicAudio(Transform parent)
        {
            var o=new GameObject("Locally synthesized sound");o.transform.SetParent(parent,false);
            effects=o.AddComponent<AudioSource>();effects.volume=0.24f;effects.spatialBlend=0;
            music=o.AddComponent<AudioSource>();music.volume=0.055f;music.loop=true;
            jump=Tone("Little leap",420,780,0.20f);collect=Tone("Starlight",880,1320,0.24f);fall=Tone("Soft landing in space",320,100,0.45f);land=Tone("Paw tap",180,90,0.09f);
            const int rate=22050;float duration=16;var data=new float[(int)(rate*duration)];
            int[] notes={0,7,12,16,12,7,4,7,0,7,12,19,16,12,7,4,5,12,17,21,17,12,9,12,7,14,19,23,19,14,11,7};
            for(int i=0;i<data.Length;i++){float t=i/(float)rate;int n=(int)(t/0.5f);float local=t%0.5f;float f=220*Mathf.Pow(2,notes[n%notes.Length]/12f);float env=Mathf.Min(1,local*35)*Mathf.Exp(-local*7);data[i]=(Mathf.Sin(t*f*2*Mathf.PI)+Mathf.Sin(t*f*Mathf.PI)*0.35f)*env*0.42f;}
            loop=AudioClip.Create("A small orbit • procedural lullaby",data.Length,1,rate,false);loop.SetData(data,0);clips.Add(loop);music.clip=loop;music.Play();
        }
        AudioClip Tone(string name,float from,float to,float seconds)
        {
            const int rate=22050;var data=new float[(int)(seconds*rate)];float phase=0;
            for(int i=0;i<data.Length;i++){float t=i/(float)data.Length;phase+=Mathf.Lerp(from,to,t)*2*Mathf.PI/rate;data[i]=(Mathf.Sin(phase)+0.18f*Mathf.Sin(phase*2))*Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-2*t)*0.7f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);clips.Add(clip);return clip;
        }
        public void Jump(){effects.PlayOneShot(jump);}
        public void Collect(int count){effects.pitch=1+count%5*0.04f;effects.PlayOneShot(collect);}
        public void Fall(){effects.pitch=1;effects.PlayOneShot(fall);}
        public void Land(){effects.pitch=1;effects.PlayOneShot(land,0.35f);}
        public void Toggle(){Muted=!Muted;effects.mute=music.mute=Muted;}
        public void Dispose(){foreach(var c in clips)if(c)Object.Destroy(c);}
    }
}
