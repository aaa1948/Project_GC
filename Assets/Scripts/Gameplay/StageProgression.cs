using UnityEngine;
namespace Vampire
{
    [DisallowMultipleComponent]
    public sealed class StageProgression:MonoBehaviour
    {
        public static bool StageTwoEnabled=>false;
        public int StageNumber=>1;
        public bool FirstBossDefeated {get;private set;}
        public bool Travelling {get;private set;}
        public StageExitPortal Portal {get;private set;}
        LevelManager level;StageExitDefinition definition;
        public static StageProgression Ensure(LevelManager level){var result=level.GetComponent<StageProgression>()??level.gameObject.AddComponent<StageProgression>();result.level=level;return result;}
        void Awake(){level=GetComponent<LevelManager>();definition=Resources.Load<StageExitDefinition>("StageExitDefinition");}
        public bool FinalBossDefeated(Vector3 position,float measuredDps)
        {
            if(FirstBossDefeated||level==null||level.IsLevelEnded)return true;
            if(definition==null||definition.crater==null)return false;
            FirstBossDefeated=true;level.NotifyExternalFinalBossSpawned();
            for(int i=0;i<3;i++){float angle=(210+i*60)*Mathf.Deg2Rad;level.EntityManager.SpawnChest(definition.rewardChest??level.CurrentLevelBlueprint.chestBlueprint,(Vector2)position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*3.2f);}
            var go=new GameObject("스테이지 1 탈출구");go.transform.position=position;
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=definition.crater;GroundVisualSorting.ApplyBackground(renderer,-10);
            go.AddComponent<CircleCollider2D>().radius=2.6f;Portal=go.AddComponent<StageExitPortal>();Portal.Configure(this);
            go.AddComponent<MapMarker>().Configure(MapMarkerKind.Event,"탈출구 · 탐험 완료");GameAudioManager.FinishStageBossAudio();return true;
        }
        public bool CanEnter(Character p)=>FirstBossDefeated&&!Travelling&&level!=null&&!level.IsLevelEnded&&!level.IsRunFlowPaused&&!MiniStageRuntimeState.IsInsideMiniStage&&BloodClotTravel.CanTravel(p)&&Time.timeScale>0;
        public bool TryEnter(Character player,StageExitPortal portal)
        {
            if(portal==null||portal!=Portal||AcidLakeEvent.Contains(portal.transform.position)||!CanEnter(player))return false;
            Travelling=true;level.LevelPassed(null);return true;
        }
    }
}
