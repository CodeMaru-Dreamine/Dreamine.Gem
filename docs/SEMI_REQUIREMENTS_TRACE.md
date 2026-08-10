# GEM 요구사항 추적표

기준일: 2026-08-10

이번 1차 구현의 Normative 기준은 로컬에서 보유하고 표지·목차·관련 절을 확인한
SEMI E30-0611 한국어 참고본이다. 영문본이 권위본이며 공식 Store에서 현재판은
E30-0526으로 확인되므로, 이 구현은 현재판 적합성·인증 또는 외부 상호운용성을
주장하지 않는다.

증거 수준:

- `Normative`: 보유한 SEMI 원문/공식 번역 참고본의 조항에서 확인
- `Official Public`: SEMI 공식 Store의 공개 설명·Revision 정보에서 확인
- `Experimental`: 확인된 개념을 이용한 자체 확장 또는 내부 통합
- `Blocked`: 규범 문서가 더 필요함

## 문서 기준

| 표준 | 로컬 | 공식 웹 대조 | 이번 적용 |
|---|---|---|---|
| SEMI E30 | E30-0611, 128쪽 | E30-0526 Current; 로컬판 Superseded | E30-0611 기반 1차 범위 |
| SEMI E5 | E5-0813 | 최신판 재검증 필요 | SECS-II 데이터 모델/메시지 계약 |
| SEMI E37 | E37-0413 | E37-0222가 공개 Revision 이력상 최신 | 하위 HSMS 전송 계층 |
| SEMI E37.1 | 원문 없음 | E37.1-0819가 E37 포함 표준 | `provisional`; GEM 적합성 근거로 사용하지 않음 |
| SEMI E139 | 원문 없음 | E30의 참조 표준 | RaP 구현 `Blocked` |

## 기본 GEM 요구사항

| 요구사항 | E30-0611 근거 | 관련 API/구현 | 테스트 | 상태 |
|---|---|---|---|---|
| Communication State Model | §3.2, Figure 4, Table 1 | `GemCommunicationStateMachine` | 장비·호스트 대기/수락/재시도/통신 손실/잘못된 전이 | 구현·테스트 |
| Equipment Processing States | §3.4, Figure 6, Table 3 | `GemProcessingStateMachine` | 정상/일시정지/중단/잘못된 전이 | 구현·테스트 |
| 장비 주도 S1F13/S1F14 | §4.1.5.2 | `GemProtocolEngine.EstablishCommunicationsAsync` | 메시지 구조/수락/거부/상관 오류 | 구현·테스트 |
| 호스트 주도 S1F13 응답 | §4.1.5.1 | `GemProtocolEngine.HandleAsync` | S1F13→S1F14/MDLN/SOFTREV | 구현·테스트 |
| Event Notification | §4.2.1.1, S6F11/F12 | `GemEventReportService` 도메인 | enable/link/report snapshot | 도메인 구현·테스트; wire 제외 |
| Online Identification | §4.2.6, S1F1/F2 | `GemProtocolEngine` | MDLN/SOFTREV 구조/통신 상태 제한 | 구현·테스트 |
| Error Messages | §4.9, S9F1/3/5/7/9/11/13 | 없음 | 미지원 Stream에 응답하지 않음만 검증 | 이번 wire 범위 제외 |
| Operator-Initiated Control | §4.12.5.1 | `GemControlStateMachine` | offline/attempt/local/remote 전이 | 구현·테스트 |
| 메시지 문서화 | §8.4 | README/본 추적표 | 문서 존재 검사 | Normative |

## 선택 GEM 기능

| 기능 | E30-0611 근거 | 관련 API/구현 | 테스트 | 상태 |
|---|---|---|---|---|
| Establish Communications | §4.1 | 상태 머신/프로토콜 엔진 | 재시도/취소/timeout | Normative |
| Dynamic Event Report | §4.2.1.2, S2F33–F38 | `GemEventReportService` | 정의/연결/해제/삭제/중복 VID 수집/disable/cancellation | 도메인 구현·테스트; wire 제외 |
| Variable Data Collection | §4.2.2, S6F19/F20 | `GemVariableCatalog` | 형식화 값/미등록 VID/필터 | 도메인 구현; wire 제외 |
| Status Data Collection | §4.2.5, S1F3/F4/F11/F12 | `GemVariableCatalog` | SVID 도메인 조회 | 도메인 구현; wire 제외 |
| Alarm Management | §4.3, S5F1–F6 | `GemAlarmService` | set/clear, 중복 변경, 현재 목록, enable 독립성 | 도메인 구현·테스트; wire 제외 |
| Remote Control | §4.4, S2F41/F42 | `GemRemoteCommandService` | Online Remote 정책/매개변수/예외/취소/주입 시간 timeout | 도메인 구현·테스트; wire 제외 |
| Equipment Constants | §4.5, S2F13–F16/F29/F30 | `GemEquipmentConstantService` | min/max 메타데이터/값·제어 상태 검증/원자적 변경 | 도메인 구현·테스트; wire 제외 |
| Process Program | §4.6.2.7, §4.6.3–§4.6.4 | `GemProcessProgramService` | 방어 복사/목록/조회/삭제 | 도메인 구현; wire 제외 |
| E42 Recipe | §4.6.2.8 | 없음 | 없음 | Blocked — E42 원문 필요 |
| E139 Recipe/RaP | §4.6.1.3, §4.6.2.3 | 없음 | 없음 | Blocked — E139 원문 필요 |
| Clock | §4.10, S2F17/F18/F31/F32 | `GemClockService` | 주입 시간/12·16자리 형식/논리 설정 | 도메인 구현; wire 제외 |
| Spooling | §4.11, Figure 11, Table 7 | `GemSpoolService` | 적재/overwrite/순서/전송 실패 보존 | Experimental 메모리 도메인; wire·영속 제외 |
| Trace Data Collection | §4.2.3 | 공개 계약 없음 | 없음 | 이번 1차 범위 제외 |
| Limits Monitoring | §4.2.4 | 공개 계약 없음 | 없음 | 이번 1차 범위 제외 |
| Material Movement | §4.7 | GEM300에서 다룸 | 없음 | 이번 GEM Core 범위 제외 |
| Terminal Services | §4.8 | 공개 계약 없음 | 없음 | 이번 1차 범위 제외 |

## 구현 제한

- 메시지 wire mapping은 본 추적표에 명시된 S1 기본 시나리오부터 제공한다. 선택
  기능의 도메인 서비스가 존재한다는 사실만으로 해당 Stream/Function 전체를
  지원한다고 해석하지 않는다.
- 내부 Loopback 테스트는 독립 장비/상용 Simulator 상호운용 시험을 대체하지 않는다.
- E30-0526의 변경점은 원문을 확보하기 전까지 `Blocked`다.
- E37.1 원문이 없으므로 HSMS-SS 범위는 SECS 계층 문서와 같이 provisional이다.

## 1차 구현 검증 결과

- `Dreamine.Gem.Abstractions.Tests`: 9개 통과
- `Dreamine.Gem.Tests`: 35개 통과(실제 TCP/HSMS 내부 Loopback 1개 포함)
- 두 라이브러리 Release 빌드: 경고 0, 오류 0
- 독립 상용 Simulator/인증 시험: 수행하지 않음
