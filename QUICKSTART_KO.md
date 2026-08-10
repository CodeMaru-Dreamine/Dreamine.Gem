# 빠른 시작

빌드 가능한 도메인 서비스 샘플은 Status Variable과 Equipment Constant 등록, Event/Report 수집, Alarm Set/Clear, 검증된 Remote Command 실행을 보여 줍니다.

```powershell
dotnet run --project samples/Dreamine.Gem.QuickStart
```

S1F13/S1F14 및 S1F1/S1F2 wire 처리는 연결·Select된 `HsmsSession` 위에 `HsmsGemTransport`와 `GemRuntime`을 조립합니다. 세션 소유권은 호출자에게 있습니다. 도메인 서비스의 존재는 관련 GEM Stream/Function 전체가 wire에 매핑됐다는 의미가 아닙니다.

이 샘플 상태는 Unit/Sample Tested이며 외부 상호운용 결과와 분리합니다. [KNOWN_LIMITATIONS.md](KNOWN_LIMITATIONS.md)를 확인하십시오.
