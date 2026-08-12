using System.Collections.ObjectModel;

namespace Dreamine.Gem.Protocol.E30;

/// <summary>\if KO E30 장비 router의 bounded queue와 single-block 경계를 설정합니다. \endif \if EN Configures the bounded queue and single-block boundary of an E30 equipment router. \endif</summary>
public sealed class E30EquipmentRouterOptions
{
    /// <summary>\if KO 명령 실행 queue 용량입니다. \endif \if EN Gets the command-work queue capacity. \endif</summary>
    public int CommandQueueCapacity { get; init; } = 64;

    /// <summary>\if KO 한 명령 실행 제한 시간입니다. \endif \if EN Gets the execution timeout of one command. \endif</summary>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>\if KO v1에서 허용하는 단일 block body 최대 바이트 수입니다. \endif \if EN Gets the maximum single-block body size permitted by v1. \endif</summary>
    public int MaximumSingleBlockBodyBytes { get; init; } = 1_048_576;

    /// <summary>\if KO 명령 이름별 완료 CEID입니다. 명령 승인 HCACK와 별도로 실행 후 발생합니다. \endif \if EN Gets completion CEIDs by command name; they are emitted after execution separately from acceptance HCACK. \endif</summary>
    public IReadOnlyDictionary<string, ulong> CommandCompletionEvents { get; init; } =
        new ReadOnlyDictionary<string, ulong>(new Dictionary<string, ulong>(StringComparer.Ordinal));

    internal void Validate()
    {
        if (CommandQueueCapacity is < 1 or > 65_536) throw new ArgumentOutOfRangeException(nameof(CommandQueueCapacity));
        if (CommandTimeout <= TimeSpan.Zero || CommandTimeout > TimeSpan.FromHours(24)) throw new ArgumentOutOfRangeException(nameof(CommandTimeout));
        if (MaximumSingleBlockBodyBytes is < 1 or > 16_777_215) throw new ArgumentOutOfRangeException(nameof(MaximumSingleBlockBodyBytes));
        ArgumentNullException.ThrowIfNull(CommandCompletionEvents);
        if (CommandCompletionEvents.Any(static pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == 0))
            throw new ArgumentException("Completion-event mappings require nonempty command names and nonzero CEIDs.", nameof(CommandCompletionEvents));
    }
}
