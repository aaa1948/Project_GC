using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    // The same closed sprite and transform exist from descent through landing. Only the cap pivots when opened.
    public sealed class SupplyCapsule : InteractableEventObject
    {
        public const float BodyWidth=3.28f; // Ashi reference width .82 × 4.
        public bool Landed { get; private set; }
        public bool Opened { get; private set; }
        public bool StartedDescent { get; private set; }
        public Transform Visual => bodyRoot;
        public SupplyRelicOrb RewardOrb { get; private set; }
        Transform bodyRoot,rightCap;
        SpriteRenderer closed;
        readonly List<SpriteRenderer> parachutes=new List<SpriteRenderer>();
        readonly List<Mesh> meshes=new List<Mesh>();
        Material splitMaterial;
        float fallAge,fallDuration,landAge;
        MapMarker marker;
        protected override bool KeepVisibleAfterInteraction=>true;
        protected override bool InteractionAvailable=>Landed&&!Opened;
        protected override Component PromptOwner=>closed!=null?(Component)closed:this;
        bool Paused=>Time.timeScale<=0||levelManager==null||levelManager.IsRunFlowPaused||levelManager.IsLevelEnded||MiniStageRuntimeState.IsInsideMiniStage;
        public static SupplyCapsule Create(Vector2 position,LevelManager level)
        {
            var go=new GameObject("보급 캡슐");go.transform.position=position;
            var hit=go.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=new Vector2(BodyWidth+1.5f,2.6f);hit.enabled=false;
            var capsule=go.AddComponent<SupplyCapsule>();capsule.levelManager=level;capsule.Build();return capsule;
        }
        void Build()
        {
            bodyRoot=new GameObject("Capsule body").transform;bodyRoot.SetParent(transform,false);
            closed=new GameObject("Capsule shell").AddComponent<SpriteRenderer>();closed.transform.SetParent(bodyRoot,false);
            closed.sprite=SupplyCapsuleArt.Closed;closed.sortingLayerName="Monster Full";closed.sortingOrder=20;
            float scale=BodyWidth/Mathf.Max(.01f,closed.sprite.bounds.size.x);bodyRoot.localScale=Vector3.one*scale;
            for(int i=0;i<2;i++)
            {
                var chute=new GameObject("Small parachute "+i).AddComponent<SpriteRenderer>();chute.transform.SetParent(bodyRoot,false);
                chute.sprite=SupplyCapsuleArt.Parachute;chute.sortingLayerName="Monster Full";chute.sortingOrder=21;
                // Sprite pivot is at the cord convergence, fixed to the rounded-end shoulder.
                chute.transform.localPosition=new Vector3((i==0?-.29f:.29f)*closed.sprite.bounds.size.x,closed.sprite.bounds.size.y*.29f,0);
                chute.transform.localScale=Vector3.one*(closed.sprite.bounds.size.x*.36f/chute.sprite.bounds.size.x);
                parachutes.Add(chute);
            }
            bodyRoot.gameObject.SetActive(false);
            marker=gameObject.AddComponent<MapMarker>();marker.Configure(MapMarkerKind.Event,"보급 캡슐",true);marker.enabled=false;
        }
        public void BeginDescent(float seconds)
        {
            if(StartedDescent)return;StartedDescent=true;fallDuration=Mathf.Max(.1f,seconds);
            bodyRoot.gameObject.SetActive(true);bodyRoot.localPosition=Vector3.up*7;
        }
        protected override void Update()
        {
            if(Paused){SetPromptVisible(false);return;}
            if(StartedDescent&&!Landed)
            {
                fallAge+=Time.deltaTime;float t=Mathf.Clamp01(fallAge/fallDuration);
                bodyRoot.localPosition=new Vector3(Mathf.Sin(t*Mathf.PI*4)*.13f*(1-t),7*(1-Mathf.SmoothStep(0,1,t)),0);
                // Director owns the exact ten-second landing and notification boundary.
            }
            if(Landed&&landAge<1.1f)
            {
                landAge+=Time.deltaTime;float a=Mathf.Clamp01(landAge/.9f);
                foreach(var chute in parachutes)
                {
                    chute.color=new Color(1,1,1,1-a);
                    if(a>=1)chute.enabled=false;
                    chute.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(a*4)*12);
                }
            }
            base.Update();
        }
        public void Land()
        {
            if(Landed)return;if(!StartedDescent)BeginDescent(.1f);
            Landed=true;bodyRoot.localPosition=Vector3.zero;
            GetComponent<Collider2D>().enabled=true;marker.enabled=true;
            StartCoroutine(LandingSplash());
        }
        IEnumerator LandingSplash()
        {
            var drops=new List<SpriteRenderer>();
            for(int i=0;i<10;i++)
            {
                var drop=new GameObject("Pink landing droplet").AddComponent<SpriteRenderer>();drop.transform.SetParent(transform,false);
                drop.sprite=SupplyCapsuleArt.Droplet;drop.color=new Color(1,.48f,.65f);drop.sortingLayerName="Monster Full";drop.sortingOrder=22;drop.transform.localScale=new Vector3(.09f,.14f,1);drops.Add(drop);
            }
            float age=0;
            while(age<.6f)
            {
                yield return null;if(Paused)continue;age+=Time.deltaTime;float t=Mathf.Clamp01(age/.6f);
                for(int i=0;i<drops.Count;i++)
                {
                    float side=i%2==0?-1:1;float spread=.15f+(i/2)*.15f;
                    drops[i].transform.localPosition=new Vector3(side*(1.1f+spread*t),-.7f+Mathf.Sin(t*Mathf.PI)*(.2f+spread),0);
                    drops[i].color=new Color(1,.48f,.65f,1-t);
                }
            }
            foreach(var drop in drops)Destroy(drop.gameObject);
        }
        protected override bool ExecuteInteraction(Character player)
        {
            if(!Landed||Opened||Paused)return false;
            Opened=true;StartCoroutine(OpenRoutine(player));return true;
        }
        IEnumerator OpenRoutine(Character player)
        {
            MakeHingedCap();closed.enabled=false;
            float age=0;
            while(age<.65f)
            {
                yield return null;if(Paused)continue;age+=Time.deltaTime;
                rightCap.localRotation=Quaternion.Euler(0,0,-48*Mathf.SmoothStep(0,1,age/.65f));
            }
            RewardOrb=SupplyRelicOrb.Create(transform.position+new Vector3(.12f,.2f,0),player,levelManager,()=>marker.enabled=false);
        }
        void MakeHingedCap()
        {
            var sprite=closed.sprite;var bounds=sprite.bounds;var size=bounds.size;
            // The painted seam slopes up-left. Clip two polygons on that seam without repainting the original.
            Vector2 bottom=new Vector2(.59f,0),top=new Vector2(.49f,1);
            Vector2 pivot=Vector2.Lerp(bottom,top,.20f);
            splitMaterial=new Material(Shader.Find("Sprites/Default"));splitMaterial.mainTexture=sprite.texture;
            MakePiece("Ivory half",new[]{Vector2.zero,bottom,top,Vector2.up},Vector2.zero,size,sprite);
            rightCap=MakePiece("Hinged mint half",new[]{bottom,Vector2.right,Vector2.one,top},pivot,size,sprite);
        }
        Transform MakePiece(string name,Vector2[] points,Vector2 pivot,Vector2 size,Sprite sprite)
        {
            var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;piece.SetParent(bodyRoot,false);
            piece.localPosition=new Vector3(pivot.x*size.x,pivot.y*size.y,0);
            var verts=new Vector3[4];var uv=new Vector2[4];
            for(int i=0;i<4;i++)
            {
                verts[i]=new Vector3((points[i].x-.5f-pivot.x)*size.x,(points[i].y-.5f-pivot.y)*size.y,0);
                uv[i]=new Vector2((sprite.rect.x+points[i].x*sprite.rect.width)/sprite.texture.width,(sprite.rect.y+points[i].y*sprite.rect.height)/sprite.texture.height);
            }
            // Both polygons retain the exact closed-sprite pixel positions before rotation.
            if(pivot!=Vector2.zero){piece.localPosition-=new Vector3(size.x*.5f,size.y*.5f,0);for(int i=0;i<4;i++)verts[i]+=new Vector3(size.x*.5f,size.y*.5f,0);}
            var mesh=new Mesh{name=name};mesh.vertices=verts;mesh.uv=uv;mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();meshes.Add(mesh);
            piece.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=piece.GetComponent<MeshRenderer>();renderer.sharedMaterial=splitMaterial;renderer.sortingLayerName="Monster Full";renderer.sortingOrder=20;return piece;
        }
        void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);if(splitMaterial!=null)Destroy(splitMaterial);}
    }
}
