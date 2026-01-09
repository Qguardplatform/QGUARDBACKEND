namespace qguardbackend.Data.DTOs.Results;

public class PaginatedResult<T>
{
    public long PageSize { get; }
    public long PageNumber { get; }
    public long TotalSize { get; }
    public T[] Items { get; }

    public PaginatedResult(T[] items, long totalSize, long pageNumber, long pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        TotalSize = totalSize;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Items = items;
    }
}