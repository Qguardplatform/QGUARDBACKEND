
namespace qguardbackend.Shared.Pagination
{
    public class PagedList<T>
    {
        public int PageNumber { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPageCount { get; private set; }
        public List<T> Items { get; set; }

        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageNumber < TotalPageCount);
            }
        }

        public PagedList(List<T> items, int pageIndex, int pageSize, int totalCount)
        {
            if (items == null)
            {
                throw new ArgumentNullException("source");
            }

            Items = items;
            PageNumber = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

        }
    }
}
