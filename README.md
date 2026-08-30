# delivery-simulator

로우폴리 3D 배달로봇 시뮬레이터 (Unity 6). 키보드/게임패드로 개발하고, Logitech G27 등 스티어링 휠은 프로파일 에셋만 바꿔서 대응한다. 모든 모델은 프리미티브를 코드로 조립해 만들어 외부 에셋 없이 재현된다.

## 실행

1. Unity Hub → Open → 이 폴더 (Unity 6000.5.x)
2. `Assets/_Project/Scenes/City.unity` 열고 ▶ Play
3. **키가 안 먹으면 Game 뷰를 한 번 클릭**해서 키보드 포커스를 준다 (게임패드는 포커스 없이도 동작)
4. 씬을 다시 만들고 싶으면 메뉴 `DeliveryBot > Build City Scene`
5. 패드/휠이 인식되는지 보려면 메뉴 `DeliveryBot > Log Input Devices` (Console에 출력)

## 조작

| 동작 | 키보드 | 게임패드 | 휠 (G27 프로파일) |
|---|---|---|---|
| 조향 | A/D, ←/→ | 왼쪽 스틱 | 핸들 축 |
| 가속 | W, ↑ | RT | 액셀 페달 |
| 브레이크 → 멈춘 뒤 계속 누르면 후진 | S, ↓ | LT | 브레이크 페달 |
| 후진 모드(누르고 가속) | Shift | – | button2 |
| 핸드브레이크 | Space | – | button3 |
| 시점 전환 (3인칭 ↔ 1인칭) | V | R스틱 클릭 | button4 |
| 상호작용 | E | A | trigger |
| 조작법 카드 | H | | |
| 입력 디버그 오버레이 (연결된 모든 장치 표시) | F1 | | |
| 재시작 | R | | |

## 게임 루프

- 시작할 때 로봇 위치·방향이 랜덤. **가게(치킨·피자·카페·분식·편의점·약국·베이커리·꽃집·마트·서점)** 중 하나가 픽업 지점으로 배정된다(최소 35 m).
- 가게 문 앞(반경 6 m)에 천천히 도착해 **A(패드) / E(키보드)** 를 누르면 픽업. 모든 문 앞과 횡단보도 양 끝에 **진입로(경사로)** 가 있어 인도로 올라갈 수 있다(바퀴 콜라이더가 12 cm 턱도 넘음). 그러면 70 m 이상 떨어진 **집(주택 또는 아파트 경비실)** 이 배달지로 배정된다.
- 집 문 앞에서 다시 A/E → 배달 완료, 다음 주문. 화면 아래에 "[A] / [E] 픽업하기" 안내가 뜬다.
- 차량/보행자와 부딪히면 +5초, 벽에 세게 부딪히면 +2초 페널티. 로봇이 길을 막고 있으면 뒤차가 경적을 울린다.

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
    Editor/    (DeliveryBot.Editor.asmdef) SceneBootstrapper, CityBuilder(블록 유형: 상업/주거/아파트/공원), StorefrontFactory(업종별 파사드),
               HouseFactory(주택·아파트), PointMarkerFactory, PropFactory, RobotFactory, VehicleFactory, HudBuilder, BuildKit, SnapshotTool
  Tests/EditMode/  AxisMapping, RoadGraph, JobPicker 테스트
  Tests/PlayMode/  City 씬 실주행(전진/후진/조향), NPC 이동, 픽업→배달 상호작용 테스트
  Scenes/ Prefabs/ Materials/ Settings/   (Build City Scene 생성물)
```

## 헤드리스 검증

```
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1-arm64/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -nographics -quit -projectPath . -executeMethod DeliveryBot.EditorTools.SceneBootstrapper.Build
$UNITY -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
$UNITY -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults playmode.xml   # City 씬에서 실제 주행 검증
$UNITY -batchmode -nographics -quit -projectPath . -buildWindows64Player Builds/Windows/DeliveryBotSim.exe
$UNITY -batchmode -quit -projectPath . -executeMethod DeliveryBot.EditorTools.SnapshotTool.Capture -snapshotDir Snapshots   # 가게/집/거리 PNG 캡처
```
