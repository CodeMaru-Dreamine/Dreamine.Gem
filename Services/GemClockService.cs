using System.Globalization;
using Dreamine.Gem.Abstractions.Interfaces;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 시스템 시계를 변경하지 않고 논리 오프셋을 적용하는 GEM 시계입니다. \endif \if EN Provides a GEM clock using a logical offset without changing the system clock. \endif</summary>
public sealed class GemClockService : IGemClockService
{
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private TimeSpan _offset;
    /// <summary>\if KO 시간 공급자로 시계를 만듭니다. \endif \if EN Creates the clock with a time provider. \endif</summary>
    public GemClockService(TimeProvider? timeProvider = null) => _timeProvider = timeProvider ?? TimeProvider.System;
    /// <inheritdoc />
    public DateTimeOffset GetUtcNow() { lock (_gate) return _timeProvider.GetUtcNow() + _offset; }
    /// <inheritdoc />
    public void SetUtcNow(DateTimeOffset value) { lock (_gate) _offset = value.ToUniversalTime() - _timeProvider.GetUtcNow(); }
    /// <inheritdoc />
    public string Format(bool fourDigitYear = true) => GetUtcNow().UtcDateTime.ToString(fourDigitYear ? "yyyyMMddHHmmssff" : "yyMMddHHmmss", CultureInfo.InvariantCulture);
}
