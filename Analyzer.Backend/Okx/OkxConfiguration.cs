namespace Analyzer.Backend.Okx;

public class OkxConfiguration
{
    public string ApiKey { get; init; } = string.Empty;

    public string Passphrase { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public bool IsDemo { get; init; }
}
