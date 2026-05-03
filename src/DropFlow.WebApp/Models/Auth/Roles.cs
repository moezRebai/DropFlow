namespace DropFlow.WebApp.Models.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Livreur = "Livreur";
    
    // Combinaisons
    public const string ManagerOrAdmin = "Manager,Admin";
    public const string All = "Admin,Manager,Livreur";
}