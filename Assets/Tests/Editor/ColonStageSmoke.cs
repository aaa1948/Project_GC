using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Vampire.Tests.Editor
{
    [InitializeOnLoad]
    public static class ColonStageSmoke
    {
        const string Key="ColonStageSmoke",Output="D:/Project_GC/work/colon-proof-release";
        static bool started;static int checks;static double deadline;
        static string pendingGroup;static int pendingFrame;
        [Serializable]class Entry{public string id;}[Serializable]class Catalog{public Entry[] entries;}
        [Serializable]class Pref{public string key;public bool had;public int value;}[Serializable]class Backup{public Pref[] prefs;}
        static ColonStageSmoke(){if(SessionState.GetBool(Key,false))Attach();}
        public static void Run(){SessionState.SetBool(Key+"NoImages",false);Start();}
        public static void RunChecksOnly(){SessionState.SetBool(Key+"NoImages",true);Start();}
        static void Start()
        {
            Directory.CreateDirectory(Output);Restore();File.WriteAllText(Output+"/checks.txt","");File.WriteAllText(Output+"/errors.txt","");
            Vampire.Editor.ColonStageInstaller.Install();
            var entries=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("TutorialClips/catalog").text).entries;
            var keys=entries.Select(e=>TutorialGuide.SeenKey(e.id)).ToArray();
            File.WriteAllText(Output+"/prefs.json",JsonUtility.ToJson(new Backup{prefs=keys.Select(k=>new Pref{key=k,had=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray()}));
            foreach(var k in keys)PlayerPrefs.SetInt(k,1);
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Done",false);SessionState.SetBool(Key+"Failed",false);
            CrossSceneData.CharacterBlueprint=AssetDatabase.LoadAssetAtPath<CharacterBlueprint>("Assets/Blueprints/Characters/Main Character Blueprint.asset");
            EditorSceneManager.OpenScene("Assets/Scenes/Game/Level 1.unity");
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).position=new Rect(0,0,1280,741);
            Attach();EditorApplication.EnterPlaymode();
        }
        static void Attach(){started=false;deadline=EditorApplication.timeSinceStartup+900;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
        static void Restore(){if(!File.Exists(Output+"/prefs.json"))return;foreach(var p in JsonUtility.FromJson<Backup>(File.ReadAllText(Output+"/prefs.json")).prefs){if(p.had)PlayerPrefs.SetInt(p.key,p.value);else PlayerPrefs.DeleteKey(p.key);}PlayerPrefs.Save();File.Delete(Output+"/prefs.json");}
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){File.AppendAllText(Output+"/errors.txt",text+"\n"+stack+"\n");SessionState.SetBool(Key+"Failed",true);}}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;
            if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool(Key+"Failed",true);File.AppendAllText(Output+"/errors.txt","Deadline\n");}
            if(SessionState.GetBool(Key+"Done",false)||SessionState.GetBool(Key+"Failed",false))
            {
                if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                Restore();Time.captureDeltaTime=0;SessionState.SetBool(Key,false);bool failed=SessionState.GetBool(Key+"Failed",false);File.WriteAllText(Output+"/result.txt",(failed?"FAIL":"PASS")+" checks="+checks);EditorApplication.Exit(failed?1:0);return;
            }
            var level=Object.FindObjectOfType<LevelManager>();if(started||!EditorApplication.isPlaying||level==null||!level.InitialFieldObjectsReady||level.CurrentLevelTime<.1f)return;
            started=true;level.StartCoroutine(Checks(level));
        }
        static FieldInfo Field(object o,string n){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(f!=null)return f;}throw new Exception(n);}
        static void Set(object o,string n,object v)=>Field(o,n).SetValue(o,v);
        static void Check(bool ok,string text){if(!ok)throw new Exception("[Colon] FAIL "+text);checks++;Debug.Log("[Colon] PASS "+text);File.AppendAllText(Output+"/checks.txt",text+"\n");}
        static void Position(Character p,Vector2 at){p.transform.position=at;var rb=p.GetComponent<Rigidbody2D>();rb.position=at;rb.velocity=Vector2.zero;Physics2D.SyncTransforms();}
        static void Capture(string group,int index){pendingGroup=group;pendingFrame=index;}
        public static void RenderPending(){if(pendingGroup==null)return;var g=pendingGroup;pendingGroup=null;CaptureNow(g,pendingFrame);}
        static void CaptureNow(string group,int index)
        {
            if(group=="swim"&&index==30)
            {
                var p=Object.FindObjectOfType<LevelManager>().PlayerCharacter;var travel=BloodClotTravel.Ensure(p);var suit=Object.FindObjectOfType<BossDivingSuit>();
                Check(suit!=null&&Vector2.Distance(suit.transform.position,travel.Visual.transform.position)<travel.ReferenceHeight*.8f,"costume stays attached after actual late-frame motion");
            }
            if(group=="stage2")
            {
                var p=Object.FindObjectOfType<LevelManager>().PlayerCharacter;var point=Camera.main.WorldToViewportPoint(p.transform.position);
                Check(point.x>.1f&&point.x<.9f&&point.y>.1f&&point.y<.9f,"camera returns to player after establishing shot");
                Check(!p.GetComponentInChildren<SpriteAnimator>().GetComponent<SpriteRenderer>().forceRenderingOff,"normal character renderer restored");
            }
            if(SessionState.GetBool(Key+"NoImages",false))return;
            string folder=Output+"/"+group;Directory.CreateDirectory(folder);
            var camera=Camera.main;var overlays=Object.FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();var orders=overlays.Select(c=>c.sortingOrder).ToArray();var overrides=overlays.Select(c=>c.overrideSorting).ToArray();
            var oldTarget=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24);Texture2D pixels=null;
            try
            {
                camera.targetTexture=rt;foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;c.overrideSorting=true;c.sortingOrder=Mathf.Clamp(32100+c.sortingOrder,32100,32760);}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(folder+"/"+index.ToString("D4")+".png",pixels.EncodeToPNG());
            }
            finally{for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];overlays[i].sortingOrder=orders[i];overlays[i].overrideSorting=overrides[i];}camera.targetTexture=oldTarget;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(pixels!=null)Object.Destroy(pixels);Canvas.ForceUpdateCanvases();}
        }
        static IEnumerator Checks(LevelManager level)
        {
            var capture=level.gameObject.AddComponent<StageCaptureProbe>();Check(capture!=null,"late-frame capture probe installed");capture.AfterLateUpdate=RenderPending;
            var prefs=GamePreferences.Current.Copy();prefs.pauseOnFocusLoss=false;prefs.muteOnFocusLoss=false;GamePreferences.Apply(prefs,false);Application.runInBackground=true;
            var tutorial=Object.FindObjectOfType<TutorialGuide>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}Time.timeScale=1;RunScore.Exclude("대장 맵 전환 검증");
            foreach(var b in Object.FindObjectsOfType<MonoBehaviour>())if(b is StageEventDirector||b is TimedSpecialMonsterSpawner||b is AcidToadSpawn||b is NPCSpawner){b.enabled=false;b.StopAllCoroutines();}
            level.enabled=false;var player=level.PlayerCharacter;Set(player,"isInvincible",true);player.CollectableCollider.enabled=false;
            foreach(var ability in Object.FindObjectsOfType<Ability>()){ability.enabled=false;ability.StopAllCoroutines();}
            foreach(var m in level.EntityManager.LivingMonsters.ToArray())m.DespawnForStageTransfer();
            var map=FiniteStomachMap.Instance;var originalRect=map.WorldRect;var originalBlueprint=level.CurrentLevelBlueprint;
            var progression=StageProgression.Ensure(level);var originalPlayer=player;var inventory=level.PlayerInventory;var skills=player.Skills;
            var bomb=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chest/Bomb.prefab")).GetComponent<Collectable>();bomb.gameObject.SetActive(false);
            var bombSlot=inventory.Slots.First(s=>s.IsBomb);bombSlot.AddItem(bomb);int bombCount=bombSlot.Count;
            player.AddDamageMultiplier(.23f);float damage=player.DamageMultiplier;player.AddReviveCount(1);int revives=player.ReviveCount;float health=player.CurrentHealth;
            Vector2 origin=map.UVToWorld(new Vector2(.53f,.47f));Time.captureDeltaTime=1f/15;
            foreach(FoodBossKind kind in Enum.GetValues(typeof(FoodBossKind)))
            {
                Position(player,origin+Vector2.right*2.4f+Vector2.up*.9f);yield return new WaitForSeconds(.3f);
                var boss=FoodBossRuntime.Spawn(kind,player,origin);boss.AutoPatterns=false;yield return null;
                boss.TakeDamage(boss.MaxHealth+1);Check(boss.Dead,"HP zero starts escape "+kind);
                int frame=0;for(float t=0;t<3.2f;t+=Time.deltaTime){Capture("escape-"+(int)kind,frame++);yield return null;}
                Check(progression.Portal!=null&&progression.Remnant!=null,"hole and matching remnant "+kind);
                Check(Mathf.Abs(progression.Remnant.GetComponent<SpriteRenderer>().bounds.size.y-BossTravelArt.AshiHeight)<.01f,"Ashi-sized remnant "+kind);
                Check(!level.IsLevelEnded&&!progression.TryEnter(player,progression.Portal),"stage one pursuit hole does not finish run "+kind);
                // Isolated death presentations use the real path; reset only test bookkeeping between cases.
                if(kind!=FoodBossKind.Hurumi)
                {
                    Object.Destroy(progression.Portal.gameObject);Object.Destroy(progression.Remnant.gameObject);
                    Set(progression,"<FirstBossDefeated>k__BackingField",false);Set(progression,"escapeHole",null);
                    foreach(var chest in Object.FindObjectsOfType<Chest>())level.EntityManager.DespawnChest(chest);yield return null;
                }
            }
            var pickup=progression.Remnant;Position(player,(Vector2)pickup.transform.position+Vector2.down*.25f);yield return new WaitForSeconds(.25f);
            InteractionFocus.SelectTouched(pickup);pickup.TryInteract();yield return null;Check(progression.SuitEquipped,"real focused remnant interaction equips suit");
            int equip=0,swim=0;float deadline=Time.time+10;
            while(!progression.AtOutlet&&Time.time<deadline){Capture(progression.TransitionPhase=="equip"?"equip":"swim",progression.TransitionPhase=="equip"?equip++:swim++);yield return null;}
            Check(progression.AtOutlet&&player.IsPortalDamageProtected,"arrival waits protected at lower outlet");
            var outlet=StomachOpening.Find(false);Check(outlet.IsOpen&&progression.CanUseOutlet(outlet),"paddling opens outlet for interaction");
            Set(level,"levelTime",1100f);var lake=level.GetComponent<AcidLakeEvent>();yield return new WaitForSeconds(4.3f);
            Check(player.IsAlive&&Mathf.Abs(player.CurrentHealth-health)<.01f,"suit blocks acid damage AND erosion while waiting");
            yield return new WaitForSeconds(.15f);InteractionFocus.SelectTouched(outlet);outlet.TryInteract();yield return null;
            Check(!progression.AtOutlet&&progression.Travelling,"real outlet interaction begins stage transfer");
            int dive=0,loading=0,intro=0;deadline=Time.time+30;bool sawClosed=false,sawPaused=false;
            while(progression.Travelling&&Time.time<deadline)
            {
                string phase=progression.TransitionPhase;
                if(phase=="dive")Capture("dive",dive++);
                else if(phase=="loading")Capture("loading",loading++);
                else{Capture("intro",intro++);sawPaused|=level.IsRunFlowPaused&&level.CurrentLevelTime==0;sawClosed|=StomachOpening.Find(true)!=null&&StomachOpening.Find(true).Openness<.1f;}
                yield return null;
            }
            Check(progression.StageNumber==2&&!progression.Travelling,"colon stage opens and completes entry");
            Check(sawClosed&&sawPaused,"closed inlet and stopped stage clock during opening");
            Check(player==originalPlayer&&inventory==level.PlayerInventory&&skills==player.Skills,"same player inventory and skill objects retained");
            Check(player.DamageMultiplier==damage&&player.ReviveCount==revives,"combat upgrades and revival count retained");
            Check(bombSlot.Count==bombCount&&bomb!=null&&!bomb.gameObject.activeSelf,"stored consumable object and count retained");
            Check(map.IsColon&&map.WorldRect==originalRect&&map.Definition.pixelWorldSize==Resources.Load<StomachMapDefinition>("StomachMap/Stage1").pixelWorldSize,"same world bounds and pixel size");
            Check(level.CurrentLevelBlueprint.monsters.Length==originalBlueprint.monsters.Length&&level.CurrentLevelBlueprint.levelTime==originalBlueprint.levelTime,"same spawn pools and duration");
            for(int i=0;i<originalBlueprint.monsters.Length;i++)Check(level.CurrentLevelBlueprint.monsters[i].monstersPrefab==originalBlueprint.monsters[i].monstersPrefab,"same monster prefab "+i);
            Check(level.InitialFieldObjectsReady&&level.GetComponent<FieldObjectLayout>().SpawnedObjects.Count==86,"stage two initial layout completes before play");
            Check(!lake.Active&&!AcidLakeEvent.Contains(player.transform.position),"stage-one acid stops in colon");
            Check(Object.FindObjectOfType<FinalBossSummonInteractable>()!=null,"stage-two boss altar survives or restores after stage-one flood");
            Check(!player.IsPortalTravelling&&!player.IsPortalPoseHeld&&player.GetComponent<Rigidbody2D>().simulated,"input and physics released after one-second settle");
            yield return new WaitForSeconds(.3f);
            Capture("stage2",0);
            // Capture the actual full terrain material without changing the gameplay camera.
            var full=(RenderTexture)map.OverviewTexture;var active=RenderTexture.active;RenderTexture.active=full;var tex=new Texture2D(full.width,full.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,full.width,full.height),0,0);tex.Apply();File.WriteAllBytes(Output+"/map.png",tex.EncodeToPNG());Object.Destroy(tex);RenderTexture.active=active;
            Check(map.AppliesAt(player.transform.position)&&map.NavigationDistance(player.transform.position,out _)>=map.WallWidth,"spawn lands on reachable flat floor");
            var ridge=map.UVToWorld(new Vector2(795f/1536,1-360f/1024));Check(!map.Contains(ridge),"approved central ridge is solid");
            var from=map.UVToWorld(new Vector2(.5f,.74f));var to=map.UVToWorld(new Vector2(.5f,.50f));
            var dash=FiniteStomachMap.ClampDash(from,to);Check((dash-to).sqrMagnitude>1,"dash cannot cross internal ridge");
            var direction=ColonNavigation.Toward(from,to);Check(Mathf.Abs(direction.x)>.1f,"horde steers around ridge rather than through it");
            Check(FiniteStomachMap.UphillMultiplier(.5f)==.5f&&Mathf.Approximately(FiniteStomachMap.UphillMultiplier(1),.1f),"50 to 90 percent uphill slowdown retained");
            level.enabled=true;float clock=level.CurrentLevelTime;yield return new WaitForSeconds(2);Check(level.CurrentLevelTime>clock+1.5f,"stage two clock runs after entry");
            Check(level.NormalMonstersSpawned>0,"stage two normal monsters actually spawn");
            level.enabled=false;Vector2 ramp=Vector2.zero;bool found=false;
            for(int y=5;y<95&&!found;y++)for(int x=5;x<95;x++)
            {var p=map.UVToWorld(new Vector2(x/100f,y/100f));float d=map.NavigationDistance(p,out _);if(d>3.8f&&d<4.2f){ramp=p;found=true;break;}}
            Check(found,"reachable slope sample exists");Position(player,ramp);map.NavigationDistance(ramp,out var inward);
            Check(player.GetComponent<StomachSlopeBody>().MovementMultiplier(-inward)<.5f,"actual player uphill multiplier reduced on colon wall");
            player.Move(-inward);Check(player.TryDash(),"actual dash remains usable on colon slope");yield return new WaitForSeconds(.7f);player.Move(Vector2.zero);Position(player,ramp);
            float beforeSlide=map.NavigationDistance(player.transform.position,out _);yield return new WaitForSeconds(1);
            Check(map.NavigationDistance(player.transform.position,out _)>beforeSlide+.02f,"actual player slides down colon wall after release");
            Position(player,map.ClampToField(map.UVToWorld(new Vector2(.75f,.3f)),10));
            foreach(var key in new[]{"Ashi","Ari","Hyuki","Shini"})Check(BloodClotTravel.Frames(key).Length==(key=="Hyuki"?5:16),"character-specific mini-stage exit art available "+key);
            var final=level.EntityManager.SpawnFinalBoss(level.CurrentLevelBlueprint,(Vector2)player.transform.position+Vector2.right*3,0,FoodBossKind.Maribi).GetComponent<FoodBossRuntime>();
            final.AutoPatterns=false;final.TakeDamage(final.MaxHealth+1);yield return new WaitForSeconds(3);
            Check(progression.Portal!=null&&progression.Remnant==null,"actual stage-two boss leaves final exit without another travel remnant");
            Check(progression.TryEnter(player,progression.Portal)&&level.IsLevelEnded,"stage two exit finishes the run");
            Time.captureDeltaTime=0;SessionState.SetBool(Key+"Done",true);
        }
    }
}
