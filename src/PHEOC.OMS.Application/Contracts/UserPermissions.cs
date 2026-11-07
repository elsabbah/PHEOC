using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class UserAccountDeleteInput
{
    [Required,StringLength(100)]public string Version{get;set;}="";
}
public record UserPermissionEdit(string Id,string Name,int? GovernorateId,IReadOnlyList<string> Roles,IReadOnlyList<string> Permissions,string Version);
public class UserPermissionsInput
{
    [Required,StringLength(100)]public string Version{get;set;}="";
    [Required,MinLength(1)]public string[] Permissions{get;set;}=[];
}
