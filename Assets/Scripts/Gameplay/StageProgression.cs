using System.Collections;
using UnityEngine;
namespace Vampire
{
    [DisallowMultipleComponent]
    public sealed class StageProgression:MonoBehaviour
    {
        public static bool StageTwoEnabled=>true;
        public int StageNumber {get;private set;}=1;
        public bool FirstBossDefeated {get;private set;}
        public bool Travelling {get;private set;}
        public bool SuitEquipped {get;private set;}
        public bool AtOutlet {get;private set;}
        public StageExitPortal Portal {get;private set;}
        public BossRemnantPickup Remnant {get;private set;}
        public FoodBossKind DefeatedKind {get;private set;}
        public string TransitionPhase {get;private set;}="field";
        LevelManager level;StageExitDefinition definition;Transform escapeHole;
        BossDivingSuit suit;bool entering;LevelBlueprint stageTwo;
        string altarSettings;Sprite altarSprite;Material altarMaterial;Vector3 altarScale;int altarLayer,altarOrder;
        public static StageProgression Ensure(LevelManager owner){var p=owner.GetComponent<StageProgression>()??owner.gameObject.AddComponent<StageProgression>();p.level=owner;return p;}
        void Awake()
        {
            level=GetComponent<LevelManager>();definition=Resources.Load<StageExitDefinition>("StageExitDefinition");
            var altar=FindObjectOfType<FinalBossSummonInteractable>();
            if(altar!=null)
            {
                altarSettings=JsonUtility.ToJson(altar);altarScale=altar.transform.localScale;
                var sr=altar.GetComponent<SpriteRenderer>();if(sr!=null){altarSprite=sr.sprite;altarMaterial=sr.sharedMaterial;altarLayer=sr.sortingLayerID;altarOrder=sr.sortingOrder;}
            }
        }
        void RestoreBossAltar(FiniteStomachMap map)
        {
            if(FindObjectOfType<FinalBossSummonInteractable>()!=null)return;
            if(altarSettings==null)throw new System.InvalidOperationException("Missing original boss altar settings");
            var go=new GameObject("대장 보스 소환 제단");go.transform.position=FiniteStomachMap.SafeSpawn(map.UVToWorld(new Vector2(.73f,.3f)),2);go.transform.localScale=altarScale;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=altarSprite;sr.sharedMaterial=altarMaterial;sr.sortingLayerID=altarLayer;sr.sortingOrder=altarOrder;
            go.AddComponent<CircleCollider2D>();var altar=go.AddComponent<FinalBossSummonInteractable>();JsonUtility.FromJsonOverwrite(altarSettings,altar);altar.ResetForNextStage();
        }
        public Transform BeginEscape(FoodBossKind kind,Vector3 position)
        {
            DefeatedKind=kind;if(escapeHole!=null)return escapeHole;
            var go=new GameObject("보스가 뚫은 추격 구멍");go.transform.position=position;escapeHole=go.transform;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=BossTravelArt.Hole??definition.crater;
            go.transform.localScale=Vector3.one*(4.8f/sr.sprite.bounds.size.x)*.001f;
            GroundVisualSorting.ApplyBackground(sr,-10);return escapeHole;
        }
        public bool FinalBossDefeated(Vector3 position,float measuredDps,FoodBossKind kind=FoodBossKind.Rolpangi)
        {
            if(FirstBossDefeated||level==null||level.IsLevelEnded)return true;
            if(definition==null||definition.crater==null)return false;
            FirstBossDefeated=true;DefeatedKind=kind;level.NotifyExternalFinalBossSpawned();
            for(int i=0;i<3;i++){float a=(210+i*60)*Mathf.Deg2Rad;var p=FiniteStomachMap.SafeSpawn((Vector2)position+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3.2f,.8f);level.EntityManager.SpawnChest(definition.rewardChest??level.CurrentLevelBlueprint.chestBlueprint,p);}
            var hole=escapeHole??BeginEscape(kind,position);var sr=hole.GetComponent<SpriteRenderer>();hole.localScale=Vector3.one*(4.8f/sr.sprite.bounds.size.x);
            hole.gameObject.AddComponent<CircleCollider2D>().radius=.9f;Portal=hole.gameObject.AddComponent<StageExitPortal>();Portal.Configure(this);
            hole.gameObject.AddComponent<MapMarker>().Configure(MapMarkerKind.Event,StageNumber==1?"보스 추격 구멍 · 스페셜 스테이지 준비 중":"탈출구 · 탐험 완료");
            if(StageNumber==1)Remnant=BossRemnantPickup.Create(this,kind,FiniteStomachMap.SafeSpawn((Vector2)position+Vector2.right*3.5f,.6f));
            GameAudioManager.FinishStageBossAudio();return true;
        }
        public bool CanEnter(Character p)=>FirstBossDefeated&&!Travelling&&level!=null&&!level.IsLevelEnded&&!level.IsRunFlowPaused&&!MiniStageRuntimeState.IsInsideMiniStage&&BloodClotTravel.CanTravel(p)&&Time.timeScale>0;
        public bool TryEnter(Character player,StageExitPortal portal)
        {
            if(StageNumber==1||portal==null||portal!=Portal||AcidLakeEvent.Contains(portal.transform.position)||!CanEnter(player))return false;
            Travelling=true;level.LevelPassed(null);return true;
        }
        public bool TryEquip(Character player,BossRemnantPickup pickup)
        {
            if(pickup!=Remnant||StageNumber!=1||SuitEquipped||!CanEnter(player))return false;
            Travelling=true;SuitEquipped=true;level.SetRunFlowPaused(true);level.EntityManager.SetFieldMonsterRuntimeSuspended(true);StartCoroutine(EquipAndSwim(player,pickup));return true;
        }
        IEnumerator EquipAndSwim(Character player,BossRemnantPickup pickup)
        {
            TransitionPhase="equip";var travel=BloodClotTravel.Ensure(player);travel.Begin();suit=BossDivingSuit.Attach(player,DefeatedKind);
            Vector3 start=pickup.transform.position;pickup.GetComponent<Collider2D>().enabled=false;pickup.GetComponent<SpriteRenderer>().enabled=false;
            for(float t=0;t<.65f;t+=Time.deltaTime){travel.SwimPose(player.transform.position,0);suit.Assemble(start,Mathf.SmoothStep(0,1,t/.65f));yield return null;}
            Destroy(pickup.gameObject);Remnant=null;var outlet=StomachOpening.Find(false);outlet.SetOpened(0);
            Vector3 from=player.transform.position,to=(Vector3)(outlet.FrontLip+outlet.Inward*.9f);TransitionPhase="swim";
            for(float t=0;t<4.5f;t+=Time.deltaTime){travel.SwimPose(Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t/4.5f)),t);suit.Kick(t,false);yield return null;}
            travel.SwimPose(to,0);TransitionPhase="paddle";
            for(float t=0;t<1.25f;t+=Time.deltaTime){travel.SwimPose(to,t);suit.Kick(t,true);outlet.SetOpened(Mathf.SmoothStep(0,1,t/1.1f));yield return null;}
            outlet.SetOpened(1);travel.HoldForInteraction();AtOutlet=true;TransitionPhase="outlet-ready";
            // Keep the travel lock / acid protection; only the reserved outlet can be interacted with.
            level.SetRunFlowPaused(false);level.EntityManager.SetFieldMonsterRuntimeSuspended(false);
        }
        public bool CanUseOutlet(StomachOpening o)=>o!=null&&!o.Upper&&StageNumber==1&&SuitEquipped&&AtOutlet&&!entering&&!level.IsLevelEnded;
        public bool TryUseOutlet(Character player,StomachOpening o)
        {if(!CanUseOutlet(o)||player!=level.PlayerCharacter)return false;entering=true;AtOutlet=false;level.SetRunFlowPaused(true);level.EntityManager.SetFieldMonsterRuntimeSuspended(true);StartCoroutine(EnterColon(player,o));return true;}
        IEnumerator EnterColon(Character player,StomachOpening outlet)
        {
            TransitionPhase="dive";var travel=BloodClotTravel.Ensure(player);var anchor=new GameObject("Outlet travel anchor").transform;anchor.position=outlet.Center;
            yield return travel.Dive(anchor);Destroy(anchor.gameObject);if(suit!=null){Destroy(suit.gameObject);suit=null;}
            TransitionPhase="loading";var loading=StageEntryLoading.CreateStageTransfer(player.Blueprint);yield return null;yield return null;
            foreach(var chest in FindObjectsOfType<Chest>())level.EntityManager.DespawnChest(chest);
            foreach(var p in FindObjectsOfType<Collectable>())p.DissolveInAcidLake();
            foreach(var p in FindObjectsOfType<SupplyRelicOrb>())Destroy(p.gameObject);
            foreach(var p in FindObjectsOfType<SupplyCapsule>())Destroy(p.gameObject);
            if(Portal!=null){Portal.gameObject.SetActive(false);Destroy(Portal.gameObject);}Portal=null;escapeHole=null;
            StageNumber=2;FirstBossDefeated=false;SuitEquipped=false;
            var map=FiniteStomachMap.Instance;var nextMap=Resources.Load<StomachMapDefinition>("ColonMap/Stage2");
            if(nextMap==null)throw new System.InvalidOperationException("ColonMap/Stage2 is missing");
            map.ChangeDefinition(nextMap);loading.SetProgress(.4f);yield return null;
            RestoreBossAltar(map);
            stageTwo=Instantiate(level.CurrentLevelBlueprint);stageTwo.name="Stage 2 · 대장 · Stage 1 spawn table";
            level.BeginNextStage(stageTwo);level.SetRunFlowPaused(true);level.EntityManager.SetFieldMonsterRuntimeSuspended(true);level.GetComponent<SupplyCapsuleDirector>()?.RestartForStage();
            foreach(var altar in FindObjectsOfType<FinalBossSummonInteractable>())altar.transform.position=FiniteStomachMap.SafeSpawn(altar.transform.position,2);
            var inlet=StomachOpening.Find(true);inlet.SetOpened(0);var landing=map.ClampToField(inlet.FrontLip+Vector2.down*2,map.WallWidth+.6f);
            travel.SwimPose(landing,0);travel.Visual.color=Color.clear;loading.SetProgress(.7f);
            yield return level.GetComponent<FieldObjectLayout>().RebuildForStage();
            if(!level.InitialFieldObjectsReady)throw new System.InvalidOperationException("Stage 2 field placement failed");
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){loading.SetProgress(Mathf.Lerp(.7f,1,t/.7f));yield return null;}
            var shot=StageTravelCamera.Begin(inlet,landing);
            loading.CompleteStageTransfer();yield return null;TransitionPhase="opening";
            for(float t=0;t<1.2f;t+=Time.deltaTime){inlet.SetOpened(t/1.2f);yield return null;}inlet.SetOpened(1);
            anchor=new GameObject("Colon inlet ejection anchor").transform;anchor.position=inlet.Center;
            TransitionPhase="eject";yield return travel.Eject(anchor,landing);Destroy(anchor.gameObject);
            travel.HoldForInteraction();if(shot!=null)shot.Release();TransitionPhase="settle";yield return new WaitForSeconds(1);
            travel.ReleasePose();Travelling=false;entering=false;TransitionPhase="field";level.EntityManager.SetFieldMonsterRuntimeSuspended(false);level.SetRunFlowPaused(false);GameAudioManager.ExitMiniStageAudio();
        }
        void OnDestroy(){if(stageTwo!=null)Destroy(stageTwo);}
    }
}
