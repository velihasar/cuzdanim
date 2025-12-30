using Core.Entities;
using System.Collections.Generic;

namespace Entities.Dtos.Asset
{
    public class AssetDistributionResultDto : IDto
    {
        public List<AssetDistributionDto> Distributions { get; set; }
        public decimal TotalValueInTl { get; set; }
    }
}

