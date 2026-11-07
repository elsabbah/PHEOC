using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Infrastructure.Services;

public record EgyptEbsServerOverride(string Server,string Revision,DateTime UpdatedAtUtc,string UpdatedBy,string? ProtectedCredentials=null);
public record EgyptEbsCredentials(string UserName,string Password);
public interface IEgyptEbsServerStore
{
    Task<EgyptEbsServerOverride?> Read();
    Task Save(string server,string revision,string actor,string? protectedCredentials=null);
}
// Never stores a connection string or plaintext credentials; optional credentials are protected before saving.
public sealed class EgyptEbsServerFileStore(string path) : IEgyptEbsServerStore
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<EgyptEbsServerOverride?> Read()
    {
        if(!File.Exists(path))return null;
        return JsonSerializer.Deserialize<EgyptEbsServerOverride>(await File.ReadAllTextAsync(path))
            ??throw new BusinessRuleException("تعذر قراءة إعداد خادم EgyptEBS المحفوظ.");
    }
    public async Task Save(string server,string revision,string actor,string? protectedCredentials=null)
    {
        await gate.WaitAsync();
        string? temporary=null;
        try
        {
            var current=await Read();
            OperationalRules.Require((current?.Revision??"")==revision,"تغير إعداد الخادم بواسطة مستخدم آخر؛ أعد فتح الصفحة قبل الحفظ.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            await File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(new EgyptEbsServerOverride(server,Guid.NewGuid().ToString("N"),DateTime.UtcNow,actor,protectedCredentials)));
            File.Move(temporary,path,true);
        }
        finally { if(temporary!=null&&File.Exists(temporary))File.Delete(temporary);gate.Release(); }
    }
}
public interface IEgyptEbsConnectionSource { Task<string?> ConnectionString(); }
public sealed class EgyptEbsConnectionSource(IConfiguration config,IEgyptEbsServerStore store,IDataProtectionProvider protection) : IEgyptEbsConnectionSource
{
    public const string ProtectionPurpose="PHEOC-OMS.EgyptEBS.ConnectionCredentials.v1";
    public async Task<string?> ConnectionString()
    {
        var original=config.GetConnectionString("EgyptEbsReader");
        if(string.IsNullOrWhiteSpace(original))return null;
        var setting=await store.Read();
        if(setting==null)return original;
        var candidate=Build(original,setting.Server);
        return ApplyCredentials(candidate,setting.ProtectedCredentials,protection);
    }
    public static string ApplyCredentials(string candidate,string? encrypted,IDataProtectionProvider protection)
    {
        if(string.IsNullOrWhiteSpace(encrypted))return candidate;
        var credentials=JsonSerializer.Deserialize<EgyptEbsCredentials>(protection.CreateProtector(ProtectionPurpose).Unprotect(encrypted))
            ??throw new BusinessRuleException("تعذر قراءة بيانات اتصال EgyptEBS المحمية.");
        var cs=new SqlConnectionStringBuilder(candidate){IntegratedSecurity=false,UserID=credentials.UserName,Password=credentials.Password};
        return cs.ConnectionString;
    }
    public static string Build(string original,string server)
    {
        OperationalRules.Require(!string.IsNullOrWhiteSpace(server)&&server.Length<=255&&
            Regex.IsMatch(server,@"^(?:tcp:)?(?:[A-Za-z0-9_.-]+|\[[0-9A-Fa-f:]+\]|\(local\))(?:\\[A-Za-z0-9_$.-]+)?(?:,[0-9]{1,5})?$",RegexOptions.CultureInvariant),
            "أدخل اسم خادم SQL أو عنوان IP صالحاً، مثل SERVER\\INSTANCE أو 192.168.1.10,1433.");
        var cs=new SqlConnectionStringBuilder(original);
        OperationalRules.Require(cs.InitialCatalog.Equals("EgyptEBS",StringComparison.OrdinalIgnoreCase),"قاعدة المصدر يجب أن تكون EgyptEBS.");
        cs.DataSource=server;return cs.ConnectionString;
    }
}
public interface IEgyptEbsConnectionProbe { Task Check(string connectionString); }
public sealed class EgyptEbsConnectionProbe : IEgyptEbsConnectionProbe
{
    public async Task Check(string connectionString)
    {
        var opening=true;
        try
        {
        var cs=new SqlConnectionStringBuilder(connectionString){ConnectTimeout=20};
        await using var connection=new SqlConnection(cs.ConnectionString);
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await connection.OpenAsync(timeout.Token);
        opening=false;
        await using var command=connection.CreateCommand();command.CommandTimeout=10;
        command.CommandText="""
        SELECT TOP (0) Id,DeletedAt,ModifiedAt,CreatedAt,DetailedAddress,SignalInformation,EventStartDate,DetectionDate FROM dbo.Signals;
        SELECT TOP (0) Id,SignalId,IsValidId,[Date],DeletedAt,ModifiedAt,CreatedAt FROM dbo.Verifications;
        SELECT TOP (0) Id,Code FROM dbo.YesNoCodeTables;
        SELECT TOP (0) Id,SignalId,SignalTypeId,RelevanceId,TriageDate,DeletedAt,ModifiedAt,CreatedAt FROM dbo.Triages;
        SELECT TOP (0) Id,Code FROM dbo.RelevanceCodeTables;
        SELECT TOP (0) Id,SignalId,LocationId,DeletedAt,ModifiedAt,CreatedAt FROM dbo.SignalLocations;
        SELECT TOP (0) Id,NameAR,ParentId,LevelId,DeletedAt FROM dbo.AdministrativeLevels;
        SELECT TOP (0) Id,Code FROM dbo.AdministrativeLevelCodeTables;
        SELECT TOP (0) Id,NameAR,SignalCategoryId FROM dbo.SignalTypes;
        SELECT TOP (0) Id,NameAR FROM dbo.SignalCategoryCodeTables;
        """;
        await using var reader=await command.ExecuteReaderAsync(timeout.Token);
        while(await reader.NextResultAsync(timeout.Token)) { }
        }
        catch(SqlException ex){throw new BusinessRuleException(Failure(opening,ex.Number));}
        catch(OperationCanceledException){throw new BusinessRuleException("انتهت مهلة اختبار الاتصال. تحقق من منفذ مثيل SQL Server وإتاحة اتصال TCP/IP. لم تتغير الإعدادات المحفوظة.");}
    }
    public static string Failure(bool opening,int number)
    {
        var detail=number switch
        {
            5 or 10013 when opening=>"رفض Windows اتصال الشبكة من عملية التطبيق (Access denied). تحقق من قيود بيئة تشغيل التطبيق أو سياسة الحماية الخاصة بعمليته. نجاح Test-NetConnection من نافذة أخرى لا يثبت أن عملية التطبيق مسموح لها بالاتصال.",
            -2 when opening=>"انتهت مهلة فتح اتصال SQL أو المصافحة قبل تسجيل الدخول. نجاح فحص منفذ TCP لا يثبت اكتمال هذه الخطوة؛ راجع استجابة SQL وسجل الاتصال على الخادم.",
            -2=>"انتهت مهلة قراءة مخطط جداول EgyptEBS بعد فتح الاتصال. راجع استجابة قاعدة المصدر.",
            18456 or 18452=>"تم الوصول إلى SQL Server، لكنه رفض حساب القراءة. استخدم حساباً صالحاً على الخادم المستهدف وكلمة مروره الصحيحة، أو راجع المسؤول عن الحساب.",
            4060=>"تم الوصول إلى SQL Server، لكن تعذر فتح قاعدة EgyptEBS. تحقق من وجود القاعدة وربط حساب القراءة بها.",
            229 or 230=>"تم الاتصال بالخادم، لكن حساب القراءة لا يملك صلاحية SELECT المطلوبة على جداول EgyptEBS.",
            208=>"تم الاتصال بالخادم، لكن أحد جداول EgyptEBS المطلوبة غير موجود أو غير متاح للحساب.",
            207=>"تم الاتصال بالخادم، لكن مخطط أعمدة EgyptEBS لا يطابق الحقول المطلوبة للتكامل.",
            _=>opening?"تعذر فتح اتصال SQL. تحقق من منفذ المثيل وإعدادات TCP/IP والجدار الناري وشهادة الاتصال.":"تم الاتصال بالخادم، لكن تعذر قراءة مخطط جداول EgyptEBS. راجع صلاحيات الحساب وتوافق المخطط."
        };
        return detail+$" (رمز SQL: {number}). لم تتغير الإعدادات المحفوظة.";
    }
}
public class EgyptEbsServerSettings(IConfiguration config,IEgyptEbsServerStore store,
    IEgyptEbsConnectionProbe probe,ICurrentUser user,IDataProtectionProvider protection) : IEgyptEbsServerSettings
{
    private void Demand() { if(!user.Can(Permissions.AdminSettings)||user.GovernorateId!=null)throw new AccessDeniedException(); }
    public async Task<EgyptEbsServerView> Get()
    {
        Demand();var original=config.GetConnectionString("EgyptEbsReader");var state=await store.Read();
        return new(state?.Server??(string.IsNullOrWhiteSpace(original)?"":new SqlConnectionStringBuilder(original).DataSource),state?.Revision??"",!string.IsNullOrWhiteSpace(original));
    }
    private async Task<(string Server,string? Credentials)> Validate(EgyptEbsServerInput input)
    {
        Demand();var original=config.GetConnectionString("EgyptEbsReader");
        OperationalRules.Require(!string.IsNullOrWhiteSpace(original),"يجب تهيئة حساب قراءة EgyptEBS أولاً بواسطة مسؤول التشغيل.");
        var candidate=EgyptEbsConnectionSource.Build(original!,input.Server?.Trim()??"");
        var current=await store.Read();
        OperationalRules.Require((current?.Revision??"")== (input.Revision??""),"تغير إعداد الخادم؛ أعد فتح الصفحة قبل الاختبار أو الحفظ.");
        string? credentials=input.UseOriginalAccount?null:current?.ProtectedCredentials;
        var hasUser=!string.IsNullOrWhiteSpace(input.UserName);var hasPassword=!string.IsNullOrEmpty(input.Password);
        OperationalRules.Require(!input.UseOriginalAccount||(!hasUser&&!hasPassword),"اختر حساب إعداد التشغيل أو أدخل حساباً بديلاً، وليس كليهما.");
        OperationalRules.Require(hasUser==hasPassword,"لتغيير حساب القراءة أدخل اسم المستخدم وكلمة المرور معاً. اتركهما فارغين للاحتفاظ بالحساب الحالي.");
        if(hasUser)
            credentials=protection.CreateProtector(EgyptEbsConnectionSource.ProtectionPurpose).Protect(JsonSerializer.Serialize(new EgyptEbsCredentials(input.UserName!.Trim(),input.Password!)));
        candidate=EgyptEbsConnectionSource.ApplyCredentials(candidate,credentials,protection);
        try { await probe.Check(candidate); }
        catch(Exception ex)when(ex is SqlException or OperationCanceledException)
        { throw new BusinessRuleException(ex is SqlException sql?EgyptEbsConnectionProbe.Failure(true,sql.Number):"انتهت مهلة اختبار الاتصال. لم تتغير الإعدادات المحفوظة."); }
        return (new SqlConnectionStringBuilder(candidate).DataSource,credentials);
    }
    public async Task Test(EgyptEbsServerInput input)=>_ = await Validate(input);
    public async Task Save(EgyptEbsServerInput input)
    {
        var candidate=await Validate(input);
        try { await store.Save(candidate.Server,input.Revision??"",user.Id,candidate.Credentials); }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException)
        { throw new BusinessRuleException("تعذر حفظ إعداد الخادم؛ راجع صلاحية الكتابة إلى مجلد إعدادات التطبيق."); }
    }
}
