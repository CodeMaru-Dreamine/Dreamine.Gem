# Dreamine.Gem

[![CI](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/actions/workflows/ci.yml/badge.svg)](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/actions/workflows/ci.yml)
[![품질 게이트](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.Gem&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.Gem) [![보안 등급](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.Gem&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.Gem) [![테스트 커버리지](https://sonarcloud.io/api/project_badges/measure?project=CodeMaru-Dreamine_Dreamine.Gem&metric=coverage)](https://sonarcloud.io/summary/new_code?id=CodeMaru-Dreamine_Dreamine.Gem)

Dreamine.Gem은 Dreamine SECS-II/HSMS Stack 위에 만든 Code-first GEM
Equipment/Host 구현 Surface입니다.

[➡️ English Version](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/README.md)

## 설치와 시작

```powershell
dotnet add package Dreamine.Gem
```

Dreamine SECS-II/HSMS Runtime 위에서 Typed GEM Host/Equipment Service가 필요할 때 선택합니다. Equipment와 Host를 별도 Process로 실행하는 [Package-first QuickStart](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/QUICKSTART_KO.md)부터 시작하고, Full Source Workspace를 검증할 때만 `-p:UseLocalDreamineSources=true`를 사용하십시오.

## 상태 경계

| 범위 | 상태 | Evidence 경계 |
|---|---|---|
| `E30-0611 derived subset profile v1` 구현 Surface | `IMPLEMENTED_UNVERIFIED` | 동결된 Source 범위이며 적합성 판정이 아닙니다. |
| 로컬 Unit 및 수작업 Wire Fixture Evidence | `PASS` | 구조, 상태, ACK, 거부, No-mutation, Correlation을 확인합니다. |
| 로컬 별도 Host/Equipment Process 실제 TCP Evidence | `PASS` | 공개 QuickStart가 고정 Dialogue 전체와 명시적 Equipment-origin Direction을 실행합니다. |
| 외부 Simulator 또는 생산 장비 | `NOT_RUN` | 로컬 Evidence로 외부·Field 결과를 승격하지 않습니다. |
| E37.1 적합성 | `BLOCKED_STANDARD` | 필요한 라이선스 Revision을 사용할 수 없습니다. |

Target Source는 E30-0611과 E5-0813입니다. 최신 Revision 적합성, 인증 또는 외부
상호운용을 주장하지 않습니다.

## 공개 구현 Surface

- `GemEquipmentProfileBuilder`는 검증된 Code-first Equipment Profile을
  Freeze하며 `CreateContext`를 호출할 때마다 격리된 Mutable Runtime 상태를 만듭니다.
- `E30DemoEquipmentProfile.Create()`는 고객 장비 의미가 없는 작은 범용 공개 Demo
  Profile을 제공합니다.
- `E30HostClient`는 고정 Host Surface의 형식화된 Call과 Outcome을 제공합니다.
  `E30EquipmentRouter`는 정확한 Equipment-side Dispatch와 명시적
  Equipment-origin Operation을 담당합니다.
- `E30IdentifierPolicy`는 CEID/RPTID/VID/SVID/DVID/ECID/ALID/DATAID별 독립
  정수 형식을 보존하며 Raw ACK 값도 관찰할 수 있습니다.
- `HsmsGemTransport`는 Application 소유 `ISecsMessageSession`을 연결합니다.
  Runtime은 전달받은 Session을 소유하거나 Dispose하지 않습니다.
- Variable, Constant, Event/Report Snapshot, Alarm, Remote Command, Process
  Program, Clock, 제한 용량 메모리 Spool 도메인 서비스도 제공합니다. 도메인
  서비스의 존재는 Wire 지원을 뜻하지 않습니다.

## 고정 Dialogue Manifest

정확한 Profile 이름은 `E30-0611 derived subset profile v1`입니다. 20개 정상
Primary/Secondary Dialogue Definition은 다음과 같습니다.

| 공개 Demo 실행의 Direction | Dialogue |
|---|---|
| Host Request → Equipment Response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment Primary → Host Response | S5F1/F2, S6F11/F12 |

같은 실제 TCP 실행에서 Router의 Typed Operation으로 Equipment-initiated S1F1/F2와
S2F17/F18도 확인합니다. Router는 Equipment-initiated S1F13/F14도 공개합니다.
Direction은 명시적이며 20개 Manifest를 모든 Dialogue의 양방향 지원으로 해석하면
안 됩니다.

## Code-first Equipment 조립

```csharp
var profile = E30DemoEquipmentProfile.Create();
var context = profile.CreateContext(new HsmsGemTransport(equipmentSession));

await using var router = new E30EquipmentRouter(
    equipmentSession,
    context,
    new E30EquipmentRouterOptions
    {
        CommandCompletionEvents = new Dictionary<string, ulong>
        {
            [E30DemoEquipmentProfile.StartCommand] =
                E30DemoEquipmentProfile.CommandCompletedEventId
        }
    });
```

Host Process는 선택한 Profile의 Identifier Policy를 사용합니다.

```csharp
using var host = new E30HostClient(
    hostSession,
    new E30IdentifierPolicy(profile.IdentifierFormats));

var result = await host.ReadStatusAsync(
    [E30DemoEquipmentProfile.EquipmentStateVariableId],
    cancellationToken);
```

Session 연결, Select, Dispatcher 수명, 연결 해제는 Application 책임입니다. 별도
Process 명령은 [빠른 시작](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/QUICKSTART_KO.md)을 참고하십시오.

## WPF Demo 선택

공개 WPF Workbench의 기본값 **E30-0611 derived subset profile v1 (Demo)**는
`E30DemoEquipmentProfile.Create()`와 `E30EquipmentRouter`를 사용합니다. 대안
**Educational basic responder (Demo-only, not GEM)**은 일곱 Pair 교육용
Fallback입니다. 선택은 상호 배타적이므로 고정 Router와 Fallback Responder가 동시에
등록되지 않습니다. [Workbench README](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit/blob/main/20_SOURCES/998.%20DEMO/000.%20Sample/010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README_KO.md)를 참고하십시오.

## 명시적 제외와 차단된 의미

- 알 수 없는 RPTID의 S6F19/F20은 `BLOCKED_STANDARD`입니다.
- S2F35 Empty Outer-list와 Empty RPTID-list Unlink/Delete Variant는
  `BLOCKED_STANDARD`이며 v1은 상태 변경 없이 Transaction을 종료합니다.
- S9F1, S9F9, S9F13은 현재 Safe Provider/Router 경계에서 임의 추정 없이 구성하는 데
  필요한 Source Context를 제공하지 않으므로 고정 v1에서
  `INTENTIONALLY_EXCLUDED`입니다.
- Multi-block S2F39/F40·S6F5/F6, Trace, Limits, Wire Spooling, S7 Process
  Program Wire Service, S10 Terminal Service, Material Handling, Enhanced
  Remote Command는 `INTENTIONALLY_EXCLUDED`입니다.

[알려진 제한](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/KNOWN_LIMITATIONS.md), [요구사항 추적표](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/docs/SEMI_REQUIREMENTS_TRACE.md),
[공개 API Review](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/docs/API_REVIEW.md)를 참고하십시오.

## 라이선스

MIT.
