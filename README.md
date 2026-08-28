# DeliveryBotSim

1인칭 배달로봇 시뮬레이터 (Unity 6, 로우폴리). 키보드/게임패드로 개발하고, Logitech G27 등 스티어링 휠은 프로파일 에셋만 바꿔서 대응한다.

## 실행

1. Unity Hub → Open → 이 폴더 (Unity 6000.5.x)
2. 메뉴 `DeliveryBot > Build Greybox Scene` (씬이 없거나 다시 만들고 싶을 때)
3. `Assets/_Project/Scenes/City.unity` 열고 Play

## 조작

| 동작 | 키보드 | 게임패드 | 휠 (G27 프로파일) |
|---|---|---|---|
| 조향 | A/D, ←/→ | 왼쪽 스틱 | 핸들 축 |
| 가속 | W, ↑ | RT | 액셀 페달 |
| 브레이크 | S, ↓ | LT | 브레이크 페달 |
| 후진(누르고 가속) | Left Shift | X | button2 |
| 핸드브레이크 | Space | B | button3 |
| 상호작용 | E | A | trigger |
| 휠 디버그 오버레이 | F1 | | |
| 재시작 | R | | |

## G27 연결 시 할 일

1. 휠 연결 → Play → F1 → 각 축/버튼 값이 라이브로 표시됨
2. 핸들 돌려보고 페달 밟아보며 어떤 경로(`<Joystick>/stick/x`, `/rz`, `/slider` …)가 무엇인지 확인
3. `Assets/_Project/Settings/G27Profile.asset` 에서 경로·rest/pressed 값·회전 범위 수정 (코드 수정 불필요)
4. 포스 피드백은 Windows 빌드에서 Logitech SDK로 별도 추가 예정 (`docs/plan.md`)

## 구조

```
Assets/_Project/
  Scripts/
    Input/     DriveInputProvider(키보드+패드+휠 병합), SteeringWheelProfile, WheelDebugOverlay
    Vehicle/   RobotController(아케이드 주행), FirstPersonCamera
    Minimap/   MinimapFollow, SpriteFactory(원형 마스크/화살표 런타임 생성)
    Delivery/  DeliveryManager(픽업→배달 루프), DeliveryPoint
    UI/        DeliveryHUD, GameBootstrap
    Editor/    SceneBootstrapper(그레이박스 도시 씬 자동 생성)
  Scenes/ Settings/ Materials/   (생성물)
```
