
using System;
using System.Linq;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using DataAccess.Abstract;
namespace DataAccess.Concrete.EntityFramework
{
    public class TransactionRepository : EfEntityRepositoryBase<Transaction, ProjectDbContext>, ITransactionRepository
    {
        public TransactionRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
