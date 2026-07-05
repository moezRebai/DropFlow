namespace DropFlow.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Livreur = "Livreur";
    public const string Accountant = "Accountant";
    public const string ReadOnly = "ReadOnly";

    public static readonly string[] All = { Admin, Manager, Livreur, Accountant, ReadOnly };
    public static bool IsValid(string role) => All.Contains(role);
}