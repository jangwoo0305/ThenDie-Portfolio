# Task System

## 1. Overview

Then Die에서는 플레이어가 게임을 진행하면서 다양한 형태의 임무를 수행합니다.

예를 들어 다음과 같은 행동들이 하나의 임무 조건이 될 수 있습니다.

- 특정 미니게임 완료
- 특정 지역 방문
- 특정 오브젝트와 상호작용
- 여러 Objective를 순차적으로 완료

각 콘텐츠에서 임무 진행 상태를 개별적으로 관리하면 새로운 임무가 추가될 때마다 별도의 진행 로직이 필요해집니다.

이를 개선하기 위해 플레이어가 수행해야 하는 행동을 **Objective**라는 공통 단위로 표현하고 `TaskManager`가 Task의 진행 상태를 관리하도록 구성했습니다.

---

## 2. Problem

초기에는 미니게임 자체의 성공 여부를 판단하는 것이 중요했습니다.

하지만 미니게임이 실제 게임의 미션으로 사용되면서 다음 단계가 필요해졌습니다.

```text id="0e0xuq"
MiniGame Start
      │
      ▼
MiniGame Success
      │
      ▼
???
      │
      ▼
Mission Progress
```

미니게임이 직접 전체 미션 진행 상태까지 변경하도록 구현할 수도 있지만, 이 경우 미니게임과 게임 진행 시스템의 결합도가 높아집니다.

또한 임무의 조건이 반드시 미니게임 완료인 것도 아니었습니다.

```text id="h21qkh"
Task A
 └── MiniGame 완료

Task B
 └── 특정 지역 방문

Task C
 ├── Objective 1
 ├── Objective 2
 └── Objective 3
```

서로 다른 행동을 하나의 임무 시스템에서 처리할 수 있는 공통 구조가 필요했습니다.

---

## 3. Solution

Task와 실제 완료 조건을 분리했습니다.

```text id="d3ypke"
Player Action
     │
     ▼
 Objective
     │
     ▼
TaskManager
     │
     ▼
 TaskData
     │
     ▼
Task Progress
```

플레이어가 수행한 행동은 해당하는 Objective의 완료로 전달되고 `TaskManager`가 Task의 전체 진행 상태를 판단합니다.

이 구조를 통해 미니게임이나 지역 Trigger가 Task의 전체 구현을 직접 알 필요가 없도록 구성했습니다.

---

## 4. Core Components

### TaskManager

Task의 진행 상태를 중앙에서 관리하는 역할을 담당합니다.

개별 콘텐츠에서 특정 조건이 완료되면 `TaskManager`에 Objective 완료를 전달하고, TaskManager는 해당 Task의 진행 상태를 갱신합니다.

```text id="e11vny"
Content
   │
   │ Objective Complete
   ▼
TaskManager
   │
   ▼
TaskData
```

이를 통해 콘텐츠마다 별도의 Task 진행 관리 코드를 작성하는 것을 줄이고자 했습니다.

### TaskData

각 Task가 어떤 Objective를 필요로 하고 현재 어떤 Objective가 완료되었는지를 관리하기 위한 데이터입니다.

개념적으로 다음 두 상태를 관리합니다.

```text id="7l1u7v"
TaskData
 ├── Required Objectives
 └── Completed Objectives
```

필요한 Objective와 완료된 Objective를 분리하여 현재 Task의 진행 상태를 판단할 수 있도록 구성했습니다.

### ObjectiveType

Task 완료 조건의 종류를 구분합니다.

예를 들어 미니게임 완료나 특정 지역 방문과 같은 서로 다른 플레이어 행동을 Objective라는 동일한 개념으로 처리할 수 있도록 사용했습니다.

```text id="xv2d0c"
Objective
   │
   ├── MiniGame Complete
   ├── Area Visit
   └── Interaction
```

새로운 임무 조건이 필요한 경우 기존 Task 시스템 전체를 새로 만드는 대신 Objective 종류를 확장하여 사용할 수 있도록 하는 것을 목표로 했습니다.

### TaskType

현재 처리할 Task의 종류를 구분하기 위해 사용합니다.

`ObjectiveType`이 **무엇을 완료해야 하는가**를 표현한다면 `TaskType`은 **어떤 Task인가**를 구분하는 역할을 담당합니다.

### TaskObjectiveCompleter

게임 오브젝트에서 특정 Objective를 완료 상태로 전달하기 위한 연결 지점 역할을 담당합니다.

```text id="slvblv"
Game Object
     │
     ▼
TaskObjectiveCompleter
     │
     ▼
TaskManager
```

게임 오브젝트가 Task의 전체 진행 로직을 직접 처리하지 않고 Objective 완료 요청만 전달하도록 역할을 분리했습니다.

### AreaVisitTrigger

플레이어가 특정 지역에 진입했을 때 해당 Objective를 완료하기 위한 Trigger입니다.

```text id="ud9szc"
Player
   │
   │ Enter Area
   ▼
AreaVisitTrigger
   │
   │ Complete Objective
   ▼
TaskManager
```

이를 통해 미니게임뿐 아니라 월드 이동 자체도 Task의 완료 조건으로 사용할 수 있도록 했습니다.

---

## 5. MiniGame Integration

Task System은 MiniGame Framework와 연결하여 사용할 수 있도록 구성했습니다.

```text id="ayopcl"
MiniGame
    │
    │ Success
    ▼
Objective Complete
    │
    ▼
TaskManager
    │
    ▼
Task Progress
```

여기서 중요한 점은 개별 미니게임이 Task 전체의 진행 상태를 직접 관리하지 않는다는 것입니다.

미니게임은 자신의 성공 조건을 판단하고, 해당 결과를 Objective 완료라는 형태로 전달합니다.

Task 진행 상태를 판단하는 책임은 `TaskManager`가 담당하도록 분리했습니다.

---

## 6. Multiple Objectives

하나의 Task에서 여러 Objective가 필요한 경우도 처리할 수 있도록 구성했습니다.

```text id="1sioaq"
Task
 │
 ├── Objective A ✓
 │
 ├── Objective B ✓
 │
 └── Objective C
          │
          ▼
       Not Complete
```

필요한 Objective가 모두 완료되었을 때 해당 Task가 완료될 수 있도록 Required Objective와 Completed Objective를 구분하여 관리했습니다.

이를 통해 단일 행동뿐만 아니라 여러 단계를 거쳐 진행되는 Task도 동일한 구조에서 표현할 수 있도록 했습니다.

---

## 7. Responsibility Separation

Task System을 구성하면서 각 시스템의 책임을 다음과 같이 분리했습니다.

```text id="y9bjbo"
MiniGame
  └── 자신의 성공 조건 판단

AreaVisitTrigger
  └── 플레이어 지역 진입 감지

TaskObjectiveCompleter
  └── Objective 완료 전달

TaskManager
  └── Task 진행 상태 관리

TaskData
  └── Task에 필요한 Objective 데이터 관리
```

이렇게 역할을 분리하여 새로운 콘텐츠가 추가되더라도 Task의 전체 진행 로직을 각각의 콘텐츠에 다시 구현하지 않도록 했습니다.

---

## 8. Extensibility

Task System을 공통 Objective 기반으로 구성하면서 서로 다른 콘텐츠를 동일한 흐름에 연결할 수 있게 되었습니다.

예를 들어 새로운 조건이 추가된다면:

```text id="pxtu04"
New Gameplay
     │
     ▼
New Objective
     │
     ▼
Existing TaskManager
```

와 같이 기존 Task 관리 구조를 유지하면서 완료 조건을 확장하는 방향으로 사용할 수 있습니다.

즉, Task System의 목적은 특정 미니게임을 위한 미션 시스템을 만드는 것이 아니라 여러 게임 콘텐츠에서 공통으로 사용할 수 있는 진행 관리 구조를 만드는 것이었습니다.

---

## 9. What I Learned

Task System을 구현하면서 게임 콘텐츠의 **성공 조건**과 게임 전체의 **진행 상태 관리**를 분리하는 것이 중요하다는 점을 경험했습니다.

처음에는 미니게임이 성공하면 해당 미니게임에서 바로 다음 진행을 처리하는 방식도 가능했지만, 미니게임과 Task 종류가 증가하면서 이러한 방식은 시스템 간 의존성을 증가시킬 수 있었습니다.

이를 Objective라는 공통 단위로 분리하면서 미니게임, 지역 방문 등 서로 다른 게임플레이를 하나의 Task 진행 구조에 연결할 수 있었습니다.

또한 각 클래스가 담당해야 할 책임을 나누면서 새로운 콘텐츠가 추가될 때 기존 코드의 변경 범위를 줄이는 방향으로 구조를 설계하는 경험을 할 수 있었습니다.