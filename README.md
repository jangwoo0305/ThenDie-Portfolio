# Then Die — Unity Client Portfolio

> 팀 **소르베**에서 개발 중인 멀티플레이 게임 **Then Die**에서 담당한 Unity 클라이언트 개발 작업을 정리한 포트폴리오 저장소입니다.

원본 프로젝트는 팀 프로젝트로 Private Repository에서 관리되고 있습니다.  
본 저장소는 전체 게임 프로젝트를 배포하기 위한 저장소가 아니며, 제가 담당한 주요 시스템의 구현 코드와 설계 내용을 포트폴리오 목적으로 정리합니다.

## Project Overview

**Then Die**는 여러 플레이어가 각자의 역할을 수행하며 미션, 회의, 투표, 사보타지 등의 상호작용을 진행하는 멀티플레이 게임 프로젝트입니다.

- **Engine:** Unity
- **Language:** C#
- **Networking:** Mirror
- **Voice Chat:** Vivox
- **Role:** Unity Client Developer

## My Contributions

### 1. MiniGame Framework

여러 미니게임을 개별적으로 실행하던 구조에서 공통 `MiniGameManager`를 중심으로 관리하는 구조로 개선했습니다.

`MiniGameLauncher`와 `MiniGameType`을 통해 미니게임 실행 진입점을 통일하고, 각 미니게임의 성공/실패 결과를 공통 흐름에서 처리할 수 있도록 구성했습니다.

**구현 미니게임**

- Temperature
- Potion
- Puzzle Picture
- Star Align

```text
MiniGameLauncher
       │
       ▼
MiniGameManager
       │
       ├── Temperature
       ├── Potion
       ├── Puzzle Picture
       └── Star Align
```

### 2. Task System

미니게임 및 맵 상호작용을 게임 진행도와 연결하기 위한 Objective 기반 Task 시스템을 구현했습니다.

**주요 구성**

- `TaskManager`
- `TaskData`
- `TaskType`
- `ObjectiveType`
- `TaskObjectiveCompleter`
- `AreaVisitTrigger`

미니게임 완료, 특정 지역 방문 등의 행동을 Objective로 관리하여 서로 다른 종류의 임무를 동일한 Task 흐름에서 처리할 수 있도록 구성했습니다.

```text
Player Action
     │
     ▼
Objective
     │
     ▼
TaskManager
     │
     ▼
Task Progress
```

### 3. Owl Mail Sabotage

제한 시간 동안 맵에 생성된 편지를 수집하여 지정된 우편함에 전달하는 사보타지 게임플레이를 구현했습니다.

초기에는 네트워크와 분리된 Offline Prototype으로 게임플레이를 검증한 뒤, Mirror 기반 멀티플레이 구조로 확장했습니다.

```text
PlayerInteractor
       │
       ▼
OwlMailLetter
       │
       ▼
OwlMailPlayerCarry
       │
       ▼
OwlMailMailbox
       │
       ▼
Sabotage Progress
```

**주요 구현 내용**

- 상호작용 인터페이스 및 플레이어 상호작용 처리
- 편지 Spawn / Pickup
- 플레이어 편지 보유 상태 동기화
- 우편함 제출
- 사보타지 진행도 UI
- Mirror 기반 서버 권한 상호작용
- 기존 Sabotage 시스템과 통합

### 4. Meeting & Voting UI

멀티플레이 회의 과정에서 사용되는 회의 및 투표 UI를 구현했습니다.

**주요 구현 내용**

- 플레이어 프로필 기반 투표
- Skip Vote
- 투표 완료 상태 처리
- 회의 채팅
- 투표 결과 Popup
- 플레이어 상태에 따른 UI 갱신
- 기존 Vote / Voice 시스템과 연동

### 5. Ingame HUD

게임 상태 및 플레이어 역할에 따라 필요한 정보를 표시하는 HUD 시스템을 구현했습니다.

공통 HUD와 역할별 HUD를 분리하여 관리했습니다.

```text
CommonHUD
   │
   ├── CitizenHUD
   ├── MafiaHUD
   └── DetectiveHUD
```

**주요 구현 내용**

- 플레이어 상태 UI
- 개인 미션 진행도
- Citizen / Mafia / Detective 역할별 HUD
- 능력 및 Cooldown UI
- Minimap
- Dead Player UI
- Voice Chat 입력/출력 제어

## Repository Structure

```text
Scripts/
├── MiniGame/
│   ├── Temperature/
│   ├── Potion/
│   ├── PuzzlePicture/
│   └── StarAlign/
├── Task/
├── OwlMail/
├── Meeting/
└── HUD/
```

## External Dependencies

본 저장소는 전체 Then Die 프로젝트가 아닌 **포트폴리오용 코드 아카이브**입니다.

따라서 일부 코드는 원본 프로젝트의 다음 시스템과 연결되어 있으며, 해당 시스템의 전체 구현은 포함하지 않습니다.

- `GameFlowManager`
- `MissionManager`
- `SabotageManager`
- `VoteManager`
- `PlayerState`
- `PlayerAbilities`
- `PlayerSecret`
- `ProximityVoiceManager`

네트워크 관련 기능은 Mirror를 기반으로 구현되었습니다.

## Notes

팀 프로젝트의 전체 소스 코드, 팀원이 작성한 코드, 외부 에셋 및 프로젝트 리소스는 본 저장소에 포함하지 않습니다.

본 저장소는 제가 직접 담당한 개발 내용과 구현 과정을 설명하기 위한 포트폴리오 목적으로 구성되어 있습니다.