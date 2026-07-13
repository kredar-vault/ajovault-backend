namespace AjoVault.API.Config;

public class KredarSettings
{
    public string BaseUrl { get; set; } = "https://api.kredar.xyz";
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = "https://api.vault.kredar.xyz/webhooks/kredar";
}
