namespace DropFlow.Domain.Constants;

public static class TenantIds
{
    public const int DropFlowAdmin = 0;  // ← Super Admin (équipe DropFlow)

    // ← Sentinel for "no tenant claim could be resolved". Never matches
    // DropFlowAdmin (0) or a real tenant id (>= 1), so query filters deny
    // access by default instead of silently falling back to Super Admin.
    public const int Unresolved = -1;

    // Les vrais tenants commencent à 1
    // 1, 2, 3, ... = Entreprises clientes
}