using DropFlow.Shared.Common;

namespace DropFlow.Shared.Clients;

public class ClientFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
}
