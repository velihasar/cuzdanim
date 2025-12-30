using Core.Aspects.Autofac.Performance;
using Core.Entities.Concrete;
using Core.Enums;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.Asset;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Assets.Queries
{
    public class GetAssetDistributionQuery
        : IRequest<IDataResult<AssetDistributionResultDto>>
    {
        public class GetAssetDistributionQueryHandler
            : IRequestHandler<GetAssetDistributionQuery, IDataResult<AssetDistributionResultDto>>
        {
            private readonly IAssetRepository _assetRepository;

            public GetAssetDistributionQueryHandler(
                IAssetRepository assetRepository)
            {
                _assetRepository = assetRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<AssetDistributionResultDto>> Handle(
                GetAssetDistributionQuery request,
                CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();

                // Kullanıcının tüm varlıklarını getir (borç olmayanlar)
                var assets = await _assetRepository.FindAllAsync(
                    filter: x => x.UserId == userId && !x.IsDebt,
                    include: i => i.Include(x => x.AssetType)
                );

                if (assets == null || !assets.Any())
                {
                    return new SuccessDataResult<AssetDistributionResultDto>(new AssetDistributionResultDto
                    {
                        Distributions = new List<AssetDistributionDto>(),
                        TotalValueInTl = 0
                    });
                }

                // Her asset için TL değerini hesapla
                var assetValues = new List<(string TypeName, decimal ValueInTl)>();

                foreach (var asset in assets)
                {
                    if (asset.AssetType == null)
                        continue;

                    // AssetType'daki TlValue kullan (job ile sürekli güncelleniyor)
                    decimal valueInTl = asset.Piece * asset.AssetType.TlValue;

                    // AssetConvertType'a göre kategori adı belirle
                    string categoryName = GetCategoryName(asset.AssetType.ConvertedAmountType, asset.AssetType.Name);

                    assetValues.Add((categoryName, valueInTl));
                }

                // Kategorilere göre grupla ve toplam değerleri hesapla
                var grouped = assetValues
                    .GroupBy(x => x.TypeName)
                    .Select(g => new
                    {
                        TypeName = g.Key,
                        TotalValue = g.Sum(x => x.ValueInTl)
                    })
                    .ToList();

                // Toplam TL değerini hesapla
                var totalValueInTl = grouped.Sum(g => g.TotalValue);

                // Yüzde hesapla ve DTO listesi oluştur
                var distributions = grouped
                    .Select(g => new AssetDistributionDto
                    {
                        AssetTypeName = g.TypeName,
                        TotalValueInTl = g.TotalValue,
                        Percentage = totalValueInTl > 0 
                            ? Math.Round((g.TotalValue / totalValueInTl) * 100, 2) 
                            : 0
                    })
                    .OrderByDescending(x => x.TotalValueInTl)
                    .ToList();

                var result = new AssetDistributionResultDto
                {
                    Distributions = distributions,
                    TotalValueInTl = totalValueInTl
                };

                return new SuccessDataResult<AssetDistributionResultDto>(result);
            }

            private string GetCategoryName(AssetConvertType convertType, string assetTypeName)
            {
                return convertType switch
                {
                    AssetConvertType.Tl => "TL",
                    AssetConvertType.GrAltin => "Altın",
                    AssetConvertType.CeyrekAltin => "Altın",
                    AssetConvertType.YarimAltin => "Altın",
                    AssetConvertType.TamAltin => "Altın",
                    AssetConvertType.Dolar => "Dolar",
                    AssetConvertType.Avro => "Euro",
                    AssetConvertType.JaponYeni => "Japon Yeni",
                    AssetConvertType.IngilizSterlini => "İngiliz Sterlini",
                    AssetConvertType.Diger => assetTypeName ?? "Diğer",
                    _ => assetTypeName ?? "Diğer"
                };
            }
        }
    }

}

