namespace DropFlow.Shared.Common;

public abstract class PaginatedFilter
{
    public int Page { get; set; } = 1;

    public int PageSize
    {
        get;
        set => field = Math.Clamp(value, 1, 500);
    } = 20;
}
