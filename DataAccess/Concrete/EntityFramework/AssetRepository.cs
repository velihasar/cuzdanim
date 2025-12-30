
using System;
using System.Linq;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using DataAccess.Abstract;
namespace DataAccess.Concrete.EntityFramework
{
    public class AssetRepository : EfEntityRepositoryBase<Asset, ProjectDbContext>, IAssetRepository
    {
        public AssetRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
