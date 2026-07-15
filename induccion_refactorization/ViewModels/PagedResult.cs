using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;

namespace induccion_refactorization.ViewModels
{
    public class PagerViewModel
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public RouteValueDictionary RouteValues { get; set; } = new RouteValueDictionary();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResult<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            var totalCount = source.Count();

            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
