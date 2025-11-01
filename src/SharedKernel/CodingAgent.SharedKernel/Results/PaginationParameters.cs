namespace CodingAgent.SharedKernel.Results;

/// <summary>
/// Represents pagination parameters for queries.
/// </summary>
public class PaginationParameters
{
    public int PageNumber { get; }
    public int PageSize { get; }
    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;

    public PaginationParameters(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        }
        
        if (pageSize < 1)
        {
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
        }
        
        if (pageSize > 100)
        {
            throw new ArgumentException("Page size cannot exceed 100", nameof(pageSize));
        }

        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

