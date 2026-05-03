namespace DropFlow.Shared.Stores;

public class StoreDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public string ContactName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}