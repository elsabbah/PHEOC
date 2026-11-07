using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ClosedXML.Excel;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Reporting;
public class ReportExporter : IReportExporter
{
    public byte[] Excel(IReadOnlyList<ReportRow> reports)
    {
        using var workbook=new XLWorkbook();var sheet=workbook.Worksheets.Add("التقارير الموقفية");sheet.RightToLeft=true;
        string[] headers=["الحدث","الإصدار","الملخص","التحديات","الإجراءات القادمة","الإعداد","الاعتماد","التوقيت UTC"];
        for(var c=0;c<headers.Length;c++)sheet.Cell(1,c+1).Value=headers[c];
        var row=2;foreach(var r in reports)
        {
            object[] values=[r.EventTitle,r.Version,r.Summary,r.Challenges,r.NextActions,r.PreparedBy,r.ApprovedBy??"غير معتمد",r.At.ToString("u")];
            for(var c=0;c<values.Length;c++)sheet.Cell(row,c+1).Value=XLCellValue.FromObject(values[c]);row++;
        }
        sheet.Row(1).Style.Font.Bold=true;sheet.Columns().Width=28;sheet.Style.Alignment.WrapText=true;
        using var stream=new MemoryStream();workbook.SaveAs(stream);return stream.ToArray();
    }
    public byte[] Word(ReportRow report)
    {
        using var stream=new MemoryStream();using(var archive=new ZipArchive(stream,ZipArchiveMode.Create,true))
        {
            Add(archive,"[Content_Types].xml","<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Add(archive,"_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            string[] lines=["PHEOC-OMS — تقرير موقفي",report.EventTitle,$"الإصدار: {report.Version} | UTC: {report.At:u}",report.Summary,"التحديات: "+report.Challenges,"الإجراءات القادمة: "+report.NextActions,"المُعد: "+report.PreparedBy,"الاعتماد: "+(report.ApprovedBy??"غير معتمد")];
            var doc=new XDocument(new XElement(w+"document",new XAttribute(XNamespace.Xmlns+"w",w),new XElement(w+"body",lines.Select(line=>new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"bidi")),new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rtl")),new XElement(w+"t",line)))))));
            Add(archive,"word/document.xml",doc.ToString());
        }return stream.ToArray();
    }
    private static void Add(ZipArchive zip,string name,string content) { using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(content); }
}
