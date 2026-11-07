using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Integration.Ebs;
namespace PHEOC.OMS.Web.Controllers;
[ApiController,Route("api/integration/ebs/events"),AllowAnonymous,IgnoreAntiforgeryToken,EnableRateLimiting("ebs")]
public class EbsController(IEventIntake intake,IConfiguration config,IIntegrationStatus status) : ControllerBase
{
    private bool Authenticated()
    {
        var expected=config["Integration:Ebs:ApiKey"];var supplied=Request.Headers["X-Api-Key"].ToString();return !string.IsNullOrWhiteSpace(expected)&&expected.Length>=32&&CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));
    }
    [HttpPost,RequestSizeLimit(65536)]
    public async Task<IActionResult> Receive([FromBody]EbsEnvelope envelope,[FromHeader(Name="Idempotency-Key"),Required,StringLength(128)]string key)
    {
        if(!Authenticated())return Unauthorized();
        var result=await intake.Receive("EBS",key,envelope.ExternalEventId,envelope.Event);return StatusCode(result.Duplicate?200:201,result);
    }
    [HttpGet("{id:guid}")]public async Task<IActionResult> Status(Guid id)=>!Authenticated()?Unauthorized():Ok(await status.Status(id));
    [HttpPost("{id:guid}/status")]public async Task<IActionResult> QueueStatus(Guid id){if(!Authenticated())return Unauthorized();await status.QueueStatus(id);return Accepted(new{message="تم وضع تحديث الحالة في طابور الإرسال"});}
}
