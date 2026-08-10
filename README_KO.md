# Dreamine.Gem

Dreamine.Gem은 Dreamine SECS-II/HSMS 스택 위에서 동작하는 테스트 가능한
GEM 장비 런타임의 1차 구현입니다.

[➡️ English Version](https://github.com/CodeMaru-Dreamine/Dreamine.Gem/blob/main/README.md)

## 구현 경계

- Communication, Control, Equipment Processing 상태 모델
- 형식화된 Variable 및 Equipment Constant 등록소
- 동적 Report 연결과 Collection Event 스냅샷
- Alarm, Remote Command, 불투명 Process Program, Clock, 제한 용량 메모리
  Spool 서비스
- 장비·호스트 주도 S1F13/S1F14 통신 수립
- S1F1/S1F2 온라인 식별
- `Dreamine.Secs.Com.Hsms.HsmsSession`을 연결하는 `HsmsGemTransport`

공개 도메인 값은 불변 정의와 형식화된 `SecsItem`을 사용합니다. 비동기 경계에는
시간과 취소를 주입할 수 있습니다. 런타임은 전달받은 연결을 소유하거나 해제하지
않습니다.

## 명시적 제한

이번 1차 구현의 로컬 Normative 근거는 SEMI E30-0611입니다. SEMI 공식 카탈로그에는
더 최신 Revision이 있으므로, 이 패키지는 현재판 적합성·인증·상호운용성을 주장하지
않습니다.

기능별 도메인 서비스가 존재한다는 사실은 관련 SECS Stream/Function 전체를
지원한다는 뜻이 아닙니다. 현재 wire 처리는 S1F13/S1F14와 S1F1/S1F2로 제한됩니다.
영속 Spooling, Trace Collection, Limits Monitoring, Terminal Services, E42 Recipe,
E139 RaP는 이번 범위 밖입니다.

[요구사항 추적표](./docs/SEMI_REQUIREMENTS_TRACE.md)와
[익명화된 로컬 문서 인벤토리](./docs/LOCAL_DOCUMENT_INVENTORY.md)를 참고하십시오.

## 조립 예시

```csharp
await using var session = new HsmsSession(options);
var transport = new HsmsGemTransport(session, options.SessionId);
var gem = new GemRuntime(transport, new GemEquipmentIdentity("MODEL", "1.0"));
```

연결 선택과 생명주기는 호출자가 관리합니다.

## 라이선스

MIT.
