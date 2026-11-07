namespace PHEOC.OMS.Application.Security;

// Suggested starting selections. The creator's final checkbox selection is authoritative.
public static class RolePermissionDefaults
{
    public const string ExplicitSelectionClaim="permission-selection-mode";
    public const string ExplicitSelectionValue="explicit";
    public static IReadOnlyList<string> For(string role)=>role switch
    {
        "System Administrator"=>Permissions.All.ToArray(),
        "PHEOC Director"=>[Permissions.QiraReview,Permissions.QiraReassess,Permissions.EventsView,Permissions.EventsUpdate,Permissions.EventsActivate,Permissions.EventsClose,Permissions.CloseExceptional,Permissions.RiskApprove,Permissions.TeamsView,Permissions.TeamsDeploy,Permissions.InventoryView,Permissions.ReportsView,Permissions.ReportsApprove,Permissions.AuditView],
        "Duty Officer"=>[Permissions.QiraReassess,Permissions.EventsView,Permissions.EventsCreate,Permissions.EventsUpdate,Permissions.RiskCreate,Permissions.TeamsView,Permissions.ReportsView,Permissions.ReportsCreate],
        "Incident Commander"=>[Permissions.QiraReview,Permissions.QiraReassess,Permissions.EventsView,Permissions.EventsUpdate,Permissions.EventsActivate,Permissions.EventsClose,Permissions.RiskCreate,Permissions.RiskApprove,Permissions.TeamsView,Permissions.TeamsDeploy,Permissions.InventoryView,Permissions.FieldManage,Permissions.ReportsView,Permissions.ReportsCreate,Permissions.ReportsApprove],
        "Risk Assessment Officer"=>[Permissions.QiraReassess,Permissions.EventsView,Permissions.RiskCreate,Permissions.ReportsView],
        "Operations Chief"=>[Permissions.EventsView,Permissions.EventsUpdate,Permissions.TeamsView,Permissions.TeamsManage,Permissions.TeamsDeploy,Permissions.FieldManage,Permissions.InventoryView,Permissions.ReportsView,Permissions.ReportsCreate],
        "Planning Chief"=>[Permissions.EventsView,Permissions.TeamsView,Permissions.InventoryView,Permissions.ReportsView,Permissions.ReportsCreate],
        "Logistics Chief"=>[Permissions.EventsView,Permissions.TeamsView,Permissions.InventoryView,Permissions.InventoryManage,Permissions.ReportsView],
        "Finance and Administration Officer"=>[Permissions.EventsView,Permissions.InventoryView,Permissions.ReportsView],
        "RRT Team Leader"=>[Permissions.EventsView,Permissions.TeamsView,Permissions.FieldManage,Permissions.ReportsView,Permissions.ReportsCreate],
        "RRT Member"=>[Permissions.EventsView,Permissions.TeamsView,Permissions.FieldManage],
        "Inventory Officer"=>[Permissions.EventsView,Permissions.InventoryView,Permissions.InventoryManage],
        "Laboratory Officer"=>[Permissions.EventsView,Permissions.SensitiveView,Permissions.HealthManage],
        "Surveillance Officer"=>[Permissions.EventsView,Permissions.EventsCreate,Permissions.EventsUpdate,Permissions.SensitiveView,Permissions.HealthManage,Permissions.ReportsView],
        "Reporting Officer"=>[Permissions.EventsView,Permissions.ReportsView,Permissions.ReportsCreate],
        "Executive Viewer"=>[Permissions.EventsView,Permissions.ReportsView],
        "External Agency User"=>[Permissions.EventsView],
        _=>[]
    };
    public static bool RequiresCentralScope(string permission)=>permission.StartsWith("Admin.",StringComparison.Ordinal)||permission==Permissions.AuditView;
}
