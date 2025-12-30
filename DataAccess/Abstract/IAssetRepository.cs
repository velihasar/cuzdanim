
using System;
using Core.DataAccess;
using Core.Entities.Concrete;
namespace DataAccess.Abstract
{
    public interface IAssetRepository : IEntityRepository<Asset>
    {
    }
}