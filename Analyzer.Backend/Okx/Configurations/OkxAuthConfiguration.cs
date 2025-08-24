namespace Analyzer.Backend.Okx.Configurations;

public class OkxAuthConfiguration
{
    public string ApiKey { get; init; } = string.Empty;

    public string Passphrase { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public bool IsDemo { get; init; }
}
