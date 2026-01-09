
namespace examportal.Shared.Pagination
{
    public static class QueryableExtensions
    {
        public static PagedList<T> ToPaginatedList<T>(this IQueryable<T> query, int pageIndex, int pageSize, int total)
        {
            var list = query.Paginate(pageIndex, pageSize).ToList();
            return new PagedList<T>(list, pageIndex, pageSize, total);
        }

        public static PagedList<T> ToPaginatedList<T>(this List<T> list, int pageIndex, int pageSize, int total)
        {
            return new PagedList<T>(list, pageIndex, pageSize, total);
        }

        public static PagedList<T> ToPaginatedList<T>(this IQueryable<T> query, int pageIndex, int pageSize)
        {
            int total = query.Count();
            var list = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(list, pageIndex, pageSize, total);
        }

        public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int pageIndex, int pageSize)
        {
            return query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
        }
    }
}
