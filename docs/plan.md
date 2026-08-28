# 제작 계획

## 목표
로지텍 G27(또는 키보드)로 조작하는 1인칭 배달로봇 시뮬레이터. 맥에서 개발, Windows 빌드 배포.

## Phase 0 — 환경 ✅ (2026-08-28)
- Unity Hub + Unity 6000.5.10f1 (Mac IL2CPP, Windows Mono 모듈), git-lfs
- 프로젝트 뼈대, 입력 추상화, 씬 자동 생성

## Phase 1 — 움직이는 로봇
- [x] 아케이드 주행(RobotController), 1인칭 카메라
- [x] 입력 추상화: 키보드/패드 즉시, 휠은 프로파일 에셋
- [ ] 주행 감 튜닝 (속도, 회전, 카메라 흔들림)
- [ ] 로봇 모델 교체 (Kenney 등 로우폴리)

## Phase 2 — 도시 + 미니맵
- [x] 그레이박스 도시 (도로 그리드 + 박스 건물)
- [x] 원형 미니맵 (RenderTexture + Mask)
- [ ] 로우폴리 건물/도로/소품 에셋 적용
- [ ] 미니맵에 목적지 아이콘 강조

## Phase 3 — 배달 루프
- [x] 픽업 → 배달 → 완료 → 다음 주문, 타이머/점수
- [ ] 보행자/장애물, 충돌 페널티
- [ ] 결과 화면, 메뉴

## Phase 4 — Windows + G27
- [ ] Windows 빌드 실행 확인
- [ ] G27 실기기로 프로파일 확정 (F1 오버레이)
- [ ] Logitech Steering Wheel SDK로 포스 피드백 (Windows 전용)

## 리스크
- G27 macOS 미지원: 입력만 가능, FFB 불가 → Windows에서 검증
- G27 페달 combined 모드 여부 → LGS에서 "Combined pedals" 해제
- Logitech SDK ↔ Unity 6 호환 미확인
