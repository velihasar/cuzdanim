using Core.Entities;

namespace Entities.Dtos.Asset
{
    public class AssetDistributionDto : IDto
    {
        public string AssetTypeName { get; set; } // "TL", "Altın", "Dolar", "Euro", "Borsa"
        public decimal TotalValueInTl { get; set; }
        public decimal Percentage { get; set; }
    }
}

