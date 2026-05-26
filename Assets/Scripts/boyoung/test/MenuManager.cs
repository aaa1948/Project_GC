using UnityEngine;
using UnityEngine.SceneManagement; // 인게임 씬(화면)으로 넘어가기 위해 필요한 기능

public class MenuManager : MonoBehaviour
{
    [Header("화면 오브젝트들")]
    // 꺼야 할 첫 화면 (StartScreen)
    public GameObject startScreen;

    // 켜야 할 캐릭터 선택 화면 (CharacterSelectGroup)
    public GameObject characterSelectScreen;

    /// <summary>
    /// [게임 시작] 버튼을 눌렀을 때 (첫 화면 -> 캐릭터 선택 창)
    /// </summary>
    public void ClickGameStart()
    {
        if (startScreen != null) startScreen.SetActive(false);                 // 첫 화면 끄기
        if (characterSelectScreen != null) characterSelectScreen.SetActive(true); // 캐릭터 선택창 켜기
    }

    /// <summary>
    /// [뒤로가기] 버튼을 눌렀을 때 (캐릭터 선택 창 -> 첫 화면)
    /// </summary>
    public void ClickBack()
    {
        if (characterSelectScreen != null) characterSelectScreen.SetActive(false); // 캐릭터 선택창 끄기
        if (startScreen != null) startScreen.SetActive(true);                 // 첫 화면 켜기
    }

    /// <summary>
    /// [선택하기] 버튼을 눌렀을 때 (실제 인게임 화면으로 진입)
    /// </summary>
    public void ClickSelectAndPlay()
    {
        // TODO: 나중에 실제 플레이할 인게임 Scene(씬)의 이름을 "InGameScene" 자리에 적어주면 됩니다.
        // 지금은 임시로 현재 화면(메인메뉴)이 다시 부드럽게 새로고침 되거나, 씬 세팅이 안 되어 있으면 에러가 날 수 있으므로
        // 로그를 찍고 다음 화면으로 넘어가도록 기본 코드를 작성해 둡니다.

        Debug.Log("캐릭터 선택 완료! 게임을 시작합니다.");

        SceneManager.LoadScene("Main Menu");
    }
}
