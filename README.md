# 24시간의사투

<p align="center">
  <img src="https://raw.githubusercontent.com/junanime/Project_GC/Junhan2/Assets/Junhan/Art/PhoenixSkills/HyukiActive.png" width="112" alt="24시간의사투 — 혁이 액티브 스킬 대표 아이콘" />
</p>

캐릭터 고유 스킬과 대시를 활용해 적을 피하고, 무기와 능력을 성장시키며 보스에 도전하는 2D 생존 액션 게임입니다. 현재 **Windows PC 테스트 버전**을 준비하고 있습니다.

## 다운로드 · 설치

### [Windows 설치 파일 · Releases](https://github.com/junanime/Project_GC/releases)

**공개 업로드 준비 중:** 설치본 제작과 로컬 설치 검증은 완료했지만, GitHub Releases 파일 업로드는 인증 문제로 아직 완료되지 않았습니다. Releases에 설치 파일이 표시된 뒤 다운로드할 수 있습니다.

1. 공개 후 위 Releases의 해당 버전 **Assets**에서 `24tu-Setup-<버전>.exe`를 다운로드합니다.
2. 실행 중인 게임이 있다면 종료한 뒤 설치 파일을 실행합니다.
3. 바탕화면의 **24시간의사투 (테스트)** 아이콘으로 실행합니다. 대표 아이콘은 혁이의 액티브 스킬 이미지입니다.
4. 게임 시작 → 캐릭터·유물·아이템 준비 → **출전하기** 순서로 시작합니다.

- **64비트 Windows용**입니다. 플레이를 위해 Unity나 프로젝트 소스를 설치할 필요는 없습니다.
- `Code → Download ZIP`과 릴리스의 `Source code`는 개발용 원본이며 게임 설치 파일이 아닙니다.
- 현재 설치 파일은 코드 서명 전 테스트 버전입니다. 보안 경고가 나타날 수 있습니다. 출처·파일명을 확인하고, 백신이나 Windows 보안 기능을 끄지 마세요.
- Windows의 설치된 앱 목록에서 게임을 제거할 수 있습니다. 설치 폴더와 영구 저장 데이터는 분리되어 있습니다.

## 플레이 방법

기본 무기 공격은 자동으로 진행됩니다. 이동과 대시로 적의 공격을 피하면서 경험치와 재화를 모으고, 레벨업 시 강화할 능력을 선택하세요. 필드의 상호작용 대상은 가까이 다가가 화면 안내에 따라 이용합니다.

| 기능 | 조작 | 설명 |
| --- | --- | --- |
| 이동 | `W A S D` / 방향키 | 캐릭터 이동 |
| 대시 | 왼쪽 또는 오른쪽 `Shift` | 충전량이 있을 때 사용; 캐릭터/상태에 따라 제한 |
| 액티브 스킬 | `R` | 선택 캐릭터의 고유 스킬; 재사용 대기시간 적용 |
| 상호작용 | `E` | 대상 가까이에서 사용; 화면 안내 우선 |
| 상태·탐험 기록 | `Tab` | 전투 중 기록 화면 열기/닫기 |
| 뒤로 / 기록 화면 | `Esc` | 열린 화면에서 뒤로; 전투 중에는 기록 화면 열기 |
| 보유 소모품 | `Z X C V` / `1 2 3 4` | 해당 슬롯 아이템 사용; 보유 수량 필요 |
| 메뉴·강화 선택 | 마우스 클릭 | 버튼과 선택지 조작 |

테스트용: 전투 중 `Insert`는 달팽이 보스 소환 단축키입니다. 이미 보스가 있거나 소환 중인 경우 등에는 동작하지 않습니다. 노트북은 키보드의 `Fn` 조합이 필요할 수 있습니다.

## 캐릭터와 보스

- **아시 · 아리 · 혁이 · 신이**: 캐릭터별 패시브와 `R` 액티브 스킬을 활용합니다. 잠긴 캐릭터나 유물은 로비의 잠금 해제 메뉴에서 확인하세요.
- **롤케이크 달팽이 보스**: 생크림과 초콜릿 롤케이크의 2페이즈 보스입니다. 탄막, 투척, 유도탄, 돌진, 미니 달팽이 소환 등 다양한 패턴에 대응합니다.
- 필드 미니 롤케이크 달팽이를 활용한 보스 약화 요소도 테스트할 수 있습니다.

테스트 중인 게임이므로 콘텐츠·수치·연출은 계속 변경됩니다.

## 저장과 업데이트

**재화·해금·유물 등 영구 성장만 유지합니다. 진행 중인 판을 종료 후 그대로 이어하는 기능은 제공하지 않습니다.**

- 현재는 같은 Windows 사용자 계정의 **로컬 저장**입니다. 다른 PC나 Windows 사용자 계정으로 자동 이전되지 않습니다.
- Steam/Google 로그인 및 계정 기반 클라우드 저장은 아직 구현되지 않았습니다.
- 현재 버전에는 자동 패치나 게임 내 업데이트 버튼이 없습니다. 새 버전이 나오면 Releases에서 최신 설치 파일을 받아, 게임을 종료한 뒤 같은 위치에 설치하세요. 기존 로컬 저장 데이터는 유지하도록 구성되어 있습니다.
- GitHub에 코드를 올리는 것만으로 설치된 게임이 자동 업데이트되지는 않습니다. 새 실행 파일을 빌드·검증하고 배포해야 합니다.
- Steam 배포 시에는 Steam의 업데이트 체계를 사용할 예정입니다. 직접 배포판의 자동 업데이트는 별도 런처/업데이터와 배포 저장소가 필요한 후속 작업입니다.

## 문제를 발견했다면

[GitHub Issues](https://github.com/junanime/Project_GC/issues)에 버전, 재현 순서, 기대한 동작, 실제 동작, 가능하면 화면 또는 영상을 남겨주세요. 공개 게시물에는 개인 정보가 포함된 전체 PC 경로나 저장 데이터를 그대로 올리지 마세요.

## 개발

- 엔진: **Unity 2022.3.62f3**
- 현재 개발·Windows 배포 기준 브랜치: **[Junhan2](https://github.com/junanime/Project_GC/tree/Junhan2)**
- 저장소 첫 화면의 `main` README도 게임 안내용으로 유지합니다. 최신 개발 코드를 열 때는 `Junhan2`를 선택하세요.
- [Windows 배포 및 저장 정책 문서](https://github.com/junanime/Project_GC/blob/Junhan2/Documentation/WindowsDesktopDistribution.md)

<details>
<summary>원작 기반 및 기존 리소스 출처</summary>

이 프로젝트는 [Matthias Broske의 VampireSurvivorsClone](https://github.com/matthiasbroske/VampireSurvivorsClone)을 기반으로 개발되었습니다. 기존 기반 코드의 저작권 고지 및 MIT 허가문은 [LICENSE](LICENSE)에 보존합니다. 이 표기는 기존 기반 코드의 출처이며, 추가된 모든 이미지·음원·리소스에 동일한 라이선스가 적용된다는 의미는 아닙니다.

기존 README에 명시된 리소스 출처:

- [Kenney](https://www.kenney.nl/assets)
- [Bonsaiheldin — gold treasure icons](https://opengameart.org/content/gold-treasure-icons-16x16)
- [Noto Sans CJK TC](https://fonts.google.com/noto/specimen/Noto+Sans+TC/about)

</details>
