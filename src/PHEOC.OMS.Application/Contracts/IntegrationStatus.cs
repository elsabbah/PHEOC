namespace PHEOC.OMS.Application.Contracts;
public record IntegrationEventStatus(Guid PublicId,string Source,string ExternalEventId,string Status,DateTime ReceivedAt);
public interface IIntegrationStatus { Task<IntegrationEventStatus> Status(Guid id); Task QueueStatus(Guid id); }
