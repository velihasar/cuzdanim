using Core.Entities;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.DataAccess
{
    public interface IEntityRepository<T>
      where T : class, IEntity
    {
        T Add(T entity);
        T Update(T entity);
        void Delete(T entity);
        IEnumerable<T> GetList(Expression<Func<T, bool>> expression = null);
        Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> expression = null);
        PagingResult<T> GetListForPaging(int page, string propertyName, bool asc, Expression<Func<T, bool>> expression = null, params Expression<Func<T, object>>[] includeEntities);
        Task<PagingResult<T>> GetListForTableSearch(TableGlobalFilter globalFilter);
        T Get(Expression<Func<T, bool>> expression);
        Task<T> GetAsync(Expression<Func<T, bool>> expression);
        int SaveChanges();
        Task<int> SaveChangesAsync();
        IQueryable<T> Query();
        Task<int> Execute(FormattableString interpolatedQueryString);

        TResult InTransaction<TResult>(Func<TResult> action, Action successAction = null, Action<Exception> exceptionAction = null);

        Task<int> GetCountAsync(Expression<Func<T, bool>> expression = null);
        int GetCount(Expression<Func<T, bool>> expression = null);
        T GetWithInclude(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);
        Task<T> GetWithIncludeAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);
        PagingResult<T> GetListForPagingWithInclude(int page, string propertyName, bool asc, Expression<Func<T, bool>> expression = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);
        ICollection<T> FindAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null, bool NoTracking = true, bool hideDeleted = true, int take = 0, int page = 0, string orderBy = "");
        Task<ICollection<T>> FindAllAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null, bool NoTracking = true, bool hideDeleted = true, int take = 0, int page = 0, string orderBy = "");

        /// <summary>
        /// PaginatedResult döndüren, include desteği olan, performanslı sayfalama metodu
        /// </summary>
        /// <param name="page">Sayfa numarası (1'den başlar)</param>
        /// <param name="take">Sayfa başına kayıt sayısı</param>
        /// <param name="orderBy">Sıralama alanı</param>
        /// <param name="isAscending">Artış sırası (true: ASC, false: DESC)</param>
        /// <param name="filter">Filtreleme koşulu</param>
        /// <param name="include">Include fonksiyonu (ThenInclude desteği ile)</param>
        /// <param name="noTracking">AsNoTracking kullanımı (performans için)</param>
        /// <param name="hideDeleted">Silinmiş kayıtları gizle</param>
        /// <returns>PaginatedResult<T> - Sayfalama bilgileri ile birlikte veri</returns>
        Task<PaginatedResult<IEnumerable<T>>> GetPaginatedListAsync(
            int page = 1,
            int take = 10,
            string orderBy = "Id",
            bool isAscending = true,
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
            bool noTracking = true,
            bool hideDeleted = true);

        /// <summary>
        /// PostgreSQL için navigation property sıralaması yapabilen özel metod
        /// </summary>
        /// <param name="page">Sayfa numarası</param>
        /// <param name="take">Sayfa başına kayıt sayısı</param>
        /// <param name="orderBySelector">Sıralama seçici (navigation property için)</param>
        /// <param name="isAscending">Artış sırası</param>
        /// <param name="filter">Filtreleme koşulu</param>
        /// <param name="include">Include fonksiyonu</param>
        /// <param name="noTracking">AsNoTracking kullanımı</param>
        /// <param name="hideDeleted">Silinmiş kayıtları gizle</param>
        /// <returns>PaginatedResult<T> - Sayfalama bilgileri ile birlikte veri</returns>
        Task<PaginatedResult<IEnumerable<T>>> GetPaginatedListWithNavigationOrderAsync<TKey>(
            int page = 1,
            int take = 10,
            Expression<Func<T, TKey>> orderBySelector = null,
            bool isAscending = true,
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
            bool noTracking = true,
            bool hideDeleted = true);

    }
}