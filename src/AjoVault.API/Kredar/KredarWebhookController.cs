using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AjoVault.API.Common;
using AjoVault.API.Config;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AjoVault.API.Kredar;

[ApiController]
public class KredarWebhookController(
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    IOptions<KredarSettings> settings,
    ILogger<KredarWebhookController> logger) : ControllerBase
{
    [HttpPost("webhooks/kredar")]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var rawBody = ms.ToArray();

        if (!VerifySignature(rawBody, Request.Headers["x-kredar-signature"].ToString()))
        {
            logger.LogWarning("Kredar webhook signature mismatch");
            return Unauthorized(ApiResponse<object>.Fail("Invalid signature."));
        }

        var eventType = Request.Headers["x-kredar-event"].ToString();
        if (eventType != "deposit.reconciled")
            return Ok(ApiResponse<object>.Success(new { }, "Event ignored."));

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data))
            return BadRequest(ApiResponse<object>.Fail("Missing data field."));

        var accountNumber = GetStr(data, "accountNumber");
        var amountNaira = GetDecimal(data, "amountNaira") ?? GetDecimal(data, "amountPaid") ?? 0;
        var txRef = GetStr(data, "kredarReference") ?? GetStr(data, "transactionReference") ?? Guid.NewGuid().ToString();
        var senderName = GetStr(data, "transferName") ?? "Unknown";

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            logger.LogWarning("Kredar webhook missing accountNumber");
            return BadRequest(ApiResponse<object>.Fail("Missing accountNumber."));
        }

        var group = await groupRepo.FindByDvaAccountNumberAsync(accountNumber);
        if (group == null)
        {
            logger.LogWarning("No AjoVault group found for DVA {AccountNumber}", accountNumber);
            return Ok(ApiResponse<object>.Success(new { }, "No matching group."));
        }

        if (group.Status != Groups.GroupStatus.Active)
        {
            logger.LogWarning("Group {GroupId} received deposit but is not Active", group.Id);
            return Ok(ApiResponse<object>.Success(new { }, "Group not active."));
        }

        // Determine current cycle
        var cycleNumber = GetCurrentCycleNumber(group);
        if (cycleNumber <= 0)
            return Ok(ApiResponse<object>.Success(new { }, "No active cycle."));

        // Find first member in this cycle who hasn't contributed yet (ordered by payout position)
        var members = await groupRepo.GetMembersAsync(group.Id);
        var existingContributions = await contributionRepo.GetByGroupAsync(group.Id);
        var paidThisCycle = existingContributions
            .Where(c => c.CycleNumber == cycleNumber)
            .Select(c => c.UserId)
            .ToHashSet();

        var nextMember = members
            .OrderBy(m => m.PayoutPosition)
            .FirstOrDefault(m => !paidThisCycle.Contains(m.UserId));

        if (nextMember == null)
        {
            logger.LogInformation("All members have contributed for cycle {Cycle} in group {GroupId}", cycleNumber, group.Id);
            return Ok(ApiResponse<object>.Success(new { }, "All contributions received for this cycle."));
        }

        // Idempotency: check by txRef
        var existing = existingContributions.FirstOrDefault(c => c.Reference == txRef);
        if (existing != null)
            return Ok(ApiResponse<object>.Success(new { }, "Already recorded."));

        var contribution = new Contribution
        {
            GroupId = group.Id,
            UserId = nextMember.UserId,
            CycleNumber = cycleNumber,
            Amount = amountNaira,
            Status = ContributionStatus.Received,
            Reference = txRef.Length > 20 ? txRef[..20] : txRef,
        };

        await contributionRepo.AddAsync(contribution);

        logger.LogInformation(
            "Kredar deposit ₦{Amount} from '{Sender}' → group {GroupId} cycle {Cycle} member {UserId}",
            amountNaira, senderName, group.Id, cycleNumber, nextMember.UserId);

        return Ok(ApiResponse<object>.Success(new
        {
            groupId = group.Id,
            userId = nextMember.UserId,
            cycleNumber,
            amountNaira
        }, "Contribution recorded."));
    }

    private bool VerifySignature(byte[] body, string header)
    {
        var secret = settings.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(header))
            return false;

        var computed = Convert.ToHexString(
            new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(body)).ToLowerInvariant();

        var a = Encoding.UTF8.GetBytes(computed);
        var b = Encoding.UTF8.GetBytes(header.Trim());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static int GetCurrentCycleNumber(Groups.SavingsGroup group)
    {
        if (!group.StartDate.HasValue) return 0;
        var daysSinceStart = (DateTime.UtcNow - group.StartDate.Value).Days;
        var cycleDays = group.Frequency switch
        {
            Groups.ContributionFrequency.Weekly => 7,
            Groups.ContributionFrequency.BiWeekly => 14,
            _ => 30
        };
        return Math.Max(1, (daysSinceStart / cycleDays) + 1);
    }

    private static string? GetStr(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }
}
