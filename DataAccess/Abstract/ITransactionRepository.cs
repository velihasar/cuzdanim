
using System;
using Core.DataAccess;
using Core.Entities.Concrete;
namespace DataAccess.Abstract
{
    public interface ITransactionRepository : IEntityRepository<Transaction>
    {
    }
}