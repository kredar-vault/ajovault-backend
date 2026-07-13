using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AjoVault.API.Config;
using Microsoft.Extensions.Options;

namespace AjoVault.API.Kredar;

public class KredarClient(IHttpClientFactory httpFactory, IOptions<KredarSettings> settings, ILogger<KredarClient> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<KredarCustomerResult?> CreateOrGetCustomerAsync(
        string firstName, string lastName, string email, string? phoneNumber = null, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new { firstName, lastName, email, phoneNumber });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/customers", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarCustomerResult>>(json, JsonOpts);
            return envelope?.Data;
        }

        // Customer likely already exists — fall back to lookup by email
        logger.LogWarning("Kredar create-customer failed {Status}: {Body} — trying lookup by email for {Email}", (int)response.StatusCode, json, email);
        var found = await FindCustomerByEmailAsync(email, ct);
        if (found == null) logger.LogError("Kredar customer lookup by email also failed for {Email} — customer not found in tenant", email);
        return found;
    }

    private async Task<KredarCustomerResult?> FindCustomerByEmailAsync(string email, CancellationToken ct)
    {
        using var http = CreateClient();
        using var response = await http.GetAsync("api/v1/customers", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode) return null;

        var envelope = JsonSerializer.Deserialize<KredarEnvelope<List<KredarCustomerResult>>>(json, JsonOpts);
        return envelope?.Data?.FirstOrDefault(c => string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<KredarDvaResult?> CreateOrGetDvaAsync(
        Guid customerId, decimal? expectedAmountNaira, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new { customerId, expectedAmount = expectedAmountNaira });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/dedicated-accounts", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarDvaResult>>(json, JsonOpts);
            return envelope?.Data;
        }

        // DVA may already exist for this customer — look it up
        logger.LogWarning("Kredar create-dva failed {Status}: {Body} — trying lookup by customerId {CustomerId}", (int)response.StatusCode, json, customerId);
        var found = await FindDvaByCustomerAsync(customerId, ct);
        if (found == null) logger.LogError("Kredar DVA lookup also failed for customerId {CustomerId} — no DVA found in tenant", customerId);
        return found;
    }

    private async Task<KredarDvaResult?> FindDvaByCustomerAsync(Guid customerId, CancellationToken ct)
    {
        using var http = CreateClient();
        using var response = await http.GetAsync("api/v1/dedicated-accounts", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode) return null;

        var envelope = JsonSerializer.Deserialize<KredarEnvelope<List<KredarDvaResult>>>(json, JsonOpts);
        return envelope?.Data?.FirstOrDefault(d => d.CustomerId == customerId);
    }

    public async Task<KredarTransferResult> InitiateTransferAsync(
        string merchantTxRef, decimal amount, string accountNumber, string bankCode,
        string? narration = null, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new { merchantTxRef, amount, accountNumber, bankCode, narration });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/transfers/bank", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        logger.LogInformation("Kredar transfer {Status}: {Body}", (int)response.StatusCode, json);

        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarTransferResult>>(json, JsonOpts);
            return envelope?.Data ?? new KredarTransferResult(merchantTxRef, "Failed", "Empty response", null);
        }

        logger.LogWarning("Kredar transfer failed {Status}: {Body}", (int)response.StatusCode, json);
        return new KredarTransferResult(merchantTxRef, "Failed", json, null);
    }

    public async Task<KredarBankLookupResult?> LookupBankAccountAsync(
        string accountNumber, string bankCode, CancellationToken ct = default)
    {
        using var http = CreateClient();
        var body = JsonSerializer.Serialize(new { accountNumber, bankCode });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.PostAsync("api/v1/transfers/bank/lookup", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode) return null;
        var envelope = JsonSerializer.Deserialize<KredarEnvelope<KredarBankLookupResult>>(json, JsonOpts);
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
public record KredarTransferResult(string MerchantTxRef, string Status, string? FailureReason, string? ProviderReference);
public record KredarBankLookupResult(string AccountName, string AccountNumber, string BankCode);
