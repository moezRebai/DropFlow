namespace DropFlow.Shared.Tenants;

/// <summary>
/// Énumération des formes juridiques françaises
/// </summary>
public static class LegalForms
{
    public const string SARL = "SARL";
    public const string SAS = "SAS";
    public const string SASU = "SASU";
    public const string EURL = "EURL";
    public const string SA = "SA";
    public const string SNC = "SNC";
    public const string EI = "EI";
    public const string AutoEntrepreneur = "Auto-entrepreneur";
    
    public static readonly string[] All = 
    {
        SARL, SAS, SASU, EURL, SA, SNC, EI, AutoEntrepreneur
    };
}