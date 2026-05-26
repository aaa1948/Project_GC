using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    public class CharacterSelection : MonoBehaviour
    {
        public CharacterData[] characters; // 캐릭터 데이터 배열
        private int currentIndex = 0;      // 현재 선택된 캐릭터 인덱스

        [System.Serializable]
        public struct CharacterData
        {
            public string charName;       // 캐릭터 이름
            public Sprite charSprite;     // 캐릭터 이미지
            public string description;    // 캐릭터 설명
            public int attackStat;        // 공격력 (1~5)
            public int defenseStat;       // 방어력 (1~5)
            public int speedStat;         // 속도 (1~5)
        }

        [Header("UI Elements - Center")]
        public Image centerCharImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public Slider attackSlider;
        public Slider defenseSlider;
        public Slider speedSlider;

        [Header("UI Elements - Left/Right Preview")]
        public Image leftPreviewImage;
        public TextMeshProUGUI leftNameText;
        public Image rightPreviewImage;
        public TextMeshProUGUI rightNameText;
        public GameObject leftCard;  // 첫 캐릭터일 때 숨기기용
        public GameObject rightCard; // 마지막 캐릭터일 때 숨기기용

        // Start is called before the first frame update
        void Start()
        {
            UpdateCharacterUI();
        }

        // Update is called once per frame
        void Update()
        {

        }

        // 오른쪽 버튼 클릭 시 호출
        public void OnClickNext()
        {
            if (currentIndex < characters.Length - 1)
            {
                currentIndex++;
                UpdateCharacterUI();
            }
        }

        // 왼쪽 버튼 클릭 시 호출
        public void OnClickPrev()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                UpdateCharacterUI();
            }
        }

        // UI를 현재 인덱스에 맞게 새로고침하는 함수
        void UpdateCharacterUI()
        {
            CharacterData current = characters[currentIndex];

            // 1. 중앙 캐릭터 정보 세팅
            centerCharImage.sprite = current.charSprite;
            nameText.text = current.charName;
            descText.text = current.description;
            attackSlider.value = current.attackStat;
            defenseSlider.value = current.defenseStat;
            speedSlider.value = current.speedStat;

            // 2. 왼쪽 이전 캐릭터 프리뷰 세팅
            if (currentIndex > 0)
            {
                leftCard.SetActive(true);
                leftPreviewImage.sprite = characters[currentIndex - 1].charSprite;
                leftNameText.text = characters[currentIndex - 1].charName;
            }
            else
            {
                leftCard.SetActive(false); // 이전 캐릭터가 없으면 프리뷰 숨김
            }

            // 3. 오른쪽 다음 캐릭터 프리뷰 세팅
            if (currentIndex < characters.Length - 1)
            {
                rightCard.SetActive(true);
                rightPreviewImage.sprite = characters[currentIndex + 1].charSprite;
                rightNameText.text = characters[currentIndex + 1].charName;
            }
            else
            {
                rightCard.SetActive(false); // 다음 캐릭터가 없으면 프리뷰 숨김
            }
        }
    }
}