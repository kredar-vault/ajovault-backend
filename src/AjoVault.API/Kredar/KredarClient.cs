using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AjoVault.API.Config;
using Microsoft.Extensions.Options;

namespace AjoVault.API.Kredar;

public class KredarClient(IHttpClientFactory httpFactory, IOptions<KredarSettings> settings, ILogger<KredarClient> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<KredarCustomerResult?> CreateCustomerAsync(
        string firstName, string lastName, string email, string? phoneNumber = null, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new { firstName, lastName, email, phoneNumber });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/customers", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Kredar create-customer failed {Status}: {Body}", (int)response.StatusCode, json);
            return null;
        }

        var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarCustomerResult>>(json, JsonOpts);
        return envelope?.Data;
    }

    public async Task<KredarDvaResult?> CreateDvaAsync(
        Guid customerId, decimal? expectedAmountNaira, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new
        {
            customerId,
            expectedAmount = expectedAmountNaira
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/dedicated-accounts", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Kredar create-dva failed {Status}: {Body}", (int)response.StatusCode, json);
            return null;
        }

        var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarDvaResult>>(json, JsonOpts);
        return envelope?.Data;
    }

    private HttpClient CreateClient()
    {
        var cfg = settings.Value;
        var http = httpFactory.CreateClient("kredar");
        http.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        return http;
    }
}

public record KredarEnvelope<T>(bool IsSuccess, string? Message, T? Data);
public record KredarCustomerResult(Guid Id, string FirstName, string LastName, string Email);
public record KredarDvaResult(Guid Id, Guid CustomerId, string AccountNumber, string BankName, string AccountName);
