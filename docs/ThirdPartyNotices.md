# المكتبات والبيانات

| المكون | الإصدار المستخدم | الترخيص / المصدر |
| --- | --- | --- |
| .NET / ASP.NET Core | net10.0 | MIT؛ Microsoft |
| EF Core SQL Server وIdentity EF | 10.0.9 | MIT وفق nuspec المحلية |
| System.Security.Cryptography.Pkcs | 10.0.13 | MIT؛ تثبيت مباشر لمعالجة اعتماد ضعيف غير مباشر |
| Serilog.AspNetCore | 8.0.3 | Apache-2.0 وفق nuspec؛ تشغيل console فقط |
| Serilog وConsole sink | 4.0.0 و6.0.0 | Apache-2.0 |
| ClosedXML | 0.105.0 | MIT وفق nuspec؛ Excel |
| SignalR browser client | 8.0.7 | MIT؛ نسخة محلية من حزمة Microsoft |
| Leaflet | 1.9.4 | BSD-2-Clause؛ LICENSE مضمن مع الملفات المحلية |
| حدود مصر 110m | Natural Earth | Public domain، وليست حدوداً إدارية رسمية |
| xUnit / Microsoft Test SDK | 2.9.3 / 17.14.1 | أدوات اختبار فقط |
| Playwright | 1.64.1 | Apache-2.0؛ اختبار بمتصفح Chrome المثبت |

لم تُضف QuestPDF لتجنب افتراض أهلية ترخيص غير متحقق منها. PDF متاح من طباعة المتصفح. Word يُنشأ كحزمة Office Open XML، وExcel بواسطة ClosedXML. يستخدم التصميم خطوط النظام الداعمة للعربية دون تحميل خطوط خارجية.

مصادر: [EF SQL provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)، [Natural Earth](https://www.naturalearthdata.com/about/terms-of-use/)، [Leaflet](https://leafletjs.com/)، [مرجع تواريخ 7-1-7](https://717alliance.org/wp-content/uploads/2024/05/717-milestones-reference-guide.pdf)، [أداة حساب 7-1-7](https://717alliance.org/wp-content/uploads/2024/03/7-1-7-Asssessment-Tool-.pdf).

توجد قائمة الاعتمادات المنشورة وتراخيصها في DependencyLicenses.tsv، ونصوص التراخيص والإشعارات في licenses/. حزمة Microsoft.Data.SqlClient.SNI.runtime تخضع لشروط Microsoft الخاصة المضمنة في sqlclient-sni-LICENSE.txt؛ ليست كل مكونات SqlClient بترخيص MIT. يلزم الاحتفاظ بهذه الشروط عند توزيع التطبيق.

## مادة QIRA المقتبسة

تتضمن وحدة QIRA أسئلة وخوارزمية مشتقة من دليل WHO بترخيص CC BY-NC-SA 3.0 IGO. يلزم مراعاة شروط النسبة وعدم الاستخدام التجاري والمشاركة بالمثل للمادة المشتقة. تفاصيل الإحالة وإخلاء مسؤولية الترجمة في [QiraSourceNotice.md](QiraSourceNotice.md). لا تستخدم الوحدة شعار WHO ولا تعني اعتمادها للتطبيق.
