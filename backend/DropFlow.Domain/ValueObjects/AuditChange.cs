namespace DropFlow.Domain.ValueObjects;

public class AuditChange
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}