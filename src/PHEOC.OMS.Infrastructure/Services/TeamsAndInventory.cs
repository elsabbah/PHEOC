using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Rules;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;

public partial class OmsService
{
    private async Task<List<DeploymentRow>> DeploymentRows(IQueryable<Deployment> query)
    {
        var rows=await query.Include(x=>x.Event).Include(x=>x.Team).OrderByDescending(x=>x.Id).Take(100).ToListAsync();
        return rows.Select(x=>new DeploymentRow(x.Id,x.EventId,x.Event.Title,x.Team.Name,x.Mission,x.CreatedAt,x.ArrivedAt,x.ReturnedAt,Version(x))).ToList();
    }
    public async Task<TeamBoard> Teams()
    {
        Demand(Permissions.TeamsView);
        var teams=await db.Teams.Include(x=>x.Governorate).Include(x=>x.Members).ThenInclude(x=>x.Person).Where(x=>user.GovernorateId==null||x.GovernorateId==user.GovernorateId).OrderBy(x=>x.Name).Take(200).ToListAsync();
        var people=await db.Personnel.Where(x=>user.GovernorateId==null||x.GovernorateId==user.GovernorateId).OrderBy(x=>x.Name).Take(500).Select(x=>new PersonRow(x.Id,x.Name,x.Specialty,x.Organization,x.EmployeeNumber,x.IsAvailable)).ToListAsync();
        return new(teams.Select(x=>new TeamRow(x.Id,x.Name,x.Governorate.Name,x.State,string.Join("، ",x.Members.Select(m=>m.Person.Name+(m.IsLeader?" (قائد)":""))),Version(x))).ToList(),people,await DeploymentRows(db.Deployments.Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId)),await Governorates());
    }
    public async Task AddPerson(PersonInput input)
    {
        Demand(Permissions.TeamsManage); Validate(input); Scope(input.GovernorateId);
        Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"محافظة غير صالحة.");
        db.Personnel.Add(new(){Name=input.Name,EmployeeNumber=input.EmployeeNumber,Organization=input.Organization,Specialty=input.Specialty,Skills=input.Skills,Phone=input.Phone,GovernorateId=input.GovernorateId}); await db.SaveChangesAsync();
    }
    public async Task AddTeam(TeamInput input)
    {
        Demand(Permissions.TeamsManage); Validate(input); Scope(input.GovernorateId);
        Require(input.PersonIds.Contains(input.LeaderId),"قائد الفريق يجب أن يكون ضمن الأعضاء.");
        var ids=input.PersonIds.Distinct().ToArray(); var people=await db.Personnel.Where(x=>ids.Contains(x.Id)).ToListAsync();
        Require(people.Count==ids.Length&&people.All(x=>x.IsAvailable),"أعضاء غير موجودين أو غير متاحين.");
        foreach(var person in people) Scope(person.GovernorateId);
        Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"محافظة غير صالحة.");
        db.Teams.Add(new(){Name=input.Name,GovernorateId=input.GovernorateId,Members=ids.Select(id=>new TeamMember{PersonId=id,IsLeader=id==input.LeaderId}).ToList()}); await db.SaveChangesAsync();
    }
    public async Task Deploy(long eventId,DeploymentInput input)
    {
        Demand(Permissions.TeamsDeploy); Validate(input);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var e=await Open(eventId); Require(e.Status is EventStatus.Activated or EventStatus.ResponseOngoing,"فعّل الحدث قبل نشر الفريق.");
        var team=await db.Teams.Include(x=>x.Members).ThenInclude(x=>x.Person).SingleOrDefaultAsync(x=>x.Id==input.TeamId)??throw new KeyNotFoundException(); Scope(team.GovernorateId);
        Require(team.State==TeamState.Ready&&team.Members.Count>0&&team.Members.Count(x=>x.IsLeader)==1,"الفريق غير جاهز أو لا يملك قائداً وأعضاء.");
        var ids=team.Members.Select(x=>x.PersonId).ToArray();
        Require(team.Members.All(x=>x.Person.IsAvailable),"أحد الأعضاء غير متاح.");
        Require(!await db.Deployments.AnyAsync(x=>x.ReturnedAt==null&&x.Team.Members.Any(m=>ids.Contains(m.PersonId))),"أحد الأعضاء مكلف بالفعل ضمن فريق آخر.");
        team.State=TeamState.Assigned;
        db.Deployments.Add(new(){EventId=eventId,TeamId=team.Id,Mission=input.Mission,ApprovedBy=user.Id,MembersSnapshot=JsonSerializer.Serialize(team.Members.Select(x=>new{x.PersonId,x.Person.Name,x.Person.Specialty,x.IsLeader}))});
        Timeline(e,"Deployed","تكليف الفريق: "+team.Name); await db.SaveChangesAsync(); if(tx!=null)await tx.CommitAsync();
    }
    public async Task DeploymentStatus(long id,VersionInput input,bool returned)
    {
        Demand(Permissions.TeamsDeploy); Validate(input); await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var d=await db.Deployments.Include(x=>x.Team).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); var e=await Open(d.EventId); Scope(d.Team.GovernorateId); CheckVersion(d,input.Version);
        Require(d.ReturnedAt==null,"أمر النشر منتهٍ.");
        if(returned){Require(d.ArrivedAt.HasValue,"سجل الوصول قبل العودة."); d.ReturnedAt=DateTime.UtcNow;d.Team.State=TeamState.Ready;}
        else {Require(d.ArrivedAt==null,"تم تسجيل الوصول بالفعل."); d.ArrivedAt=DateTime.UtcNow;d.Team.State=TeamState.OnSite;}
        Timeline(e,returned?"TeamReturned":"TeamArrived",d.Team.Name); await db.SaveChangesAsync(); if(tx!=null)await tx.CommitAsync();
    }
    public async Task<InventoryBoard> Inventory()
    {
        Demand(Permissions.InventoryView);
        var batches=await db.InventoryBatches.Include(x=>x.Warehouse).Include(x=>x.Item).Where(x=>user.GovernorateId==null||x.Warehouse.GovernorateId==user.GovernorateId).OrderBy(x=>x.ExpiresAt).Take(500).ToListAsync();
        var moves=await db.InventoryTransactions.Where(x=>user.GovernorateId==null||x.Batch.Warehouse.GovernorateId==user.GovernorateId).OrderByDescending(x=>x.Id).Take(100).Select(x=>new StockMovement(x.Id,x.Batch.Item.Name,x.Quantity,x.Reason,x.ApprovedBy,x.CreatedAt)).ToListAsync();
        var warehouses=await db.Warehouses.Where(x=>user.GovernorateId==null||x.GovernorateId==user.GovernorateId).OrderBy(x=>x.Name).Select(x=>new Lookup((int)x.Id,x.Name)).ToListAsync();
        var items=await db.InventoryItems.OrderBy(x=>x.Name).Select(x=>new Lookup((int)x.Id,x.Name)).ToListAsync();
        return new(batches.Select(x=>new StockRow(x.Id,x.Warehouse.Name,x.Item.Name,x.Item.Unit,x.BatchNumber,x.ExpiresAt,x.Balance,x.Item.Minimum,Version(x))).ToList(),moves,warehouses,items,await Governorates());
    }
    public async Task AddWarehouse(WarehouseInput input)
    {
        Demand(Permissions.InventoryManage); Validate(input); Scope(input.GovernorateId); Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"محافظة غير صالحة.");
        db.Warehouses.Add(new(){Name=input.Name,GovernorateId=input.GovernorateId}); await db.SaveChangesAsync();
    }
    public async Task AddItem(ItemInput input)
    {
        Demand(Permissions.InventoryManage); Validate(input); db.InventoryItems.Add(new(){Name=input.Name,Unit=input.Unit,Minimum=input.Minimum}); await db.SaveChangesAsync();
    }
    public async Task AddBatch(BatchInput input)
    {
        Demand(Permissions.InventoryManage); Validate(input);
        var w=await db.Warehouses.SingleOrDefaultAsync(x=>x.Id==input.WarehouseId)??throw new BusinessRuleException("المستودع غير موجود."); Scope(w.GovernorateId);
        Require(await db.InventoryItems.AnyAsync(x=>x.Id==input.ItemId),"الصنف غير موجود.");
        Require(input.ExpiresAt==null||input.ExpiresAt>DateTime.UtcNow.Date,"الدفعة منتهية الصلاحية.");
        var batch=new InventoryBatch{WarehouseId=input.WarehouseId,ItemId=input.ItemId,BatchNumber=input.BatchNumber,ExpiresAt=Utc(input.ExpiresAt),Balance=input.Quantity};
        db.InventoryBatches.Add(batch); db.InventoryTransactions.Add(new(){Batch=batch,Quantity=input.Quantity,Reason="رصيد افتتاحي للدفعة",ApprovedBy=user.Id}); await db.SaveChangesAsync();
    }
    public async Task MoveStock(long id,MovementInput input)
    {
        Demand(Permissions.InventoryManage); Validate(input);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var batch=await db.InventoryBatches.Include(x=>x.Warehouse).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); Scope(batch.Warehouse.GovernorateId); CheckVersion(batch,input.Version);
        if(input.EventId.HasValue) await Open(input.EventId.Value);
        if(input.Quantity<0) {Require(input.EventId.HasValue,"اربط الصرف بالحدث.");Require(batch.ExpiresAt==null||batch.ExpiresAt>=DateTime.UtcNow.Date,"لا يمكن صرف دفعة منتهية الصلاحية.");}
        batch.Balance=StockBalance(batch.Balance,input.Quantity);
        db.InventoryTransactions.Add(new(){BatchId=id,EventId=input.EventId,Quantity=input.Quantity,Reason=input.Reason,ApprovedBy=user.Id}); await db.SaveChangesAsync(); if(tx!=null)await tx.CommitAsync();
    }
    public async Task<FieldBoard> Field()
    {
        Demand(Permissions.FieldManage);
        var deployments=await DeploymentRows(db.Deployments.Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId));
        var reports=await db.FieldReports.Where(x=>user.GovernorateId==null||x.Deployment.Event.GovernorateId==user.GovernorateId).OrderByDescending(x=>x.Id).Take(100).Select(x=>new ActivityRow(x.Id,x.Deployment.Event.Title,x.Activities+" | الاحتياجات: "+x.Needs,x.ReportedBy,x.CreatedAt,"","")).ToListAsync();
        return new(deployments,reports);
    }
    public async Task AddFieldReport(FieldInput input)
    {
        Demand(Permissions.FieldManage); Validate(input); var d=await db.Deployments.SingleOrDefaultAsync(x=>x.Id==input.DeploymentId)??throw new KeyNotFoundException();var e=await Load(d.EventId);
        Require(input.Latitude.HasValue==input.Longitude.HasValue,"أدخل الإحداثيين معاً أو اتركهما فارغين.");
        if(input.ClientRequestId!=Guid.Empty){var previous=await db.FieldReports.SingleOrDefaultAsync(x=>x.ClientRequestId==input.ClientRequestId);if(previous!=null){Require(previous.ReportedBy==user.Id&&previous.DeploymentId==input.DeploymentId&&previous.Activities==input.Activities&&previous.Needs==input.Needs&&previous.Latitude==input.Latitude&&previous.Longitude==input.Longitude,"معرف الطلب مستخدم لمحتوى مختلف.");return;}}
        Require(e.Status!=EventStatus.Closed&&d.ReturnedAt==null,"الحدث مغلق أو انتهى أمر النشر.");
        db.FieldReports.Add(new(){ClientRequestId=input.ClientRequestId==Guid.Empty?Guid.NewGuid():input.ClientRequestId,DeploymentId=d.Id,Activities=input.Activities,Needs=input.Needs,Latitude=input.Latitude,Longitude=input.Longitude,ReportedBy=user.Id}); Timeline(e,"FieldReport","إضافة تقرير ميداني"); await db.SaveChangesAsync();
    }
}
