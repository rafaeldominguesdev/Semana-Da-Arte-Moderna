using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Arquitetura original inspirada nas galerias palacianas do Louvre, mantendo o percurso de 1922.</summary>
    public static class LouvreMuseumSetup
    {
        const string AssetRoot = "Assets/MuseumStyle";
        const string RootName = "Museum_LouvreStyle";
        static Transform root;
        static Material stone, plaster, gold, dark, velvet, glass, crystal;
        static Material[] wood;
        static readonly Dictionary<string, Geometry> parts = new Dictionary<string, Geometry>();
        static readonly Dictionary<string, Material> partMaterials = new Dictionary<string, Material>();
        static string zone;
        static int colliderIndex;

        [MenuItem("Museum/Visual/Aplicar galerias inspiradas no Louvre")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            if (UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None).Length != 28)
                throw new InvalidOperationException("A cena com as 28 fichas deve estar instalada antes do redesign.");
            EnsureFolder(AssetRoot); EnsureFolder(AssetRoot + "/Materials"); EnsureFolder(AssetRoot + "/Meshes");
            var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;
            root.gameObject.AddComponent<LouvreGalleryLighting>();
            parts.Clear(); partMaterials.Clear(); colliderIndex = 0;
            PrepareMaterials();
            DisableLegacy();
            Room("Salao", Vector3.zero, 20, 20, 6.4f, true, true, true, true, true);
            Room("Pintura", new Vector3(0,0,17), 16, 14, 5.2f, false, true, false, false);
            Room("Escultura", new Vector3(17,0,0), 14, 16, 5.2f, false, false, false, true);
            Room("Literatura", new Vector3(-17,0,0), 14, 16, 5.2f, false, false, true, false);
            Room("Entrada", new Vector3(0,0,-15), 10, 10, 5.2f, true, false, false, false);
            zone = "Salao";
            Dome(); Chandelier(new Vector3(0,5.5f,0));
            FloorMedallion();
            PortalCaption(new Vector3(0,5.35f,9.73f), Quaternion.Euler(0,180,0), "01   /   PINTURA");
            PortalCaption(new Vector3(9.73f,5.35f,0), Quaternion.Euler(0,-90,0), "02   /   ESCULTURA");
            PortalCaption(new Vector3(-9.73f,5.35f,0), Quaternion.Euler(0,90,0), "03   /   PALAVRA E MÚSICA");
            PortalCaption(new Vector3(0,5.35f,-9.73f), Quaternion.identity, "SEMANA   /   1922");
            FinishMeshes();
            LouvreExhibitionDetails.Apply(root, gold, stone, dark, velvet);
            ConfigureLight();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar o novo museu.");
            Debug.Log("[LouvreStyle] Galerias, arcos, cúpula, lustre, parquet e expografia salvos; 28 fichas preservadas.");
        }
        static void DisableLegacy()
        {
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (new[] {"Museum_Geometry", "Lixeiras", "Plantas", "Barreiras", "Pedestal", "Tapetes", "Bancos"}.Contains(go.name) || go.name.StartsWith("Placa_SA"))
                    go.SetActive(false);
            foreach (var builder in UnityEngine.Object.FindObjectsByType<MuseumCeilingBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None)) builder.enabled = false;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) light.enabled = false;
            foreach (var lod in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) lod.enabled = false;
            foreach (var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None)) probe.enabled = false;
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component.GetType().Name == "PostProcessLayer" || component.GetType().Name == "PostProcessVolume") component.enabled = false;
            foreach (var t in GameObject.Find("Museum_GuidedExhibition").GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Sinalizacao_") || t.name == "Luminaria") t.gameObject.SetActive(false);
        }
        static void PrepareMaterials()
        {
            stone = Mat("Calcario", new Color(.79f,.74f,.64f), .03f,.32f);
            plaster = Mat("Estuque", new Color(.89f,.86f,.78f), 0,.20f);
            gold = Mat("Bronze_dourado", new Color(.62f,.39f,.13f), .55f,.62f);
            dark = Mat("Verde_profundo", new Color(.055f,.105f,.09f), .08f,.38f);
            velvet = Mat("Veludo_vinho", new Color(.27f,.052f,.065f), 0,.20f);
            glass = Mat("Claraboia_leitosa", new Color(.80f,.86f,.88f), 0,.40f, .48f);
            crystal = Mat("Cristal_fosco", new Color(.92f,.86f,.66f), .16f,.75f,.27f);
            var tex = new Texture2D(256,256,TextureFormat.RGB24,false) { name="Veios_carvalho", wrapMode=TextureWrapMode.Repeat };
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                float u=x/256f,v=y/256f;
                float grain=.85f+.08f*Mathf.PerlinNoise(u*5,v*95)+.055f*Mathf.Sin(v*280+Mathf.PerlinNoise(u*3,v*6)*8);
                tex.SetPixel(x,y,new Color(grain,grain,grain));
            }
            tex.Apply(); var saved = SaveAsset(tex, AssetRoot+"/Materials/Veios_carvalho.asset");
            wood=new Material[7];
            for(int i=0;i<wood.Length;i++)
            {
                float t=(i-3)*.035f;
                wood[i]=Mat("Carvalho_"+i,new Color(.43f+t,.245f+t*.7f,.115f+t*.45f),0,.34f);
                wood[i].mainTexture=saved;
            }
        }
        static Material Mat(string name, Color color, float metallic, float smooth, float emission=0)
        {
            string path=AssetRoot+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color; m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);
            if(emission>0) {m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
            else {m.DisableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;}
            EditorUtility.SetDirty(m);return m;
        }
        static void Room(string name,Vector3 c,float w,float d,float h,bool n,bool s,bool e,bool west,bool central=false)
        {
            zone=name;
            Floor(c,w,d);
            Wall(c+new Vector3(0,0,d/2),Quaternion.Euler(0,180,0),w,h,n);
            Wall(c+new Vector3(0,0,-d/2),Quaternion.identity,w,h,s);
            Wall(c+new Vector3(w/2,0,0),Quaternion.Euler(0,-90,0),d,h,e);
            Wall(c+new Vector3(-w/2,0,0),Quaternion.Euler(0,90,0),d,h,west);
            if(!central) Vault(c,w-.3f,d-.3f,4.88f,1.75f);
        }
        static void Wall(Vector3 origin,Quaternion rot,float width,float height,bool door)
        {
            const float halfDoor=2.2f,spring=2.7f,thickness=.125f;
            Action<Vector3,Vector3,Material,bool> box=(p,s,m,collide)=>
            {
                Box(origin+rot*p,s,m,rot);
                if(collide) ColliderBox(origin+rot*p,s,rot);
            };
            if(door)
            {
                float seg=width/2-halfDoor;
                for(int sign=-1;sign<=1;sign+=2) box(new Vector3(sign*(halfDoor+seg/2),height/2,thickness/2),new Vector3(seg,height,thickness),stone,true);
                for(int i=0;i<32;i++)
                {
                    float x0=Mathf.Lerp(-halfDoor,halfDoor,i/32f),x1=Mathf.Lerp(-halfDoor,halfDoor,(i+1)/32f);
                    float y0=spring+Mathf.Sqrt(Mathf.Max(0,halfDoor*halfDoor-x0*x0));
                    float y1=spring+Mathf.Sqrt(Mathf.Max(0,halfDoor*halfDoor-x1*x1));
                    Quad(origin+rot*new Vector3(x0,y0,thickness),origin+rot*new Vector3(x0,height,thickness),origin+rot*new Vector3(x1,height,thickness),origin+rot*new Vector3(x1,y1,thickness),stone,true);
                }
                box(new Vector3(0,(height+4.9f)/2,thickness/2),new Vector3(4.4f,height-4.9f,thickness),stone,true);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    var p=origin+rot*new Vector3(sign*2.53f,0,.31f);
                    Column(p,2.75f,.25f);
                    box(new Vector3(sign*2.30f,1.34f,.2f),new Vector3(.11f,2.68f,.16f),plaster,false);
                }
                for(int band=0;band<3;band++)
                {
                    var path=new List<Vector3>();
                    for(int i=0;i<=48;i++) {float a=Mathf.PI*i/48;path.Add(origin+rot*new Vector3(Mathf.Cos(a)*(2.24f+band*.11f),spring+Mathf.Sin(a)*(2.24f+band*.11f),.24f));}
                    Tube(path,band==1?.028f:.063f,band==1?gold:plaster,8);
                }
                box(new Vector3(0,4.95f,.28f),new Vector3(.25f,.34f,.18f),plaster,false);
            }
            else box(new Vector3(0,height/2,thickness/2),new Vector3(width,height,thickness),stone,true);
            // Socle, cimaise et corniche: profils étagés qui accrochent la lumière.
            float[] levels={.09f,.22f,.88f,.98f,height-.42f,height-.25f,height-.09f};
            for(int i=0;i<levels.Length;i++)
            {
                float y=levels[i],depth=i<4?.18f:.27f, tall=(i==0?.18f:i==3?.065f:.055f);
                if(door && y<4.95f)
                {
                    float len=width/2-2.68f;
                    for(int sign=-1;sign<=1;sign+=2)box(new Vector3(sign*(2.68f+len/2),y,depth/2),new Vector3(len,tall,depth),i==2?gold:plaster,false);
                }
                else box(new Vector3(0,y,depth/2),new Vector3(width,tall,depth),i==2?gold:plaster,false);
            }
            for(float x=-width/2+.9f;x<width/2-.5f;x+=1.5f)
            {
                if(door && Mathf.Abs(x)<3.25f)continue;
                for(int edge=-1;edge<=1;edge+=2)
                {
                    box(new Vector3(x+edge*.59f,.54f,.14f),new Vector3(.025f,.48f,.022f),plaster,false);
                    box(new Vector3(x,.54f+edge*.24f,.14f),new Vector3(1.18f,.025f,.022f),plaster,false);
                }
            }
        }
        static void Column(Vector3 origin,float h,float radius)
        {
            Vector2[] profile={new Vector2(radius*1.4f,0),new Vector2(radius*1.4f,.12f),new Vector2(radius*1.13f,.17f),new Vector2(radius*1.2f,.23f),new Vector2(radius,.30f),new Vector2(radius*.90f,h-.32f),new Vector2(radius*1.15f,h-.27f),new Vector2(radius*1.4f,h-.18f),new Vector2(radius*1.4f,h)};
            Lathe(origin,profile,plaster,24);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6; var p=origin+new Vector3(Mathf.Cos(a)*radius*.93f,.35f,Mathf.Sin(a)*radius*.93f);
                Tube(new[]{p,p+Vector3.up*(h-.76f)},.014f,stone,5);
            }
        }
        static void Vault(Vector3 c,float width,float depth,float spring,float rise)
        {
            for(int i=0;i<40;i++)
            {
                float a0=i*Mathf.PI/40,a1=(i+1)*Mathf.PI/40;
                Vector3 p0=c+new Vector3(Mathf.Cos(a0)*width/2,spring+Mathf.Sin(a0)*rise,-depth/2);
                Vector3 p1=c+new Vector3(Mathf.Cos(a1)*width/2,spring+Mathf.Sin(a1)*rise,-depth/2);
                Quad(p0,p1,p1+Vector3.forward*depth,p0+Vector3.forward*depth,i>=15&&i<25?glass:plaster,true);
                // Fecha as lunetas entre o topo das paredes e a abóbada.
                float bottom=5.18f;
                for(int sign=-1;sign<=1;sign+=2)
                {
                    var a=c+new Vector3(Mathf.Cos(a0)*width/2,bottom,sign*depth/2);
                    var b=c+new Vector3(Mathf.Cos(a1)*width/2,bottom,sign*depth/2);
                    Quad(a,b,new Vector3(b.x,Mathf.Max(c.y+bottom,p1.y),b.z),new Vector3(a.x,Mathf.Max(c.y+bottom,p0.y),a.z),plaster,true);
                }
            }
            int bays=Mathf.Max(2,Mathf.RoundToInt(depth/4.5f));
            for(int r=0;r<=bays;r++)
            {
                float z=Mathf.Lerp(-depth/2,depth/2,r/(float)bays);
                var path=new List<Vector3>();
                for(int j=0;j<=48;j++){float a=Mathf.PI*j/48;path.Add(c+new Vector3(Mathf.Cos(a)*width/2,spring+Mathf.Sin(a)*rise-.035f,z));}
                Tube(path,.07f,stone,8);
                if(r>0&&r<bays)
                    for(int sign=-1;sign<=1;sign+=2)
                        Box(c+new Vector3(sign*(width/2-.10f),4.6f,z),new Vector3(.26f,.50f,.33f),plaster);
            }
            for(int j=15;j<=25;j+=2)
            {
                float a=Mathf.PI*j/40;
                Tube(new[]{c+new Vector3(Mathf.Cos(a)*width/2,spring+Mathf.Sin(a)*rise-.06f,-depth/2),c+new Vector3(Mathf.Cos(a)*width/2,spring+Mathf.Sin(a)*rise-.06f,depth/2)},.026f,gold,6);
            }
        }
        static void Dome()
        {
            const float radius=7.45f,baseY=6.3f,rise=3.2f;
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                float sr=10/Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z)),sq=10/Mathf.Max(Mathf.Abs(q.x),Mathf.Abs(q.z));
                Quad(p*radius+Vector3.up*baseY,p*sr+Vector3.up*baseY,q*sq+Vector3.up*baseY,q*radius+Vector3.up*baseY,plaster,true);
                for(int j=0;j<18;j++)
                {
                    float t0=j*Mathf.PI/36,t1=(j+1)*Mathf.PI/36;
                    Vector3 p0=p*(radius*Mathf.Cos(t0))+Vector3.up*(baseY+rise*Mathf.Sin(t0));
                    Vector3 q0=q*(radius*Mathf.Cos(t0))+Vector3.up*(baseY+rise*Mathf.Sin(t0));
                    Vector3 p1=p*(radius*Mathf.Cos(t1))+Vector3.up*(baseY+rise*Mathf.Sin(t1));
                    Vector3 q1=q*(radius*Mathf.Cos(t1))+Vector3.up*(baseY+rise*Mathf.Sin(t1));
                    Quad(p0,q0,q1,p1,j>=14?glass:plaster,true);
                }
            }
            foreach(float r in new[]{radius,radius-.15f,2.54f}) Circle(new Vector3(0,r>3?baseY-.045f:9.3f,0),r,.045f,gold);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;var path=new List<Vector3>();
                for(int j=0;j<=28;j++){float t=j/28f*1.22f;path.Add(new Vector3(Mathf.Cos(a)*radius*Mathf.Cos(t),baseY+rise*Mathf.Sin(t)-.05f,Mathf.Sin(a)*radius*Mathf.Cos(t)));}
                Tube(path,.035f,gold,6);
            }
            for(int sign=-1;sign<=1;sign+=2)for(int side=-1;side<=1;side+=2)
                Column(new Vector3(sign*8.7f,0,side*8.7f),6.18f,.33f);
        }
        static void Chandelier(Vector3 center)
        {
            Tube(new[]{new Vector3(0,9.45f,0),center+Vector3.up*.8f},.042f,gold,10);
            Lathe(center,new[]{new Vector2(.04f,-1.0f),new Vector2(.20f,-.75f),new Vector2(.11f,-.55f),new Vector2(.28f,-.35f),new Vector2(.14f,.1f),new Vector2(.38f,.3f),new Vector2(.13f,.65f),new Vector2(.05f,.9f)},gold,24);
            for(int tier=0;tier<2;tier++)
            {
                float y=tier==0?0:.9f, r=tier==0?1.8f:1.1f;
                Circle(center+Vector3.up*(y-.12f),r*.7f,.035f,gold);
                int arms=tier==0?16:10;
                for(int i=0;i<arms;i++)
                {
                    float a=i*Mathf.PI*2/arms;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var path=new List<Vector3>();
                    for(int j=0;j<=20;j++){float t=j/20f;path.Add(center+dir*(.18f+(r-.18f)*t)+Vector3.up*(y-.38f*Mathf.Sin(t*Mathf.PI)+.28f*t*t));}
                    Tube(path,.028f,gold,6);
                    var candle=center+dir*r+Vector3.up*(y+.24f);
                    Lathe(candle,new[]{new Vector2(.02f,-.04f),new Vector2(.13f,0),new Vector2(.12f,.035f),new Vector2(.035f,.065f),new Vector2(.035f,.25f)},gold,14);
                    Lathe(candle+Vector3.up*.25f,new[]{new Vector2(.025f,0),new Vector2(.055f,.07f),new Vector2(.04f,.13f),new Vector2(0,.22f)},crystal,10);
                    for(int k=0;k<3;k++)
                    {
                        Vector3 drop=center+dir*(r*(.45f+k*.20f))+Vector3.up*(y-.29f-k*.035f);
                        Tube(new[]{drop,drop-Vector3.up*.10f},.01f,gold,5);
                        Lathe(drop-Vector3.up*.32f,new[]{new Vector2(0,0),new Vector2(.075f,.095f),new Vector2(.045f,.17f),new Vector2(.012f,.22f)},crystal,8);
                    }
                }
            }
        }
        static void FloorMedallion()
        {
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                G(stone).Triangle(new Vector3(0,.016f,0),new Vector3(Mathf.Cos(a)*3.6f,.016f,Mathf.Sin(a)*3.6f),new Vector3(Mathf.Cos(b)*3.6f,.016f,Mathf.Sin(b)*3.6f),true);
            }
            Circle(new Vector3(0,.022f,0),3.6f,.025f,gold);Circle(new Vector3(0,.022f,0),3.35f,.014f,dark);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-dir.z,0,dir.x);
                G(i%2==0?gold:dark).Triangle(dir*.4f+Vector3.up*.018f,dir*(i%2==0?2.8f:2.0f)+Vector3.up*.018f,dir*.7f+side*.22f+Vector3.up*.018f,true);
                G(i%2==0?gold:dark).Triangle(dir*.4f+Vector3.up*.018f,dir*.7f-side*.22f+Vector3.up*.018f,dir*(i%2==0?2.8f:2.0f)+Vector3.up*.018f,true);
            }
        }
        static void Floor(Vector3 center,float w,float d)
        {
            Box(center+Vector3.down*.09f,new Vector3(w,.18f,d),dark);ColliderBox(center+Vector3.down*.09f,new Vector3(w,.18f,d),Quaternion.identity);
            // Parquet em espinha: duas tábuas por célula de rede, recortadas na borda de cada sala.
            const float unit=.23f; const int n=6; var turn=Quaternion.Euler(0,45,0);
            int extent=Mathf.CeilToInt((w+d)/unit);
            for(int i=-extent/n;i<=extent/n;i++)for(int j=-extent;j<=extent;j++)
            {
                float x=(i*n-j)*unit,z=(i*n+j)*unit;
                Plank(x,z,n*unit,unit,false,i,j);Plank(x+n*unit,z,unit,n*unit,true,i+9,j+5);
            }
            void Plank(float x,float z,float sx,float sz,bool vertical,int i,int j)
            {
                float gap=.002f;
                var points=new List<Vector3>{turn*new Vector3(x+gap,.008f,z+gap),turn*new Vector3(x+gap,.008f,z+sz-gap),turn*new Vector3(x+sx-gap,.008f,z+sz-gap),turn*new Vector3(x+sx-gap,.008f,z+gap)};
                for(int edge=0;edge<4&&points.Count>0;edge++) points=Clip(points,edge,edge<2?w/2-.20f:d/2-.20f);
                if(points.Count<3)return;
                var geom=G(wood[Mathf.Abs(i*31+j*17)%wood.Length]);
                var uvs=points.Select(p=>{var q=Quaternion.Inverse(turn)*p;return vertical?new Vector2((q.z-z)/sz,(q.x-x)/sx):new Vector2((q.x-x)/sx,(q.z-z)/sz);}).ToArray();
                geom.Polygon(points.Select(p=>p+center).ToArray(),uvs);
            }
            for(int sign=-1;sign<=1;sign+=2)
            {
                Box(center+new Vector3(0,.006f,sign*(d/2-.10f)),new Vector3(w,.012f,.20f),wood[2]);
                Box(center+new Vector3(sign*(w/2-.10f),.006f,0),new Vector3(.20f,.012f,d),wood[2]);
                Box(center+new Vector3(0,.014f,sign*(d/2-.205f)),new Vector3(w-.4f,.008f,.015f),gold);
                Box(center+new Vector3(sign*(w/2-.205f),.014f,0),new Vector3(.015f,.008f,d-.4f),gold);
            }
        }
        static List<Vector3> Clip(List<Vector3> points,int edge,float bound)
        {
            var output=new List<Vector3>();
            Func<Vector3,float> f=p=>edge==0?p.x-bound:edge==1?-p.x-bound:edge==2?p.z-bound:-p.z-bound;
            for(int i=0;i<points.Count;i++)
            {
                var a=points[i];var b=points[(i+1)%points.Count];float da=f(a),db=f(b);
                if(da<=0)output.Add(a);if((da<=0)!=(db<=0))output.Add(Vector3.Lerp(a,b,da/(da-db)));
            }
            return output;
        }
        static void ConfigureLight()
        {
            LouvreGalleryLighting.ApplyDaylight();
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                renderer.lightProbeUsage=LightProbeUsage.Off;
            var reflection=new Cubemap(32,TextureFormat.RGB24,false);
            foreach(CubemapFace face in new[]{CubemapFace.PositiveX,CubemapFace.NegativeX,CubemapFace.PositiveY,CubemapFace.NegativeY,CubemapFace.PositiveZ,CubemapFace.NegativeZ})
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float panel=Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(x-15.5f)/10),3);
                    Color tone=face==CubemapFace.PositiveY?new Color(.88f,.90f,.91f):face==CubemapFace.NegativeY?new Color(.25f,.18f,.11f):Color.Lerp(new Color(.43f,.39f,.31f),new Color(.84f,.82f,.72f),panel*.8f);
                    reflection.SetPixel(face,x,y,tone);
                }
            reflection.Apply();RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflection=SaveAsset(reflection,AssetRoot+"/Materials/Reflexo_da_galeria.asset");
            RenderSettings.reflectionIntensity=.75f;
            var sun=new GameObject("Luz_diurna");sun.transform.SetParent(root);sun.transform.rotation=Quaternion.Euler(65,-25,0);
            var l=sun.AddComponent<Light>();l.type=LightType.Directional;l.color=new Color(1,.94f,.82f);l.intensity=.65f;l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;
            foreach(var p in new[]{new Vector3(0,5.5f,0),new Vector3(0,4.5f,17),new Vector3(17,4.5f,0),new Vector3(-17,4.5f,0),new Vector3(0,4.5f,-15)})
            {
                var go=new GameObject("Luz_difusa");go.transform.SetParent(root);go.transform.position=p;
                var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=13;light.intensity=.75f;light.color=new Color(1,.89f,.73f);light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForceVertex;
            }
            QualitySettings.pixelLightCount=2;
        }
        static void PortalCaption(Vector3 pos,Quaternion rot,string value)
        {
            var go=new GameObject("Inscricao_"+value);go.transform.SetParent(root);go.transform.SetPositionAndRotation(pos,rot*Quaternion.Euler(0,180,0));
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=96;text.characterSize=.045f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=gold.color;
        }
        static Geometry G(Material mat)
        {
            string key=zone+"_"+mat.name;if(!parts.TryGetValue(key,out var g)){g=new Geometry();parts[key]=g;partMaterials[key]=mat;}return g;
        }
        static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material mat,bool two=false)=>G(mat).Quad(a,b,c,d,two);
        static void Box(Vector3 p,Vector3 scale,Material mat,Quaternion? rotation=null)
        {
            Quaternion q=rotation??Quaternion.identity;Vector3[] v=new Vector3[8];
            for(int i=0;i<8;i++)v[i]=p+q*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),scale);
            var g=G(mat);g.Quad(v[0],v[2],v[3],v[1]);g.Quad(v[4],v[5],v[7],v[6]);g.Quad(v[0],v[4],v[6],v[2]);g.Quad(v[1],v[3],v[7],v[5]);g.Quad(v[2],v[6],v[7],v[3]);g.Quad(v[0],v[1],v[5],v[4]);
        }
        static void Tube(IList<Vector3> path,float radius,Material mat,int sides)
        {
            var geom=G(mat);
            for(int i=0;i<path.Count-1;i++)
            {
                var t=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;
                var n=Vector3.Cross(t,Mathf.Abs(t.y)<.9f?Vector3.up:Vector3.right).normalized;var b=Vector3.Cross(t,n);
                var t2=(path[Mathf.Min(i+2,path.Count-1)]-path[i]).normalized;
                var n2=Vector3.Cross(t2,Mathf.Abs(t2.y)<.9f?Vector3.up:Vector3.right).normalized;var b2=Vector3.Cross(t2,n2);
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides,a2=(j+1)*Mathf.PI*2/sides;
                    geom.Quad(path[i]+radius*(n*Mathf.Cos(a)+b*Mathf.Sin(a)),path[i]+radius*(n*Mathf.Cos(a2)+b*Mathf.Sin(a2)),path[i+1]+radius*(n2*Mathf.Cos(a2)+b2*Mathf.Sin(a2)),path[i+1]+radius*(n2*Mathf.Cos(a)+b2*Mathf.Sin(a)));
                }
            }
        }
        static void Circle(Vector3 center,float radius,float thickness,Material mat)
        {
            var points=new List<Vector3>();for(int i=0;i<=96;i++){float a=i*Mathf.PI/48;points.Add(center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}Tube(points,thickness,mat,6);
        }
        static void Lathe(Vector3 origin,IList<Vector2> profile,Material mat,int sides)
        {
            for(int i=0;i<profile.Count-1;i++)for(int j=0;j<sides;j++)
            {
                float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                Vector3 P(Vector2 p,float angle)=>origin+new Vector3(Mathf.Cos(angle)*p.x,p.y,Mathf.Sin(angle)*p.x);
                G(mat).Quad(P(profile[i],a),P(profile[i+1],a),P(profile[i+1],b),P(profile[i],b));
            }
        }
        static void ColliderBox(Vector3 p,Vector3 s,Quaternion q)
        {
            var go=new GameObject("Colisao_"+(colliderIndex++));go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,q);go.AddComponent<BoxCollider>().size=s;
        }
        static void FinishMeshes()
        {
            foreach(var pair in parts)
            {
                var mesh=pair.Value.ToMesh(pair.Key);var saved=SaveAsset(mesh,AssetRoot+"/Meshes/Architecture_"+pair.Key+".asset");
                var go=new GameObject(pair.Key,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterial=partMaterials[pair.Key];
                go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            }
        }
        static T SaveAsset<T>(T obj,string path) where T:UnityEngine.Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null){AssetDatabase.CreateAsset(obj,path);return obj;}
            EditorUtility.CopySerialized(obj,existing);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(existing);return existing;
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
        }
        sealed class Geometry
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<int> triangles=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool two=false)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
                if(two)Quad(d,c,b,a);
            }
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,bool two)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one});triangles.AddRange(new[]{i,i+1,i+2});if(two)Triangle(c,b,a,false);
            }
            public void Polygon(Vector3[] v,Vector2[] coords)
            {
                int start=vertices.Count;vertices.AddRange(v);uv.AddRange(coords);for(int i=1;i<v.Length-1;i++)triangles.AddRange(new[]{start,start+i,start+i+1});
            }
            public Mesh ToMesh(string name)
            {
                var m=new Mesh{name=name,indexFormat=vertices.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;
            }
        }
    }
}
