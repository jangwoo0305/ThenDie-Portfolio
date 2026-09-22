# Owl Mail Sabotage

## 1. Overview

Owl Mail Sabotage는 사보타지 발생 시 맵에 생성되는 편지를 플레이어가 수집하여 제한 시간 안에 우편함으로 전달하는 게임플레이입니다.

단순 UI 미니게임과 달리 실제 게임 월드에서 여러 플레이어가 동시에 참여하는 콘텐츠이기 때문에 다음 요소를 함께 고려해야 했습니다.

- 월드 오브젝트와의 상호작용
- 편지 획득 및 보유 상태
- 멀티플레이 상태 동기화
- 서버 기준의 상호작용 검증
- 사보타지 진행 상태
- 진행도 UI

초기에는 네트워크 환경과 분리된 Offline Prototype을 제작하여 핵심 게임플레이를 먼저 검증한 뒤, Mirror 기반 네트워크 구조로 확장했습니다.

---

## 2. Gameplay Flow

전체 게임플레이 흐름은 다음과 같습니다.

```text
Sabotage Start
      │
      ▼
Letter Spawn
      │
      ▼
Player Interaction
      │
      ▼
Letter Pickup
      │
      ▼
Player Carry
      │
      ▼
Mailbox Delivery
      │
      ▼
Sabotage Progress
      │
      ├── Required Count Reached → Success
      │
      └── Time Limit Exceeded   → Fail
```

플레이어는 맵에 생성된 편지를 찾아 획득하고 우편실의 우편함으로 이동하여 보유 중인 편지를 전달합니다.

필요한 수의 편지가 전달되면 사보타지가 해제되는 구조입니다.

---

## 3. Development Process

### Step 1. Offline Prototype

처음부터 네트워크 코드를 작성하기보다 네트워크 환경과 분리된 상태에서 핵심 게임플레이를 먼저 구현했습니다.

이 단계에서는 다음 흐름을 검증하는 데 집중했습니다.

```text
Letter
  ↓
Player Pickup
  ↓
Carry
  ↓
Mailbox
  ↓
Delivery
```

이를 통해 네트워크 동기화 문제와 게임 규칙 자체의 문제를 분리하여 확인할 수 있도록 했습니다.

### Step 2. Interaction System

월드에 존재하는 편지와 우편함을 동일한 방식으로 상호작용할 수 있도록 상호작용 구조를 구성했습니다.

주요 구성 요소:

- `IInteractable`
- `PlayerInteractor`

```text
PlayerInteractor
       │
       │ Detect
       ▼
  IInteractable
       │
       ├── OwlMailLetter
       └── OwlMailMailbox
```

플레이어는 주변의 상호작용 가능한 오브젝트를 탐색하고 해당 오브젝트에 상호작용을 요청합니다.

이를 통해 편지와 우편함 각각에 별도의 플레이어 입력 로직을 작성하지 않고 동일한 상호작용 흐름을 사용할 수 있도록 구성했습니다.

### Step 3. Network Integration

Offline Prototype에서 검증한 게임플레이를 Mirror 기반 멀티플레이 구조로 확장했습니다.

주요 네트워크 요소:

- `NetworkBehaviour`
- `NetworkIdentity`
- `[Command]`
- `[SyncVar]`

플레이어의 입력 자체는 로컬에서 발생하지만 실제 게임 상태 변경은 서버에서 처리하도록 구성했습니다.

```text
Local Player
     │
     │ Interaction Input
     ▼
PlayerInteractor
     │
     │ Command
     ▼
   Server
     │
     ├── Interaction Validation
     │
     ├── Letter Pickup
     │
     └── Mailbox Delivery
     │
     ▼
Network State Sync
```

이를 통해 각 클라이언트가 임의로 편지 획득이나 제출 상태를 변경하는 것이 아니라 서버의 게임 상태를 기준으로 처리할 수 있도록 했습니다.

---

## 4. Core Components

### PlayerInteractor

플레이어 주변에 존재하는 상호작용 가능한 네트워크 오브젝트를 탐색하고 상호작용 요청을 처리합니다.

주요 역할:

- 주변 `NetworkIdentity` 탐색
- 상호작용 대상 관리
- 플레이어 입력 처리
- 서버에 상호작용 요청
- 현재 게임 진행 상태 확인

현재 구현에서는 `GameFlowManager` 및 `PlayerState`와 연동하여 플레이어가 상호작용할 수 있는 상태인지 확인합니다.

해당 시스템들은 원본 프로젝트의 공통 시스템이므로 포트폴리오 저장소에는 전체 구현을 포함하지 않습니다.

### OwlMailLetter

월드에 생성되는 편지 오브젝트입니다.

주요 역할:

- 플레이어와의 상호작용
- 현재 사보타지 진행 여부 확인
- 플레이어의 편지 보유 가능 여부 확인
- 편지 획득 처리

현재 사보타지 상태 및 최대 보유 가능 개수 등의 게임 규칙은 `SabotageManager`와 연동됩니다.

### OwlMailPlayerCarry

플레이어가 현재 보유하고 있는 편지 상태를 관리합니다.

Mirror의 `SyncVar`를 이용하여 편지 보유 상태를 네트워크에서 동기화하도록 구성했습니다.

```text
Player
  │
  ▼
OwlMailPlayerCarry
  │
  └── Current Letter Count
```

### OwlMailMailbox

플레이어가 획득한 편지를 제출하는 오브젝트입니다.

플레이어가 우편함과 상호작용하면 현재 보유 중인 편지를 확인하고 서버에서 전달 처리를 수행합니다.

```text
Player Carry
     │
     ▼
OwlMailMailbox
     │
     ▼
ServerDeliverLetters()
     │
     ▼
Sabotage Progress
```

### OwlMailSabotageUI

현재 Owl Mail 사보타지의 진행 상태를 플레이어에게 표시합니다.

사보타지 시스템의 현재 상태를 확인하여 진행도 및 관련 UI를 갱신하도록 구성했습니다.

---

## 5. Server Authority

멀티플레이 환경에서는 클라이언트가 직접 중요한 게임 상태를 변경하지 않도록 하는 것이 필요했습니다.

예를 들어 편지 획득을 단순히 로컬에서 처리한다면 여러 클라이언트에서 동일한 편지를 동시에 획득하거나 서로 다른 편지 개수를 가지는 문제가 발생할 수 있습니다.

따라서 다음과 같은 흐름을 사용했습니다.

```text
Client
  │
  │ "이 편지와 상호작용"
  ▼
Command
  │
  ▼
Server
  │
  ├── 상호작용 가능 여부 확인
  ├── 사보타지 진행 상태 확인
  └── 편지 보유 가능 여부 확인
  │
  ▼
Game State Update
  │
  ▼
Client Sync
```

클라이언트는 상호작용을 요청하고 실제 게임 상태 변경은 서버에서 처리하는 구조를 사용했습니다.

---

## 6. Integration with Sabotage System

Owl Mail 자체에서 전체 게임의 사보타지 상태를 관리하지 않고 기존 `SabotageManager`와 연동하도록 구성했습니다.

```text
SabotageManager
       │
       ├── IsOwlMailRunning
       ├── MaxCarryCount
       ├── Interaction Validation
       └── Delivery Progress
                ▲
                │
       Owl Mail System
```

Owl Mail 시스템은 편지 획득과 전달이라는 개별 게임플레이를 담당하고, 전체 사보타지의 시작/진행/완료 상태는 상위 시스템에서 관리하도록 역할을 분리했습니다.

---

## 7. External Dependencies

본 포트폴리오 저장소에서는 제가 담당한 Owl Mail 관련 구현을 중심으로 공개하고 있습니다.

따라서 다음과 같은 원본 프로젝트의 공통 시스템 전체 구현은 포함되어 있지 않습니다.

- `SabotageManager`
- `GameFlowManager`
- `PlayerState`

또한 네트워크 구현을 위해 Mirror를 사용합니다.

따라서 본 디렉터리의 코드는 독립적으로 실행되는 Unity Package가 아니라 실제 팀 프로젝트에서 사용된 기능 구현 코드의 일부입니다.

---

## 8. What I Learned

Owl Mail 기능을 개발하면서 로컬에서 동작하는 게임플레이를 멀티플레이 환경으로 확장할 때 고려해야 하는 차이를 경험했습니다.

특히 먼저 Offline Prototype으로 핵심 게임 규칙을 검증하고 이후 네트워크 기능을 적용하면서 게임플레이 문제와 네트워크 문제를 분리하여 개발할 수 있었습니다.

또한 클라이언트의 입력과 실제 게임 상태 변경을 분리하고 서버에서 중요한 상태를 처리하는 구조를 구현하면서 Mirror의 `Command`, `SyncVar`, `NetworkIdentity` 등의 역할을 실제 게임 기능에 적용해 볼 수 있었습니다.

마지막으로 하나의 기능을 독립적으로 구현하는 것에서 끝나는 것이 아니라 `SabotageManager`, `GameFlowManager`, HUD 등 기존 게임 시스템과 연결하면서 여러 시스템 사이의 책임과 의존성을 고려하는 경험을 할 수 있었습니다.