using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AjoVault.API.Config;
using Microsoft.Extensions.Options;

namespace AjoVault.API.Kredar;

public sealed class WebhookRegistrationService(
    IHttpClientFactory httpFactory,
    IOptions<KredarSettings> settings,
    ILogger<WebhookRegistrationService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the app to finish starting before hitting an external service
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        try { await EnsureRegisteredAsync(stoppingToken); }
        catch (Exception ex) { logger.LogError(ex, "Webhook auto-registration failed — deposits will not be credited until this is fixed"); }
    }

    private async Task EnsureRegisteredAsync(CancellationToken ct)
    {
        var cfg = settings.Value;
        if (string.IsNullOrWhiteSpace(cfg.ApiKey) || string.IsNullOrWhiteSpace(cfg.WebhookUrl))
        {
            logger.LogWarning("KredarSettings.ApiKey or WebhookUrl not configured — skipping webhook registration");
            return;
        }

        using var http = CreateClient(cfg);

        // Check existing endpoints
        using var listResp = await http.GetAsync("api/v1/webhook-endpoints", ct);
        var listJson = await listResp.Content.ReadAsStringAsync(ct);

        if (listResp.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<KredarEnvelope<List<WebhookEndpointItem>>>(listJson, JsonOpts);
            var already = envelope?.Data?.Any(e => e.Active &&
                string.Equals(e.Url, cfg.WebhookUrl, StringComparison.OrdinalIgnoreCase));
            if (already == true)
            {
                logger.LogInformation("Kredar webhook endpoint already registered: {Url}", cfg.WebhookUrl);
                return;
            }
        }
        else
        {
            logger.LogWarning("Could not list Kredar webhook endpoints ({Status}): {Body} — attempting registration anyway",
                (int)listResp.StatusCode, listJson);
        }

        // Register
        var body = JsonSerializer.Serialize(new { url = cfg.WebhookUrl });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var regResp = await http.PostAsync("api/v1/webhook-endpoints", content, ct);
        var regJson = await regResp.Content.ReadAsStringAsync(ct);

        if (regResp.IsSuccessStatusCode)
            logger.LogInformation("Kredar webhook endpoint registered: {Url}", cfg.WebhookUrl);
        else
            logger.LogError("Kredar webhook registration failed ({Status}): {Body}", (int)regResp.StatusCode, regJson);
    }

    private HttpClient CreateClient(KredarSettings cfg)
    {
        var http = httpFactory.CreateClient("kredar");
        http.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        return http;
    }

    private record WebhookEndpointItem(Guid Id, string Url, bool Active);
}
