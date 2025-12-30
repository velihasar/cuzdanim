
using System;
using System.Linq;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using DataAccess.Abstract;
namespace DataAccess.Concrete.EntityFramework
{
    public class IncomeCategoryRepository : EfEntityRepositoryBase<IncomeCategory, ProjectDbContext>, IIncomeCategoryRepository
    {
        public IncomeCategoryRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
