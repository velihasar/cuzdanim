using System;
using System.Collections.Generic;
using System.Linq;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.URI;

namespace Business.Helpers
{
    public static class PaginationHelper
    {
        /// <summary>
        /// Create paginated response with int page numbers
        /// </summary>
        /// <param name="data"></param>
        /// <param name="paginationFilter"></param>
        /// <param name="totalRecords"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Paginated result with int page numbers</returns>
        public static PaginatedResult<IEnumerable<T>> CreatePaginatedResponse<T>(IEnumerable<T> data, PaginationFilter paginationFilter, int totalRecords)
        {
            data = data.Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                .Take(paginationFilter.PageSize);
            int roundedTotalPages;
            var response =
                new PaginatedResult<IEnumerable<T>>(data, paginationFilter.PageNumber, paginationFilter.PageSize);
            var totalPages = totalRecords / (double)paginationFilter.PageSize;
            if (paginationFilter.PageNumber <= 0 || paginationFilter.PageSize <= 0)
            {
                roundedTotalPages = 1;
                paginationFilter.PageNumber = 1;
                paginationFilter.PageSize = 1;
            }
            else
            {
                roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));
            }

            // NextPage hesapla
            if (paginationFilter.PageNumber >= 1 && paginationFilter.PageNumber < roundedTotalPages)
            {
                response.NextPage = paginationFilter.PageNumber + 1;
            }

            // PreviousPage hesapla
            if (paginationFilter.PageNumber - 1 >= 1 && paginationFilter.PageNumber <= roundedTotalPages)
            {
                response.PreviousPage = paginationFilter.PageNumber - 1;
            }

            // FirstPage ve LastPage hesapla
            response.FirstPage = 1;
            response.LastPage = roundedTotalPages;
            response.TotalPages = roundedTotalPages;
            response.TotalRecords = totalRecords;
            return response;
        }

        /// <summary>
        /// Create paginated response with URI service (for backward compatibility)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="paginationFilter"></param>
        /// <param name="totalRecords"></param>
        /// <param name="uriService"></param>
        /// <param name="route"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Paginated result with URI</returns>
        public static PaginatedResult<IEnumerable<T>> CreatePaginatedResponseWithUri<T>(IEnumerable<T> data, PaginationFilter paginationFilter, int totalRecords, Core.Utilities.URI.IUriService uriService, string route)
        {
            data = data.Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                .Take(paginationFilter.PageSize);
            int roundedTotalPages;
            var response =
                new PaginatedResult<IEnumerable<T>>(data, paginationFilter.PageNumber, paginationFilter.PageSize);
            var totalPages = totalRecords / (double)paginationFilter.PageSize;
            if (paginationFilter.PageNumber <= 0 || paginationFilter.PageSize <= 0)
            {
                roundedTotalPages = 1;
                paginationFilter.PageNumber = 1;
                paginationFilter.PageSize = 1;
            }
            else
            {
                roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));
            }

            // URI servisi ile NextPage ve PreviousPage hesapla
            if (paginationFilter.PageNumber >= 1 && paginationFilter.PageNumber < roundedTotalPages)
            {
                var nextPageUri = uriService.GeneratePageRequestUri(
                    new PaginationFilter(paginationFilter.PageNumber + 1, paginationFilter.PageSize), route);
                response.NextPage = int.Parse(nextPageUri.Query.Split('=')[1].Split('&')[0]);
            }

            if (paginationFilter.PageNumber - 1 >= 1 && paginationFilter.PageNumber <= roundedTotalPages)
            {
                var previousPageUri = uriService.GeneratePageRequestUri(
                    new PaginationFilter(paginationFilter.PageNumber - 1, paginationFilter.PageSize), route);
                response.PreviousPage = int.Parse(previousPageUri.Query.Split('=')[1].Split('&')[0]);
            }

            // FirstPage ve LastPage hesapla
            var firstPageUri = uriService.GeneratePageRequestUri(new PaginationFilter(1, paginationFilter.PageSize), route);
            var lastPageUri = uriService.GeneratePageRequestUri(new PaginationFilter(roundedTotalPages, paginationFilter.PageSize), route);

            response.FirstPage = 1;
            response.LastPage = roundedTotalPages;
            response.TotalPages = roundedTotalPages;
            response.TotalRecords = totalRecords;
            return response;
        }
    }
}