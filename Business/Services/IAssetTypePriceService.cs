using Core.Entities.Concrete;
using Core.Enums;
using System.Threading.Tasks;

namespace Business.Services
{
    public interface IAssetTypePriceService
    {
        Task<decimal?> GetPriceForAssetConvertTypeAsync(AssetConvertType assetConvertType);
        Task<decimal?> GetPriceForAssetTypeAsync(AssetType assetType);
    }
}

