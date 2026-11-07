using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;

public class RiskPolicy : Record
{
    [MaxLength(120)] public string Name { get; set; } = "";
    public int Version { get; set; }
    public int ModerateFrom { get; set; }
    public int HighFrom { get; set; }
    public int CriticalFrom { get; set; }
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
}
public class RiskAssessment : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public long PolicyId { get; set; }
    public RiskPolicy Policy { get; set; } = null!;
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int Score { get; set; }
    [MaxLength(40)] public string Level { get; set; } = "";
    [MaxLength(2000)] public string Rationale { get; set; } = "";
    [MaxLength(450)] public string AssessedBy { get; set; } = "";
}
public class ActivationDecision : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public int Level { get; set; }
    [MaxLength(2000)] public string Rationale { get; set; } = "";
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
}
public class IncidentAssignment : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(120)] public string Position { get; set; } = "";
    [MaxLength(160)] public string PersonName { get; set; } = "";
    [MaxLength(160)] public string Organization { get; set; } = "";
    public DateTime? EndedAt { get; set; }
}
public class OperationalTask : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(450)] public string AssignedTo { get; set; } = "";
    public DateTime DueAt { get; set; }
    public TaskState State { get; set; }
    public int Progress { get; set; }
}
public class IncidentDecision : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(4000)] public string Text { get; set; } = "";
    [MaxLength(450)] public string DecidedBy { get; set; } = "";
    public long? CorrectsDecisionId { get; set; }
    public IncidentDecision? CorrectsDecision { get; set; }
}
public class Person : Record
{
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(80)] public string EmployeeNumber { get; set; } = "";
    [MaxLength(160)] public string Organization { get; set; } = "";
    [MaxLength(120)] public string Specialty { get; set; } = "";
    [MaxLength(120)] public string Skills { get; set; } = "";
    [MaxLength(80)] public string Phone { get; set; } = "";
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
    public bool IsAvailable { get; set; } = true;
}
public class ResponseTeam : Record
{
    [MaxLength(160)] public string Name { get; set; } = "";
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
    public TeamState State { get; set; } = TeamState.Ready;
    public List<TeamMember> Members { get; set; } = [];
}
public class TeamMember
{
    public long TeamId { get; set; }
    public ResponseTeam Team { get; set; } = null!;
    public long PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public bool IsLeader { get; set; }
}
public class Deployment : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public long TeamId { get; set; }
    public ResponseTeam Team { get; set; } = null!;
    [MaxLength(2000)] public string Mission { get; set; } = "";
    public string MembersSnapshot { get; set; } = "";
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
    public DateTime? ArrivedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
