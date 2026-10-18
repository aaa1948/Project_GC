using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Vampire
{
    public sealed class FoodBossHealthHud:MonoBehaviour
    {
        FoodBossRuntime boss;GameObject root;Image fill;TextMeshProUGUI label;
        public void Initialize(FoodBossRuntime b)
        {
            boss=b;root=new GameObject("Food boss health",typeof(Canvas),typeof(CanvasScaler));var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=20;
            root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;root.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1280,720);
            var back=new GameObject("Health",typeof(RectTransform),typeof(Image));back.transform.SetParent(root.transform,false);var r=(RectTransform)back.transform;r.anchorMin=new Vector2(.23f,.84f);r.anchorMax=new Vector2(.77f,.875f);r.offsetMin=r.offsetMax=Vector2.zero;back.GetComponent<Image>().color=new Color(.13f,.08f,.14f,.9f);back.GetComponent<Image>().raycastTarget=false;
            var f=new GameObject("Fill",typeof(RectTransform),typeof(Image));f.transform.SetParent(back.transform,false);fill=f.GetComponent<Image>();fill.color=b.Warning;fill.raycastTarget=false;fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            label=new GameObject("Boss name",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(back.transform,false);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            if(ApothecaryUI.Instance!=null)label.font=ApothecaryUI.Instance.Config.font;label.fontSize=19;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.outlineWidth=.18f;label.outlineColor=new Color(.15f,.07f,.1f);label.raycastTarget=false;
        }
        void LateUpdate(){if(boss==null||root==null)return;root.SetActive(!boss.Dead&&!MiniStageRuntimeState.IsInsideMiniStage&&(ApothecaryUI.Instance==null||ApothecaryUI.Instance.Page=="hud"));fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(boss.Health/boss.MaxHealth),1);label.text=$"{FoodBossArt.Names[(int)boss.Kind]} · {(boss.PhaseTwo?2:1)}페이즈   {Mathf.CeilToInt(boss.Health):N0}"+(boss.Recovering?(boss.Kind==FoodBossKind.Denikani?$"  방향 뒤집기 {boss.Reversals}/5":"  회복 저지!"):"");}
        void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
