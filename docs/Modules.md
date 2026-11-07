# الوحدات

| الوحدة | المسار | الخدمة الأساسية |
| --- | --- | --- |
| القيادة وCOP | / و/Dashboard/Wall | OmsService.Dashboard، OperationsHub |
| الأحداث والمخاطر والتفعيل وIMS والمهام والقرارات | /Events | OmsService.cs |
| RRT والأفراد والنشر | /Teams | TeamsAndInventory.cs |
| المخزون والتحويل | /Inventory | TeamsAndInventory.cs، LogisticsService.cs |
| طلبات الإمداد والمركبات | /Logistics | LogisticsService.cs |
| الميدان وPWA | /Field | TeamsAndInventory.cs، field-drafts.js، sw.js |
| المخالطون والعينات | /Health | HealthAndReporting.cs |
| التقارير | /Reports | HealthAndReporting.cs، ReportExporter.cs |
| 7-1-7 | /Analytics | AnalyticsService.cs، SevenOneSeven.cs |
| SLA والإشعارات | /Communications | CoordinationService.cs، SlaEngine.cs |
| الرسائل والمرفقات | داخل صفحة الحدث | CoordinationService.cs |
| الخريطة | /Map | CoordinationService.MapEvents، map.js |
| مصدر EgyptEBS | /EgyptEbs | EgyptEbsReader.cs، EgyptEbsSync.cs |
| السياسات والإغلاق وAAR | /Policies وداخل الحدث و/Events/Improvements | PolicyService.cs، HealthAndReporting.cs |
| المستخدمون والتدقيق والتكامل | /Admin | IdentityServices.cs، خدمات القراءة |

المسارات ليست إثباتاً لاكتمال كل تفصيل في البرومبت؛ حالة الاختبارات والحدود التفصيلية في DevelopmentProgress.md.
