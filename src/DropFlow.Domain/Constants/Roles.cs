namespace DropFlow.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Livreur = "Livreur";

    public static readonly string[] All = { Admin, Manager, Livreur };
    public static bool IsValid(string role) => All.Contains(role);
}