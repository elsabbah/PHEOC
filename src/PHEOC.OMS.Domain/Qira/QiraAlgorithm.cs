using System.Text.Json;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Domain.Qira;

public enum QiraChoice { Yes=1,No=2,Unknown=3 }
public enum QiraRisk { VeryLow=1,Low=2,Moderate=3,High=4,VeryHigh=5 }
public enum QiraConfidence { High=1,Moderate=2,Low=3 }
public enum QiraState { New=0,InProgress=1,Completed=2,Reviewed=3,Approved=4,Deleted=5 }
public record QiraNode(string Id,int Axis,string Number,string Arabic,string English,string Guidance,string Yes,string No,string Unknown);
public record QiraDocument(string Version,string Source,IReadOnlyList<string> Axes,IReadOnlyList<QiraNode> Nodes);
public record QiraEvaluation(IReadOnlyList<string> Path,string? Next,QiraRisk? Risk,IReadOnlyList<string> UnknownNodes);
public static class QiraAlgorithm
{
    public const string Version="WHO-QIRA-2026-02-Fig2-v1";
    public const string Source="https://www.who.int/southeastasia/internal-publications-detail/WHE2602261";
    private const string CapacityAr="هل تتوافر قدرات كافية لتدابير الوقاية والسيطرة؟";
    private const string CapacityEn="Are capacities for prevention and control measures in place?";
    private const string CapacityHelp="راجع توافر التدابير الفعالة، وقدرات الترصد والمختبرات، والعلاج والموارد والوعي المجتمعي، ومواطن الضعف أو العوائق. لا يكفي وجود خطة على الورق إذا كانت غير قابلة للتطبيق.";
    public static QiraDocument Current=>new(Version,Source,["طبيعة الخطر","التعرض","شدة التأثير الصحي","الانتشار وحجم التأثير","القدرات ومواطن الضعف"],[
        new("Q1",1,"1","هل يتضمن الحدث خطراً محدداً مسبقاً كخطر عالِ التهديد في المنطقة محل التقييم؟","Does the signal or event involve a hazard predefined as a high threat in your geographical area?","ارجع إلى القائمة الوطنية أو المحلية المعتمدة. عند غياب القائمة اختر غير معروف. للخطر المستجد أو المجهول يحدد الفريق مسار نعم/لا مع توثيق المبرر.","C52-HV","Q2","Q2"),
        new("Q2",2,"2","هل يُرجح حدوث تعرض إضافي للخطر؟","Is further exposure likely to take place?","راجع استمرار وجود الخطر، وإمكان تعرض الناس له، وقابلية السكان للإصابة عند التعرض. قيّم مسارات الانتقال والمناعة والتدابير القائمة.","Q3","Q41","Q3"),
        new("Q3",3,"3","هل يُرجح أن يكون المرض متوسطاً إلى شديد بين الحالات في هذه الفئة السكانية؟","Is the disease likely to be moderate to severe among cases in this population?","راجع الوفيات وشدة المرض والمضاعفات والدخول للمستشفيات والرعاية المركزة مقارنة بالتوقعات والسياق المعروف للخطر. لا توجد عتبة عددية عامة واحدة لكل الأمراض.","Q42-H","Q42-L","Q42-H"),
        new("Q41",4,"4.1","هل يتأثر حالياً عدد يُعد كبيراً من الناس؟","Are a significant number of people currently affected?","قارن العدد بما يُتوقع عادةً من حدث مماثل في السكان والمكان والوقت؛ يُسأل هذا السؤال عندما لا يُرجح تعرض إضافي.","C52-VL","@VeryLow","C52-VL"),
        new("Q42-L",4,"4.2","هل يُتوقع مستقبلاً عدد كبير من الحالات أو انتشار جغرافي واسع؟","Could a high number of cases or substantial geographical spread be expected in the future?","راجع قابلية الخطر للانتشار وتكرار المخالطة، ومعدل الهجوم، والتسارع غير المعتاد مقارنة بخط أساس مناسب. سجّل عدم اليقين إذا كانت المعلومات غير كافية.","C52-LM","C52-VL","C52-LM"),
        new("Q42-H",4,"4.2","هل يُتوقع مستقبلاً عدد كبير من الحالات أو انتشار جغرافي واسع؟","Could a high number of cases or substantial geographical spread be expected in the future?","راجع قابلية الخطر للانتشار وتكرار المخالطة، ومعدل الهجوم، والتسارع غير المعتاد مقارنة بخط أساس مناسب.","Q51","C52-LM","Q51"),
        new("Q51",5,"5.1","هل يُرجح أن تتجاوز الاحتياجات قدرة نظام الرعاية الصحية؟","Is the health care system likely to be overwhelmed?","قارن الاحتياجات الحالية والمتوقعة إلى الاستشفاء والرعاية المركزة مع الأسرة والقوى البشرية المدربة والمعدات والأدوية والسلع الصحية المتاحة.","C52-HV","C52-MH","C52-HV"),
        new("C52-VL",5,"5.2",CapacityAr,CapacityEn,CapacityHelp,"@VeryLow","@Low","@Low"),
        new("C52-LM",5,"5.2",CapacityAr,CapacityEn,CapacityHelp,"@Low","@Moderate","@Moderate"),
        new("C52-MH",5,"5.2",CapacityAr,CapacityEn,CapacityHelp,"@Moderate","@High","@High"),
        new("C52-HV",5,"5.2",CapacityAr,CapacityEn,CapacityHelp,"@High","@VeryHigh","@VeryHigh")
    ]);
    public static string Snapshot=>JsonSerializer.Serialize(Current);
    public static QiraDocument Read(string json)=>JsonSerializer.Deserialize<QiraDocument>(json)??throw new BusinessRuleException("تعذر قراءة إصدار خوارزمية QIRA.");
    public static QiraEvaluation Evaluate(QiraDocument definition,IReadOnlyDictionary<string,QiraChoice> answers)
    {
        var nodes=definition.Nodes.ToDictionary(n=>n.Id);var path=new List<string>();var unknown=new List<string>();var current="Q1";
        while(!current.StartsWith('@'))
        {
            OperationalRules.Require(path.Count<nodes.Count&&!path.Contains(current)&&nodes.ContainsKey(current),"تعريف مسار QIRA غير صالح.");
            path.Add(current);var node=nodes[current];
            if(!answers.TryGetValue(current,out var answer))return new(path,current,null,unknown);
            OperationalRules.Require(Enum.IsDefined(answer),"إجابة QIRA غير صالحة.");
            if(answer==QiraChoice.Unknown)unknown.Add(current);
            current=answer switch{QiraChoice.Yes=>node.Yes,QiraChoice.No=>node.No,_=>node.Unknown};
        }
        OperationalRules.Require(Enum.TryParse<QiraRisk>(current[1..],out var level)&&Enum.IsDefined(level),"نتيجة QIRA غير صالحة.");
        return new(path,null,level,unknown);
    }
}
