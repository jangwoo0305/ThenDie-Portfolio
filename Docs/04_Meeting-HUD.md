# Meeting & Ingame HUD

## 1. Overview

Then Die의 UI는 단순히 고정된 정보를 표시하는 것이 아니라 현재 게임 진행 상태와 플레이어의 상태에 따라 지속적으로 변경되어야 합니다.

특히 회의 및 인게임 HUD에서는 다음과 같은 상태를 고려해야 했습니다.

- 현재 게임 Phase
- 플레이어 생존 여부
- 플레이어 역할
- 투표 진행 상태
- 회의 진행 상태
- 사보타지 진행 상태

이를 위해 회의/투표 UI와 인게임 HUD를 각각 구현하고, 게임의 여러 시스템에서 전달되는 상태를 UI에 반영하도록 구성했습니다.

---

## 2. Meeting Flow

회의는 하나의 화면에서 끝나는 기능이 아니라 진행 상태에 따라 UI와 플레이어의 행동 가능 여부가 변경되는 기능입니다.

전체적인 흐름은 다음과 같습니다.

```text id="z2xj7q"
Meeting Start
      │
      ▼
  Discussion
      │
      ▼
    Voting
      │
      ├── Player Vote
      └── Skip Vote
      │
      ▼
 Vote Closing
      │
      ▼
 Result Popup
      │
      ▼
Meeting End
```

회의 진행 상태에 따라 투표 가능 여부, 남은 시간, 플레이어 상태 및 결과 UI 등을 갱신하도록 구성했습니다.

---

## 3. Meeting UI Components

### MeetingPopupUI

회의 화면의 주요 UI 흐름을 관리합니다.

투표 시스템의 이벤트를 구독하여 투표 상태와 결과를 UI에 반영하고, 회의 진행 중 필요한 플레이어 기능과 연동합니다.

```text id="ky48aq"
VoteManager
     │
     ├── Vote Submitted
     └── Vote Result
             │
             ▼
      MeetingPopupUI
             │
             ▼
        UI Update
```

현재 구현은 원본 프로젝트의 `VoteManager`, `PlayerAbilities` 등 게임 시스템과 연동됩니다.

### MeetingVoteUI

회의에 참여한 플레이어들의 상태와 투표 UI를 관리합니다.

주요 역할:

- 플레이어 프로필 표시
- 투표 대상 선택
- 투표 상태 반영
- 현재 회의 Phase에 따른 UI 갱신
- 플레이어 네트워크 상태 확인
- 회의 진행 시간 표시

네트워크에 존재하는 플레이어 정보를 기반으로 회의 UI를 구성하고, 현재 상태가 변경되면 화면에 반영하도록 구현했습니다.

```text id="6ad8av"
Network Player
      │
      ▼
MeetingVoteUI
      │
      ├── Player Profile
      ├── Vote State
      └── Player State
```

### MeetingChatUI

회의 중 플레이어가 사용할 수 있는 채팅 UI를 담당합니다.

회의 상태 및 플레이어 상태에 따라 채팅 기능이 동작하도록 구성하여 일반적인 인게임 상태와 회의 상태를 구분할 수 있도록 했습니다.

### MeetingResultPopupUI

투표 종료 후 결과를 플레이어에게 표시하는 UI입니다.

회의 진행 UI와 결과 UI를 분리하여 투표 진행과 결과 표시의 책임을 나누었습니다.

```text id="qwh2sr"
Voting
   │
   ▼
Vote Result
   │
   ▼
MeetingResultPopupUI
```

---

## 4. Vote Interaction

플레이어는 회의 화면에서 다른 플레이어를 선택하거나 Skip Vote를 선택할 수 있습니다.

```text id="rd6g57"
Player Input
     │
     ├── Select Player
     │
     └── Skip
     │
     ▼
PlayerAbilities
     │
     ▼
VoteManager
     │
     ▼
Vote State
     │
     ▼
Meeting UI
```

UI에서는 투표 입력을 제공하지만 실제 투표 상태 자체는 기존 게임 시스템과 연동하여 처리하도록 구성했습니다.

이를 통해 UI가 투표 결과를 직접 결정하는 것이 아니라 게임 시스템에서 관리되는 상태를 화면에 표현하도록 역할을 분리했습니다.

---

## 5. Ingame HUD

인게임에서는 모든 플레이어에게 공통으로 필요한 UI와 역할에 따라 달라지는 UI가 존재합니다.

이를 위해 공통 HUD와 역할별 HUD를 분리했습니다.

```text id="s0tb8q"
             CommonHUD
                 │
        ┌────────┼────────┐
        ▼        ▼        ▼
   CitizenHUD  MafiaHUD  DetectiveHUD
```

`CommonHUDController`는 공통적인 플레이어 상태와 UI를 관리하고, 역할별 HUD에서는 해당 역할에 필요한 기능을 처리하도록 구성했습니다.

---

## 6. Common HUD

### CommonHUDController

게임 중 공통으로 필요한 UI와 플레이어 상태를 연결합니다.

현재 구현에서는 다음과 같은 시스템과 연동됩니다.

- `PlayerState`
- `PlayerSecret`
- `PlayerAbilities`

```text id="b9dmfb"
PlayerState ─────────┐
                     │
PlayerSecret ────────┤
                     ▼
              CommonHUDController
                     │
PlayerAbilities ─────┘
                     │
                     ▼
                  HUD UI
```

플레이어의 생존 상태나 역할이 변경되면 HUD 역시 해당 상태에 맞게 갱신될 수 있도록 구성했습니다.

---

## 7. Player State

멀티플레이 게임에서는 로컬 플레이어의 상태가 게임 진행 중 변경될 수 있습니다.

예를 들어 플레이어가 사망하면 기존 HUD를 그대로 표시하는 것이 아니라 현재 상태에 맞는 UI를 보여줘야 합니다.

`CommonHUDController`에서는 `PlayerState`의 상태 변경을 감지하여 HUD에 반영하도록 구성했습니다.

```text id="3zq9w5"
PlayerState
    │
    │ AliveChanged
    ▼
CommonHUDController
    │
    ▼
HUD Update
```

단순히 `Update()`에서 상태를 계속 확인하기보다 상태 변경 이벤트를 UI와 연결하여 필요한 시점에 갱신할 수 있도록 했습니다.

---

## 8. Role-based HUD

플레이어의 역할에 따라 필요한 UI와 기능이 달라집니다.

이를 역할별 HUD로 분리했습니다.

### CitizenHUD

시민 역할에서 필요한 게임 정보를 표시합니다.

### MafiaHUD

마피아 역할에서 필요한 능력 및 관련 UI를 담당합니다.

### DetectiveHUD

탐정 역할에서 필요한 능력 및 관련 UI를 담당합니다.

역할별 기능을 하나의 HUD 클래스에 모두 넣는 대신 역할별 HUD를 분리하여 관리하도록 구성했습니다.

```text id="mczqld"
Role Assigned
      │
      ▼
PlayerSecret
      │
      ▼
CommonHUDController
      │
      ├── Citizen
      ├── Mafia
      └── Detective
```

---

## 9. Minimap

인게임 HUD의 일부로 Minimap UI를 구현했습니다.

주요 구성:

- `MiniMapToggleUI`
- `MiniMapPoint`

Minimap은 단순 표시/숨김뿐 아니라 현재 게임 Phase와 사보타지 진행 상태 등의 영향을 받을 수 있도록 기존 게임 시스템과 연동했습니다.

```text id="pcvn4q"
GameFlowManager ────┐
                    │
SabotageManager ────┤
                    ▼
             MiniMapToggleUI
                    │
                    ▼
                 Minimap
```

현재 구현에서는 FreeRoam, Meeting, Voting 등의 게임 상태를 확인하여 Minimap 동작을 제어합니다.

---

## 10. External Dependencies

본 저장소에는 제가 담당한 Meeting 및 HUD 관련 구현 코드를 중심으로 포함하고 있습니다.

다음 시스템은 원본 팀 프로젝트의 공통 시스템이므로 전체 구현을 포함하지 않습니다.

- `VoteManager`
- `PlayerState`
- `PlayerSecret`
- `PlayerAbilities`
- `GameFlowManager`
- `SabotageManager`

네트워크 플레이어 정보를 처리하기 위해 Mirror 역시 사용합니다.

따라서 본 코드들은 독립적으로 실행되는 UI Package가 아니라 실제 멀티플레이 게임에 통합되어 사용된 클라이언트 구현 코드입니다.

---

## 11. What I Learned

Meeting과 HUD를 구현하면서 게임 UI는 단순히 화면을 구성하는 작업이 아니라 **게임 상태를 플레이어에게 정확하게 표현하는 역할**을 담당한다는 점을 경험했습니다.

특히 멀티플레이 환경에서는 플레이어의 생존 상태, 역할, 투표 상태와 같은 정보가 네트워크를 통해 변경될 수 있기 때문에 UI 역시 이러한 상태 변화에 맞춰 갱신되어야 했습니다.

또한 `PlayerState`의 상태 변경 이벤트를 HUD에 연결하고 `VoteManager`의 투표 이벤트를 Meeting UI에서 처리하면서 상태를 반복적으로 확인하는 방식뿐 아니라 이벤트 기반으로 UI를 갱신하는 구조를 실제 프로젝트에 적용했습니다.

마지막으로 HUD, 투표, Minimap처럼 서로 다른 기능들이 하나의 게임 화면에서 함께 동작해야 했기 때문에 각 UI가 어떤 시스템의 상태를 표현하고 어떤 기능까지 담당해야 하는지 구분하는 경험을 할 수 있었습니다.