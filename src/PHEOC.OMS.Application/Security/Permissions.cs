namespace PHEOC.OMS.Application.Security;
public static class Permissions
{
    public const string QiraReview="QIRA.Review",QiraReassess="QIRA.Reassess";
    public const string CloseExceptional="Events.CloseExceptional";
    public const string EventsView = "Events.View", EventsCreate = "Events.Create", EventsUpdate = "Events.Update", EventsActivate = "Events.Activate", EventsClose = "Events.Close";
    public const string RiskCreate = "RiskAssessment.Create", RiskApprove = "RiskAssessment.Approve", TeamsView = "RRT.View", TeamsManage = "RRT.Manage", TeamsDeploy = "RRT.Deploy";
    public const string InventoryView = "Inventory.View", InventoryManage = "Inventory.Manage", FieldManage = "Field.Manage", SensitiveView = "Health.View", HealthManage = "Health.Manage";
    public const string ReportsView = "Reports.View", ReportsCreate = "Reports.Create", ReportsApprove = "Reports.Approve", AdminUsers = "Admin.ManageUsers", AdminSettings = "Admin.ManageSettings", AuditView = "Audit.View";
    public static readonly string[] All = [QiraReview,QiraReassess,CloseExceptional,EventsView, EventsCreate, EventsUpdate, EventsActivate, EventsClose, RiskCreate, RiskApprove, TeamsView, TeamsManage, TeamsDeploy, InventoryView, InventoryManage, FieldManage, SensitiveView, HealthManage, ReportsView, ReportsCreate, ReportsApprove, AdminUsers, AdminSettings, AuditView];
    public static readonly string[] Roles = ["System Administrator", "PHEOC Director", "Duty Officer", "Incident Commander", "Risk Assessment Officer", "Operations Chief", "Planning Chief", "Logistics Chief", "Finance and Administration Officer", "RRT Team Leader", "RRT Member", "Inventory Officer", "Laboratory Officer", "Surveillance Officer", "Reporting Officer", "Executive Viewer", "External Agency User"];
}
public interface ICurrentUser
{
    string Id { get; }
    int? GovernorateId { get; }
    bool Can(string permission);
}
public sealed class AccessDeniedException : Exception { }
