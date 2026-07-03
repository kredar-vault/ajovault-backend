using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Contributions.Dto;

public class RecordContributionRequest
{
    [Range(1, int.MaxValue)]
    public int CycleNumber { get; set; }
}
