using Dreamine.Secs.Abstractions.Enums;

namespace Dreamine.Gem.QuickStart;

internal sealed record DemoSampleOptions(
    SecsRole Role,
    string Host,
    int Port,
    ushort SessionId,
    int TimeoutSeconds,
    string? EvidencePath,
    bool ShowHelp)
{
    public static DemoSampleOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        SecsRole? role = null;
        var host = "127.0.0.1";
        var port = 5000;
        ushort sessionId = 37;
        var timeoutSeconds = 45;
        string? evidencePath = null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < arguments.Count; index++)
        {
            var option = arguments[index];
            if (option is "--help" or "-h")
                return new DemoSampleOptions(SecsRole.Host, host, port, sessionId, timeoutSeconds, null, true);
            if (!seen.Add(option)) throw new ArgumentException($"Option '{option}' was specified more than once.");
            if (++index >= arguments.Count) throw new ArgumentException($"Option '{option}' requires a value.");
            var value = arguments[index];
            switch (option.ToLowerInvariant())
            {
                case "--role":
                    role = value.ToLowerInvariant() switch
                    {
                        "host" => SecsRole.Host,
                        "equipment" => SecsRole.Equipment,
                        _ => throw new ArgumentException("--role must be host or equipment.")
                    };
                    break;
                case "--host":
                    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("--host cannot be empty.");
                    host = value;
                    break;
                case "--port":
                    if (!int.TryParse(value, out port) || port is <= 0 or > 65535)
                        throw new ArgumentOutOfRangeException(nameof(arguments), "--port must be between 1 and 65535.");
                    break;
                case "--session-id":
                    if (!ushort.TryParse(value, out sessionId) || sessionId == 0 || sessionId == ushort.MaxValue)
                        throw new ArgumentOutOfRangeException(nameof(arguments), "--session-id must be between 1 and 65534.");
                    break;
                case "--timeout-seconds":
                    if (!int.TryParse(value, out timeoutSeconds) || timeoutSeconds is < 10 or > 120)
                        throw new ArgumentOutOfRangeException(nameof(arguments), "--timeout-seconds must be between 10 and 120.");
                    break;
                case "--evidence":
                    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("--evidence cannot be empty.");
                    evidencePath = Path.GetFullPath(value);
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{option}'.");
            }
        }

        return new DemoSampleOptions(
            role ?? throw new ArgumentException("--role host|equipment is required."),
            host,
            port,
            sessionId,
            timeoutSeconds,
            evidencePath,
            false);
    }

    public static void PrintHelp()
    {
        Console.WriteLine("Dreamine.Gem.QuickStart --role host|equipment [--host 127.0.0.1] [--port 5000] [--session-id 37] [--timeout-seconds 45] [--evidence result.json]");
        Console.WriteLine("EN: Start Equipment first, then Host in a separate process. The Demo validates the frozen E30-derived subset; it is not a conformance claim.");
        Console.WriteLine("KO: Equipment를 먼저, Host를 별도 프로세스로 실행합니다. 동결 E30 파생 부분집합 Demo이며 적합성 주장이 아닙니다.");
    }
}
