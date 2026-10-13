using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    // Finite, fixed world terrain. Mesh chunks share one macro layer and one world-pixel grid.
    [DefaultExecutionOrder(500)]
    public sealed class FiniteStomachMap : MonoBehaviour
    {
        public Rect WorldRect { get; private set; }
        public StomachMapDefinition Definition { get; private set; }
        public int ChunkCount => chunks.Count;
        public bool FieldVisible { get; private set; }
        public static FiniteStomachMap Instance { get; private set; }
        StageProgression progression;
        public bool StageOne
        {
            get { if(level==null)return false;if(progression==null)progression=level.GetComponent<StageProgression>();return progression==null || progression.StageNumber==1; }
        }
        public float WallWidth => Definition.wallWidth;
        public float ClimbLimit => Mathf.Max(.5f, WallWidth-Definition.climbableWidth);
        public Material TerrainMaterial => material;
        Texture2D distanceTexture;
        const int FieldResolution = 512;
        Color[] distanceSamples;
        readonly List<GameObject> chunks = new List<GameObject>();
        readonly List<Mesh> meshes = new List<Mesh>();
        LevelManager level;
        InfiniteBackground legacy;
        Renderer legacyRenderer;
        Material material;
        Vector2[] boundary;
        bool legacyEnabled;
        bool legacyRendererEnabled;

        public static FiniteStomachMap Install(LevelManager owner, StomachMapDefinition definition, InfiniteBackground background)
        {
            if (definition == null || definition.terrainMaterial == null) return null;
            var existing=owner.GetComponent<FiniteStomachMap>();
            if (existing != null) return existing;
            var map=owner.gameObject.AddComponent<FiniteStomachMap>();
            map.level=owner; map.Definition=definition; map.legacy=background;
            map.Build(); return map;
        }

        void Build()
        {
            Instance=this;
            Vector2 size=Definition.Size;
            Vector2 origin=(Vector2)level.PlayerCharacter.transform.position-Vector2.Scale(Definition.startUV,size);
            WorldRect=new Rect(origin,size);
            material=new Material(Definition.terrainMaterial) { name="Finite Stomach (Runtime)", hideFlags=HideFlags.DontSave };
            material.SetVector("_MapRect",new Vector4(origin.x,origin.y,size.x,size.y));
            material.SetFloat("_PixelSize",Definition.pixelWorldSize);
            int cols=Mathf.CeilToInt(size.x/Definition.chunkSize), rows=Mathf.CeilToInt(size.y/Definition.chunkSize);
            for(int y=0;y<rows;y++) for(int x=0;x<cols;x++)
            {
                float left=origin.x+x*Definition.chunkSize, bottom=origin.y+y*Definition.chunkSize;
                float width=Mathf.Min(Definition.chunkSize,WorldRect.xMax-left);
                float height=Mathf.Min(Definition.chunkSize,WorldRect.yMax-bottom);
                var go=new GameObject($"Stomach terrain [{x},{y}]"); go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(left,bottom,0);
                var mesh=new Mesh { name=$"Stomach chunk {x},{y}" };
                mesh.vertices=new[]{Vector3.zero,new Vector3(width,0,0),new Vector3(width,height,0),new Vector3(0,height,0)};
                mesh.triangles=new[]{0,2,1,0,3,2}; mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false; GroundVisualSorting.ApplyBackground(renderer,-1000);
                chunks.Add(go); meshes.Add(mesh);
            }
            boundary=CreateBoundary();
            BuildDistanceField();
            StomachSlopeBody.Ensure(level.PlayerCharacter.gameObject);
            if(legacy!=null)
            {
                legacyEnabled=legacy.enabled; legacyRenderer=legacy.GetComponent<Renderer>();
                legacyRendererEnabled=legacyRenderer!=null && legacyRenderer.enabled;
                legacy.enabled=false; if(legacyRenderer!=null)legacyRenderer.enabled=false;
            }
            foreach(var director in FindObjectsOfType<MiniStageDirector>()) director.KeepRoomsOutside(WorldRect);
            FieldVisible=true;
        }

        public Vector2 UVToWorld(Vector2 uv) => WorldRect.min+Vector2.Scale(uv,WorldRect.size);

        // A smooth closed outline just inside the outer rim. Both anatomical openings remain
        // visual landmarks until their travel mechanics are designed; they are not stage portals.
        Vector2[] CreateBoundary()
        {
            Vector2[] uv={new Vector2(.032f,.49f),new Vector2(.055f,.70f),new Vector2(.13f,.85f),
                new Vector2(.245f,.927f),new Vector2(.37f,.87f),new Vector2(.445f,.79f),
                new Vector2(.50f,.735f),new Vector2(.56f,.735f),new Vector2(.655f,.80f),
                new Vector2(.76f,.855f),new Vector2(.81f,.89f),new Vector2(.849f,.905f),
                new Vector2(.875f,.845f),new Vector2(.91f,.773f),new Vector2(.948f,.65f),
                new Vector2(.962f,.52f),new Vector2(.957f,.385f),new Vector2(.904f,.245f),
                new Vector2(.815f,.17f),new Vector2(.68f,.113f),new Vector2(.56f,.111f),
                new Vector2(.442f,.147f),new Vector2(.336f,.194f),new Vector2(.265f,.205f),
                new Vector2(.20f,.24f),new Vector2(.13f,.30f),new Vector2(.076f,.377f)};
            var points=new List<Vector2>();
            for(int i=0;i<uv.Length;i++) for(int step=0;step<6;step++)
            {
                Vector2 a=uv[(i+uv.Length-1)%uv.Length],b=uv[i],c=uv[(i+1)%uv.Length],d=uv[(i+2)%uv.Length];
                float t=step/6f,t2=t*t,t3=t2*t;
                var p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3);
                points.Add(UVToWorld(p));
            }
            return points.ToArray();
        }

        public bool Contains(Vector2 p)
        {
            bool inside=false;
            for(int i=0,j=boundary.Length-1;i<boundary.Length;j=i++)
            {
                Vector2 a=boundary[i],b=boundary[j];
                if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside;
        }

        public Vector2 ClampToField(Vector2 p, float radius=.25f)
        {
            // Project repeatedly: a concave corner may have more than one nearest segment.
            for(int pass=0;pass<12;pass++)
            {
                float distance=ExactDistance(p,out var inward);
                if(distance>=radius-.001f)return p;
                p+=inward*(radius-distance+.005f);
            }
            return p;
        }

        public float ExactDistance(Vector2 p,out Vector2 inward)
        {
            bool inside=Contains(p); float nearest=float.MaxValue; Vector2 result=p; inward=Vector2.zero;
            for(int i=0;i<boundary.Length;i++)
            {
                var a=boundary[i]; var edge=boundary[(i+1)%boundary.Length]-a;
                var q=a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude);
                float dist=(q-p).sqrMagnitude;
                if(dist<nearest){nearest=dist;result=q;inward=new Vector2(edge.y,-edge.x).normalized;}
            }
            float distance=Mathf.Sqrt(nearest);
            if(distance>.0001f)inward=(inside?p-result:result-p)/distance;
            return inside?distance:-distance;
        }

        void BuildDistanceField()
        {
            distanceSamples=new Color[FieldResolution*FieldResolution];
            for(int y=0;y<FieldResolution;y++)for(int x=0;x<FieldResolution;x++)
            {
                float d=ExactDistance(UVToWorld(new Vector2(x/(float)(FieldResolution-1),y/(float)(FieldResolution-1))),out var n);
                distanceSamples[y*FieldResolution+x]=new Color(d,n.x,n.y,1);
            }
            // This is numerical geometry data, not a resized background image.
            distanceTexture=new Texture2D(FieldResolution,FieldResolution,TextureFormat.RGBAFloat,false,true);
            distanceTexture.name="Stomach signed distance / inward normal";
            distanceTexture.wrapMode=TextureWrapMode.Clamp;distanceTexture.filterMode=FilterMode.Bilinear;
            distanceTexture.SetPixels(distanceSamples);distanceTexture.Apply(false,false);
            material.SetTexture("_DistanceTex",distanceTexture);
            material.SetFloat("_WallWidth",WallWidth);
        }

        public float DistanceToRim(Vector2 p,out Vector2 inward)
        {
            if(!WorldRect.Contains(p))return ExactDistance(p,out inward);
            Vector2 uv=(p-WorldRect.min); uv=new Vector2(uv.x/WorldRect.width,uv.y/WorldRect.height);
            float x=uv.x*(FieldResolution-1),y=uv.y*(FieldResolution-1);
            int ix=Mathf.Clamp(Mathf.FloorToInt(x),0,FieldResolution-2),iy=Mathf.Clamp(Mathf.FloorToInt(y),0,FieldResolution-2);
            int index=iy*FieldResolution+ix;
            var sample=Color.Lerp(Color.Lerp(distanceSamples[index],distanceSamples[index+1],x-ix),
                Color.Lerp(distanceSamples[index+FieldResolution],distanceSamples[index+FieldResolution+1],x-ix),y-iy);
            inward=new Vector2(sample.g,sample.b).normalized;return sample.r;
        }
        public float SlopeProgress(Vector2 p) => Mathf.Clamp01((WallWidth-DistanceToRim(p,out _))/Definition.climbableWidth);
        public static float UphillMultiplier(float progress) => progress<=.5f ? Mathf.Lerp(1,.5f,Mathf.SmoothStep(0,1,progress*2)) : Mathf.Lerp(.5f,.1f,Mathf.SmoothStep(0,1,(progress-.5f)*2));
        public bool AppliesAt(Vector2 p) => StageOne && !MiniStageRuntimeState.IsInsideMiniStage && Vector2.Distance(p,WorldRect.center)<WorldRect.width;
        public static bool BlocksDash(Vector2 p) => Instance!=null && Instance.AppliesAt(p) && Instance.SlopeProgress(p)>.005f;
        public static Vector2 ClampDash(Vector2 from,Vector2 to)
        {
            var map=Instance;if(map==null || !map.AppliesAt(from))return to;
            return map.ClampToField(to,map.WallWidth+.12f);
        }
        public static Vector2 SafeSpawn(Vector2 position,float radius=.5f,bool miniStageOwned=false)
        {
            var map=Instance;
            if(map==null || !map.StageOne || miniStageOwned || MiniStageRuntimeState.IsInsideMiniStage)return position;
            // Room OnInit may run during the portal animation, before the global entered flag.
            if(map.level.IsRunFlowPaused && !map.AppliesAt(position))return position;
            // Interactables and stationary snipers start on the flat floor, not an inaccessible slope.
            return map.ClampToField(position,map.WallWidth+Mathf.Max(.25f,radius));
        }

        Texture2D overviewTexture;
        public Texture OverviewTexture
        {
            get
            {
                if(overviewTexture==null)CaptureOverview();
                return overviewTexture;
            }
        }
        void CaptureOverview()
        {
            var go=new GameObject("Stomach map overview camera"){hideFlags=HideFlags.HideAndDontSave};
            var cam=go.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;
            cam.orthographicSize=WorldRect.height*.5f;cam.aspect=WorldRect.width/WorldRect.height;
            cam.transform.position=new Vector3(WorldRect.center.x,WorldRect.center.y,-20);
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.25f,.06f,.10f);cam.cullingMask=1<<31;
            var target=new RenderTexture(768,512,16);var previous=RenderTexture.active;
            var layers=new int[chunks.Count];
            try
            {
                for(int i=0;i<chunks.Count;i++){layers[i]=chunks[i].layer;chunks[i].layer=31;}
                cam.targetTexture=target;cam.Render();RenderTexture.active=target;
                overviewTexture=new Texture2D(768,512,TextureFormat.RGB24,false);
                overviewTexture.ReadPixels(new Rect(0,0,768,512),0,0);overviewTexture.Apply();
            }
            finally
            {
                for(int i=0;i<chunks.Count;i++)chunks[i].layer=layers[i];
                RenderTexture.active=previous;cam.targetTexture=null;target.Release();Destroy(target);Destroy(go);
            }
        }

        void LateUpdate()
        {
            if(level==null || Definition==null) return;
            var stage=level.GetComponent<StageProgression>();
            bool stageOne=stage==null || stage.StageNumber==1;
            var player=level.PlayerCharacter;
            // Mini-stage rooms are separate islands at the existing anchor, not inside the stomach.
            bool atField=stageOne && player!=null && Vector2.Distance(player.transform.position,WorldRect.center)<WorldRect.width;
            if(FieldVisible!=atField)
            {
                FieldVisible=atField; foreach(var c in chunks)c.SetActive(atField);
            }
            if(legacy!=null)
            {
                legacy.enabled=stageOne?false:legacyEnabled;
                if(legacyRenderer!=null)legacyRenderer.enabled=!stageOne && legacyRendererEnabled;
            }
            if(!atField || player.IsPortalTravelling || MiniStageRuntimeState.IsInsideMiniStage || level.IsRunFlowPaused) return;
            var body=player.GetComponent<Rigidbody2D>();
            Vector2 position=player.transform.position;
            var collider=player.GetComponent<Collider2D>();
            float radius=collider!=null?Mathf.Max(collider.bounds.extents.x,collider.bounds.extents.y):.25f;
            Vector2 clamped=ClampToField(position,ClimbLimit+radius);
            if((position-clamped).sqrMagnitude>.000001f)
            {
                if(body!=null){body.position=clamped;var normal=(clamped-position).normalized;
                    body.velocity-=Mathf.Min(0,Vector2.Dot(body.velocity,normal))*normal;}
                player.transform.position=new Vector3(clamped.x,clamped.y,player.transform.position.z);
            }
        }

        void OnDestroy()
        {
            if(Instance==this)Instance=null;
            if(distanceTexture!=null)Destroy(distanceTexture);
            if(overviewTexture!=null)Destroy(overviewTexture);
            foreach(var c in chunks)if(c!=null)Destroy(c);
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(material!=null)Destroy(material);
            if(legacy!=null){legacy.enabled=legacyEnabled;if(legacyRenderer!=null)legacyRenderer.enabled=legacyRendererEnabled;}
        }
    }
}
