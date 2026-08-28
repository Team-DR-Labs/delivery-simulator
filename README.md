# delivery-simulator

로우폴리 3D 배달로봇 시뮬레이터 (Unity 6). 키보드/게임패드로 개발하고, Logitech G27 등 스티어링 휠은 프로파일 에셋만 바꿔서 대응한다. 모든 모델은 프리미티브를 코드로 조립해 만들어 외부 에셋 없이 재현된다.

## 실행

1. Unity Hub → Open → 이 폴더 (Unity 6000.5.x)
2. `Assets/_Project/Scenes/City.unity` 열고 ▶ Play
3. **키가 안 먹으면 Game 뷰를 한 번 클릭**해서 키보드 포커스를 준다
4. 씬을 다시 만들고 싶으면 메뉴 `DeliveryBot > Build City Scene`

## 조작

| 동작 | 키보드 | 게임패드 | 휠 (G27 프로파일) |
|---|---|---|---|
| 조향 | A/D, ←/→ | 왼쪽 스틱 | 핸들 축 |
| 가속 | W, ↑ | RT | 액셀 페달 |
| 브레이크 | S, ↓ | LT | 브레이크 페달 |
| 후진(누르고 가속) | Shift | X | button2 |
| 핸드브레이크 | Space | B | button3 |
| 시점 전환 (3인칭 ↔ 1인칭) | V | R스틱 클릭 | button4 |
| 상호작용 | E | A | trigger |
| 조작법 카드 | H | | |
| 입력 디버그 오버레이 | F1 | | |
| 재시작 | R | | |

## 게임 루프

- 시작할 때 로봇 위치·방향이 랜덤, 30개 상점 중 픽업 지점이 랜덤으로 배정된다(최소 35 m).
- 픽업하면 70 m 이상 떨어진 배달 지점이 배정되고, 완료하면 다음 주문.
- 차량/보행자와 부딪히면 +5초, 벽에 세게 부딪히면 +2초 페널티.

## G27 연결 시 할 일

1. 휠 연결 → Play → F1 → 각 축/버튼 값이 라이브로 표시됨
2. 핸들·페달을 움직여 어떤 경로(`<Joystick>/stick/x`, `/rz`, `/slider` …)가 무엇인지 확인
3. `Assets/_Project/Settings/G27Profile.asset` 에서 경로·rest/pressed 값·회전 범위 수정 (코드 수정 불필요)
4. 포스 피드백은 Windows 빌드에서 Logitech SDK로 별도 추가 예정 (`docs/plan.md`)

## 구조

```
Assets/_Project/
  Scripts/  (DeliveryBot.Runtime.asmdef)
    Input/     DriveInputProvider(키보드+패드+휠 병합, 레거시 폴백), SteeringWheelProfile, WheelDebugOverlay
    Vehicle/   RobotController(아케이드 주행), WheelVisuals, RobotCargoVisual
    Camera/    CameraRig(3인칭/1인칭, 충돌 회피, 셰이크)
    World/     RoadGraph(도로 그래프, 순수 C#), CityLayout, Billboard, PulseScale
    Traffic/   TrafficCar, TrafficSpawner, Pedestrian, PedestrianSpawner
    Delivery/  DeliveryManager, DeliveryPoint, JobPicker(순수 로직)
    Audio/     ProceduralAudio(코드 생성 사운드), RobotAudio, SfxPlayer
    UI/        DeliveryHUD, HudToast, ControlsHint, GameFeedback, ConfettiFactory
    Editor/    (DeliveryBot.Editor.asmdef) SceneBootstrapper, CityBuilder, PropFactory, RobotFactory, VehicleFactory, DeliveryPointFactory, HudBuilder, BuildKit
  Tests/EditMode/  AxisMapping, RoadGraph, JobPicker 테스트
  Scenes/ Prefabs/ Materials/ Settings/   (Build City Scene 생성물)
```

## 헤드리스 검증

```
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1-arm64/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -nographics -quit -projectPath . -executeMethod DeliveryBot.EditorTools.SceneBootstrapper.Build
$UNITY -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
$UNITY -batchmode -nographics -quit -projectPath . -buildWindows64Player Builds/Windows/DeliveryBotSim.exe
```
