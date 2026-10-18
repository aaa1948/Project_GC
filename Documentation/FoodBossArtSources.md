# 보스 아트 제작 기록

확정 이동 원화의 형태/비율을 기준으로 5종 × 2페이즈, 각 8프레임을 동일 캔버스와 바닥 피벗에 정렬했다. PNG는 `Assets/Resources/FoodBosses`에 포함되어 외부 경로 없이 실행된다.

제작 도구: 내장 image_gen. 공격 부품과 회복 그림은 투명 배경으로 생성하고, 알파를 유지한 채 기계적으로 분리/축소했다.

제작 프롬프트 구성:
- 확정 보스 원화를 참조하여 같은 외곽선·픽셀 질감·색조로 공격용 분리 부품을 제작. 데니쉬 집게, 날개, 설탕 뭉치와 손상형, 설탕막 딸기씨.
- 몸체 형태를 유지한 집게 없는 소라게 몸체, 숨은 껍질, 비행 풍뎅이, 닫힌 가리비, 웅크리거나 도약한 우동 말미잘.
- 보스별 기본 탄막: 블루베리, 버터, 레몬씨, 청포도, 나루토 어묵. 배경·글자 없이 선명한 작은 실루엣.
- 롤팽이 회전용 케이크와 떠오르는 토핑을 각각 1/2페이즈 원화와 동일하게 분리. 레몬 혓바닥, 딸기씨, 딸기, 즙 방울.

집게 관절 추가 제작의 최종 프롬프트:
> Create a production-ready game sprite attachment for the reference Danish pastry hermit crab. Reference image is style and palette only. Output ONLY ONE detached straight forearm segment, horizontally oriented left to right, isolated on true transparent background, centered with empty margins. No character, no claws, no fingers, no text. This short articulated crab arm segment joins the body to a pincer. Warm baked caramel brown exoskeleton, cream pale rounded ball joint at left end, slightly tapering glossy brown carapace capsule to the right end. Length about 3 times thickness. Chunky clean pixel art outline matching reference black-brown outline, smooth readable large highlights, few curved shell seam details, no wood grain. End joints rounded so multiple copies can meet seamlessly at angled elbows. No shadow or backdrop.

사운드는 image_gen과 별개이며 자체 합성 도구 `Tools/generate-food-boss-audio.cjs`로 제작했다.
