
using System;
using System.Linq;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using DataAccess.Abstract;
namespace DataAccess.Concrete.EntityFramework
{
    public class AssetTypeRepository : EfEntityRepositoryBase<AssetType, ProjectDbContext>, IAssetTypeRepository
    {
        public AssetTypeRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
