# 빠른 시작

`Dreamine.Gem.QuickStart`는 공개 범용 Demo Profile을 실제 TCP/HSMS의 별도 두
Process로 실행합니다. Equipment를 먼저 시작하고 Host를 실행하십시오.

정확한 고정 범위 Label은 `E30-0611 derived subset profile v1`입니다. 로컬 실행
예제이며 최신 Revision 적합성이나 외부 상호운용 Evidence가 아닙니다.

## 1. Passive Equipment Process 시작

`Dreamine.Gem` Repository Root에서 실행합니다.

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart -- `
  --role equipment `
  --host 127.0.0.1 `
  --port 5000 `
  --session-id 37 `
  --timeout-seconds 45 `
  --evidence equipment-evidence.json
```

Equipment Role은 `E30DemoEquipmentProfile`, 격리된 `GemEquipmentContext`,
`E30EquipmentRouter`를 만듭니다. Select와 Communication 수립을 기다린 뒤 명시적
Equipment-origin Check를 실행하고 Host Request를 처리합니다.

## 2. Active Host Process 시작

두 번째 Terminal에서 같은 Endpoint와 0이 아닌 Session ID를 사용합니다.

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart -- `
  --role host `
  --host 127.0.0.1 `
  --port 5000 `
  --session-id 37 `
  --timeout-seconds 45 `
  --evidence host-evidence.json
```

Host Role은 `E30HostClient`로 Typed Success, Timeout, Cancellation, Raw ACK,
W-bit, System Bytes Correlation, 선택한 Session ID를 확인합니다. Process는 제한
시간 안에 Evidence Check가 모두 통과해야 `0`을 반환합니다. 설정·Runtime 실패는
`1`, 제한 시간 초과·취소는 `2`입니다.

## 실행되는 Dialogue Direction

고정 Manifest의 20개 Entry를 모두 실행합니다.

| Direction | Dialogue |
|---|---|
| Host Request → Equipment Response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment Primary → Host Response | S5F1/F2, S6F11/F12 |

명시적 Direction Check로 Equipment-initiated S1F1/F2와 S2F17/F18도 실행합니다.
`E30EquipmentRouter.EstablishCommunicationsAsync`는 Equipment-initiated
S1F13/F14를 별도로 공개합니다. 모든 Manifest Entry가 양방향 구현됐다고 추론하면
안 됩니다.

## 결과 해석

| 결과 Surface | 상태 |
|---|---|
| 고정 구현 Surface | `IMPLEMENTED_UNVERIFIED` |
| 로컬 Unit 및 수작업 Fixture Evidence | `PASS` |
| 두 Role이 모두 `0`을 반환한 별도 Process 실제 TCP 실행 | `PASS` |
| 외부 Simulator 또는 생산 장비 | `NOT_RUN` |
| E37.1 적합성 | `BLOCKED_STANDARD` |

알 수 없는 RPTID의 S6F19/F20 표현과 S2F35 Empty-list Unlink/Delete 의미는
`BLOCKED_STANDARD`입니다. S9F1, S9F9, S9F13, Multi-block 및
[알려진 제한](KNOWN_LIMITATIONS.md)에 적은 다른 기능 계열은 고정 v1에서
`INTENTIONALLY_EXCLUDED`입니다.

## WPF Workbench

Workbench에서 같은 공개 Demo Profile을 선택할 수 있습니다. 대안 교육용 Fallback은
Demo-only이며 GEM이 아니고 E30 Router와 상호 배타적입니다.
[Workbench README](../../998.%20DEMO/000.%20Sample/010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README_KO.md)를 참고하십시오.

조립 방식은 [기본 README](README_KO.md), Normative 판정과 Evidence 분리는
[요구사항 추적표](docs/SEMI_REQUIREMENTS_TRACE.md)를 참고하십시오.
