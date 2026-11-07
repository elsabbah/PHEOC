using PHEOC.OMS.Domain.Qira;
namespace PHEOC.OMS.Web.ViewSupport;
public static class QiraTree
{
    // Presentation coordinates only. All edges and results come from the saved definition.
    public static (int Column,double Row) Position(QiraNode n)=>n.Id switch
    {
        "Q1"=>(0,0),"Q2"=>(1,0),"Q3"=>(2,1),"Q41"=>(3,0),
        "Q42-L"=>(3,1),"Q42-H"=>(3,2),"Q51"=>(4,3),
        "C52-VL"=>(5,0),"C52-LM"=>(5,1),"C52-MH"=>(5,2),"C52-HV"=>(5,3.5),
        _=>(Math.Clamp(n.Axis-1,0,5),0)
    };
    public static string Target(QiraNode n,QiraChoice c)=>c switch{QiraChoice.Yes=>n.Yes,QiraChoice.No=>n.No,_=>n.Unknown};
}
