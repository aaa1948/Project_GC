# 웹 데모 · 2026-10-24

## 공개 범위와 종료

PC·모바일은 스테이지 1까지 공개한다. 기존 보스 도주/보상, 캡슐 낙하와 최초 한의사 튜토리얼, 해치 탑승, 육지·위산 수면 이동, 좌측 하단 구멍 열림을 유지한다. 목적지 도착 후 아래 문구를 하단 스티치 대화창에 표시한다.

> 데모버전은 스테이지 1까지만 공개됩니다!

`메인화면으로` 확인 → 2초 나선 닫힘 → 메인화면. 대화창 중에는 전투·게임 시각을 정지하고 버튼 중복 입력을 무시한다. 성공 기록·획득 재화 저장은 한 번만 처리한다. 개발판의 Stage 2 진행 경로는 유지한다.

`WebDemoBuild.Run`은 항상 `PROJECT_GC_STAGE_ONE_ONLY`로 빌드한다. `WebDemoStageScope`가 ColonMap/ColonMonsters Resources를 일시적으로 Editor 폴더로 옮기고 빌드가 끝나면 경로/GUID를 복원한다. BuildReport에 두 폴더가 포함되면 빌드를 실패 처리한다. 중단된 작업은 `24투/Restore browser build stage resources`로 복원 가능하다.

## PC / 모바일 안내

| 동작 | PC | 모바일 |
|---|---|---|
| 이동 | WASD / 방향키 | 왼쪽 조이스틱 |
| 대쉬 | Shift | 우측 하단 대쉬 |
| 액티브 | R (신이 길게 누르기) | 대쉬 왼쪽 액티브 (신이 길게 누르기) |
| 상호작용 | 근처에서 E | 근처 NPC·오브젝트 직접 터치 |
| 아이템 | 1–4 / ZXCV | 중앙 하단 보관함 |
| 정보 / 설정 | Tab / Esc | 우측 상단 톱니바퀴 |
| 지도 | 휠 확대·축소, 드래그 이동 | 두 손가락 확대·축소, 드래그 이동 |

기기 선택 웹 페이지·PC/모바일 로딩 화면·동봉 사용 안내를 함께 갱신한다. 모바일에 별도 상호작용 버튼이 있다는 안내는 사용하지 않는다.

## 패키징

PC DXT Crunch / 모바일 ASTC 4×4 / gzip. 이미지 해상도·스프라이트 분할·필터를 유지하며 작은 도트 이미지는 무손실로 둔다. 원본 import 설정은 빌드 후 복구한다. 모바일 전용 스트리밍 압축 해제 및 Unity Services callback 호환 보정을 유지한다. 생성 빌드는 Git에 포함하지 않는다.

## 검증

- `Vampire.Tests.Editor.WebDemoEndingSmoke.Run`: 실제 Level 1 PC/터치 설정 두 경로. 캡슐 낙하/탑승/이동/출구 열림, 정확한 문구, Stage 2 미진입, 시간 정지, 성공 기록, 확인 중복, 로비 복귀와 런타임 제거 확인. Stage 2 리소스 임시 제외/원래 GUID 복원 확인.
- 모바일 HTML 17항목, 압축 다운로드 메모리 10항목, Unity callback 호환 검사 통과.
- 기기 선택 HTML 48항목 통과. 이 검사는 게임 로더를 대체한 웹 페이지 검사다.
- 실제 컴파일된 PC 웹에서 인트로·출전 준비·전투·증강 선택을 확인했다. 안드로이드/아이폰 User-Agent·터치 에뮬레이션의 실제 모바일 빌드 출전/전투 진입도 통과했다. 휴대폰 실기기 및 실제 Safari의 성능 테스트는 하지 않았다.


## 배포 결과

- 현재 주소: https://touch-knives-won-dispatched.trycloudflare.com
- 기존 `https://gps-moms-studying-mai.trycloudflare.com` 주소는 DNS 만료로 사용할 수 없어 새 임시 주소를 발급했다. 호스트 PC와 터널이 실행 중인 동안 접속 가능하다. 주소 변경으로 이전 사이트의 브라우저 저장 데이터가 자동 이전되지는 않는다.
- 게임 코드: `697f74dfb5e96cb3d9c6d45a5305bceddf567deb`. 빌드 당시 작업 트리를 해당 커밋과 확인한 뒤 배포 메타데이터에 기록했다. 빌드 시작 기준 커밋은 별도 `buildBaseCommit`으로 보존했다.
- PC: DXT Crunch 대상 258개, 실제 참조 파일+StreamingAssets 다운로드 564,520,883 bytes (약 564.5 MB).
- 모바일: ASTC 4×4 대상 749개 (비정형 크기 491개 포함), 실제 다운로드 400,731,746 bytes (약 400.7 MB). 모바일 안내는 올림값 401 MB로 표시한다.
- 두 빌드 모두 `stageCount=1`, `stageTwoAssetsExcluded=true`. Unity BuildReport에서 대장 맵/몬스터 없음 확인. 원본 대장 Resources와 texture import 설정은 복원했다.
- 외부 HTTPS 루트·PC 빌드 정보·모바일 빌드 정보·모바일 페이지 모두 HTTP 200 및 정확한 소스/스테이지 범위 확인.
- 종료 회귀 최종 37항목 통과, 오류 0. PC/터치 각각 획득 재화 저장과 중복 완료 방지까지 검증했다.
- 배포 서버 16항목, 최신 안내 페이지 48항목, 모바일 로딩 페이지 17항목, 메모리 스트리밍 10항목, Unity Services callback ABI 검사 통과.
- 실제 웹 QA에서 신규 계정의 개인 랭킹 조회 404는 아직 점수가 없는 플레이어 응답이다. 게임 로딩 실패가 아니다. 기존 MapPanelRoot/MapViewport의 누락 스크립트 경고와 Unity Web API 경고는 남아 있으며, 이번 검사에서 Unity 실행 예외나 브라우저 pageerror는 없었다.
- 노션: [통합 문서](https://app.notion.com/p/341636e6d18f807594a2ec7c596e2c7f), [위장](https://app.notion.com/p/402636e6d18f8154a92aff6dd54283f7), [캡슐 이동](https://app.notion.com/p/402636e6d18f8147a10ced9d0e28e46f). 후반 몬스터 수치·스폰표·걷기 GIF, 웹 데모 종료 화면, PC/모바일 조작과 개발판/공개판 차이를 기록한다.
