# MiniGame Framework

## 1. Overview

Then Die에는 플레이어가 게임 진행 중 수행하는 여러 종류의 미니게임이 존재합니다.

구현한 미니게임은 다음과 같습니다.

- Temperature
- Potion
- Puzzle Picture
- Star Align

초기에는 개별 미니게임의 구현을 중심으로 개발했지만, 미니게임의 종류가 늘어나면서 실행 및 종료 흐름을 각 미니게임에서 개별적으로 관리하는 방식은 확장성과 유지보수 측면에서 비효율적이라고 판단했습니다.

이를 개선하기 위해 `MiniGameManager`를 중심으로 미니게임의 실행과 결과 처리를 관리하고, `MiniGameLauncher`와 `MiniGameType`을 통해 각 미니게임의 실행 진입점을 통일하는 구조로 개선했습니다.

---

## 2. Problem

초기 개발 단계에서는 Temperature, Potion, Puzzle Picture 등의 미니게임을 각각 구현하는 데 집중했습니다.

하지만 미니게임의 종류가 증가하면서 다음과 같은 문제가 발생했습니다.

### 개별 실행 구조

각 미니게임을 실행하는 오브젝트가 직접 특정 미니게임을 알고 실행하는 구조에서는 새로운 미니게임이 추가될 때마다 실행 측 코드도 함께 변경해야 했습니다.

```text
Object A ──────> Temperature

Object B ──────> Potion

Object C ──────> Puzzle

Object D ──────> StarAlign
```

미니게임 종류가 늘어날수록 실행 코드와 미니게임 사이의 의존 관계가 증가하게 됩니다.

### 결과 처리의 분산

각 미니게임에서 성공/실패 및 종료 처리를 개별적으로 관리하면 게임 진행 시스템에서 미니게임 결과를 일관된 방식으로 처리하기 어려워집니다.

특히 이후 Task 시스템과 미니게임을 연결하면서,

```text
미니게임 실행
    ↓
미니게임 완료
    ↓
Task Objective 완료
```

라는 공통 흐름이 필요해졌습니다.

---

## 3. Solution

미니게임 실행 구조를 다음과 같이 변경했습니다.

```text
Player Interaction
        │
        ▼
 MiniGameLauncher
        │
        │ MiniGameType
        ▼
 MiniGameManager
        │
        ├── Temperature
        ├── Potion
        ├── Puzzle Picture
        └── Star Align
```

`MiniGameLauncher`는 어떤 미니게임을 실행할 것인지 전달하고, 실제 미니게임의 생성 및 실행 흐름은 `MiniGameManager`에서 관리하도록 역할을 분리했습니다.

이를 통해 게임 오브젝트가 특정 미니게임 구현을 직접 관리하지 않아도 되도록 구성했습니다.

---

## 4. Core Components

### BaseMiniGame

각 미니게임에서 공통으로 필요한 실행 및 종료 흐름을 정의하기 위한 기반 클래스입니다.

개별 미니게임은 `BaseMiniGame`을 기반으로 자신의 게임 규칙만 구현하도록 구성했습니다.

```text
BaseMiniGame
    │
    ├── TemperatureMiniGame
    ├── PotionMiniGame
    ├── Puzzle Picture
    └── StarAlignMiniGame
```

이를 통해 MiniGameManager가 개별 미니게임의 세부 구현을 모두 알 필요 없이 공통된 방식으로 관리할 수 있도록 했습니다.

### MiniGameType

실행할 미니게임을 구분하기 위한 타입입니다.

```text
Temperature
Potion
PuzzlePicture
StarAlign
```

미니게임 실행 측에서는 구체적인 클래스 대신 `MiniGameType`을 통해 실행할 미니게임을 지정할 수 있도록 구성했습니다.

### MiniGameLauncher

플레이어와 미니게임 사이의 실행 진입점 역할을 담당합니다.

```text
Player
   │
   │ Interaction
   ▼
MiniGameLauncher
   │
   │ MiniGameType
   ▼
MiniGameManager
```

현재 프로젝트에서는 네트워크 및 미션 시스템과 통합되면서 Mirror의 `NetworkBehaviour`와 `MissionManager`를 통해 미션 시작 가능 여부를 확인하는 기능도 포함되어 있습니다.

포트폴리오 저장소에는 전체 `MissionManager` 구현이 포함되어 있지 않으며, 해당 부분은 원본 프로젝트와의 Integration Point입니다.

### MiniGameManager

미니게임 실행 흐름을 중앙에서 관리하는 역할을 담당합니다.

주요 역할은 다음과 같습니다.

- 미니게임 실행
- 현재 미니게임 관리
- 성공/실패 결과 처리
- 미니게임 종료
- 게임플레이 입력과 미니게임 입력 상태 관리

개별 시스템에서 미니게임의 생명주기를 직접 관리하지 않고 `MiniGameManager`를 통해 처리하도록 구성했습니다.

---

## 5. Implemented MiniGames

### Temperature

목표 온도에 현재 온도를 맞추는 미니게임입니다.

플레이어는 온도 증가/감소 버튼을 사용하며 버튼을 길게 누르는 입력도 처리할 수 있도록 구현했습니다.

**주요 구현**

- 목표 온도 설정
- 현재 온도 증가/감소
- 버튼 Hold 입력
- 목표 도달 판정
- 성공 처리

### Potion

여러 종류의 재료를 선택하여 주어진 목표 조합을 완성하는 미니게임입니다.

**주요 구현**

- Potion Type 관리
- 재료 입력 처리
- 목표 조합 판정
- 잘못된 입력 피드백
- 완료 상태 처리

### Puzzle Picture

회전된 퍼즐 조각을 올바른 방향으로 맞추는 미니게임입니다.

**주요 구현**

- Puzzle Piece 회전
- 초기 랜덤 회전
- 정답 방향 판정
- 전체 Puzzle 완료 판정

### Star Align

움직이는 요소와 목표 지점의 타이밍을 맞추는 미니게임입니다.

**주요 구현**

- 목표 위치 관리
- 플레이어 입력 판정
- 성공 범위 판정
- 진행 상태 관리
- 최종 성공 처리

---

## 6. Task System Integration

미니게임 구조를 공통화한 이후 Task 시스템과 연결했습니다.

```text
MiniGameLauncher
        │
        ▼
MiniGameManager
        │
        ▼
   MiniGame
        │
        │ Success
        ▼
Task Objective
        │
        ▼
  TaskManager
```

미니게임 자체는 자신의 성공 조건을 판단하고, 완료 결과는 상위 시스템에서 Task 진행 상태와 연결할 수 있도록 역할을 분리했습니다.

이를 통해 미니게임 로직과 게임 진행 로직이 직접 강하게 결합되는 것을 줄이고자 했습니다.

---

## 7. Development Process

미니게임 시스템은 처음부터 현재 구조로 설계된 것이 아니라 실제 기능을 추가하면서 단계적으로 개선했습니다.

```text
Temperature MiniGame
        │
        ▼
Puzzle / Potion 추가
        │
        ▼
Star Align 추가
        │
        ▼
Task System 연동
        │
        ▼
MiniGameManager 중심 구조로 개선
        │
        ▼
Network / Mission System 연동
```

초기에는 개별 미니게임 구현에 집중했지만, 기능이 증가하면서 반복되는 실행 및 결과 처리 흐름을 확인했고 이를 공통 시스템으로 분리했습니다.

이후 Task 및 멀티플레이 게임 시스템과 연결하면서 미니게임이 독립적인 콘텐츠이면서도 게임 전체 진행 흐름과 연동될 수 있도록 구조를 확장했습니다.

---

## 8. What I Learned

이 작업을 통해 단순히 개별 기능을 구현하는 것뿐만 아니라 기능의 수가 증가했을 때 공통되는 책임을 찾아 별도의 시스템으로 분리하는 과정의 중요성을 경험했습니다.

특히 미니게임 자체의 규칙과 미니게임을 실행하고 관리하는 책임을 분리하면서 새로운 미니게임을 추가할 때 기존 실행 흐름에 미치는 영향을 줄일 수 있었습니다.

또한 이후 Task 및 네트워크 시스템과 연동하면서 독립적인 기능을 전체 게임 흐름에 통합할 때 시스템 간 책임과 의존 관계를 명확하게 관리하는 것이 중요하다는 점을 경험했습니다.