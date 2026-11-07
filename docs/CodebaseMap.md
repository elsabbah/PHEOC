# خريطة مكونات PHEOC-OMS

آخر تحديث: 2026-11-06. الجذر: D:\MyAI\PHEOC2. هذا وصف للكود الموجود، ويحل محل خريطة المرحلة صفر.

## نقاط البداية

- `PHEOC.OMS.sln`: ستة مشاريع تطبيق ومشروعا اختبار، net10.0.
- `src/PHEOC.OMS.Web/Program.cs`: DI وIdentity والصلاحيات وSecurity headers وRate limiting ومسارات MVC/API/SignalR وأوامر CLI. لا migrations عند بدء التشغيل العادي.
- `src/PHEOC.OMS.Infrastructure/Data/OmsDbContext.cs`: مخطط EF وIdentity والقيود والفهارس والتدقيق ومنع التعديل الصامت.
- `src/PHEOC.OMS.Infrastructure/Data/Migrations/`: سبع migrations مطبقة؛ `database/001-InitialSchema.sql` نسخة SQL قابلة للمراجعة.
- `scripts/Start-Development.ps1`: تشغيل محلي؛ `Initialize-Development.ps1`: تهيئة صريحة؛ `Enable-EgyptEbsReader.ps1`: صلاحيات المصدر المصرح بها.

## المشاريع

| المشروع | الموجود |
| --- | --- |
| Domain | Events/EmergencyEvent.cs؛ Operations للفرق والمخزون والميدان والمخالطين والعينات والتقارير والسياسات وSLA وEBS sync؛ Rules للانتقالات والمخزون و7-1-7 |
| Application | Contracts: DTOs/ViewModels ومدخلات التحقق وواجهات الخدمات؛ Security: الصلاحيات وICurrentUser |
| Infrastructure | Data/Identity/Migrations؛ Services مقسمة حسب الوحدات، مع EF SQL Server 2016 ومعاملات وصلاحيات ونطاقات |
| Integration | Ebs/EbsEnvelope.cs لعقد Push المقترح؛ القراءة المباشرة موجودة في Infrastructure لأنها تعتمد على SQL وDataProtection |
| Reporting | ReportExporter.cs: Excel عبر ClosedXML وWord بصيغة Open XML |
| Web | Controllers منفصلة، Views عربية، wwwroot/css وjs محلية، Realtime للـHub والعمال |
| UnitTests | OperationalRulesTests وSevenOneSevenTests |
| IntegrationTests | HttpSecurityTests وSqlPersistenceTests وOperationalSqlTests؛ تعطيل التوازي بين اختبارات SQL ذات المعاملات |

## ربط المهام بالملفات

| المهمة | الملف / المجلد |
| --- | --- |
| الأحداث والمخاطر وIMS والمهام والقرارات | Infrastructure/Services/OmsService.cs وWeb/Controllers/EventsController.cs |
| الأفراد والفرق والنشر والمخزون والميدان | Infrastructure/Services/TeamsAndInventory.cs |
| الإمداد والتحويل والمركبات | Infrastructure/Services/LogisticsService.cs |
| المخالطون والعينات والتقارير والإغلاق وAAR | Infrastructure/Services/HealthAndReporting.cs |
| سياسة الإغلاق | Infrastructure/Services/PolicyService.cs |
| 7-1-7 والأدلة | Domain/Rules/SevenOneSeven.cs وInfrastructure/Services/AnalyticsService.cs |
| SLA | Infrastructure/Services/SlaEngine.cs وWeb/Realtime/SlaWorker.cs |
| الرسائل والمرفقات والخريطة | Infrastructure/Services/CoordinationService.cs |
| مصدر EgyptEBS | Infrastructure/Services/EgyptEbsReader.cs وEgyptEbsSync.cs وWeb/Realtime/EgyptEbsWorker.cs |
| Push API ومنع التكرار | Infrastructure/Services/EventIntake.cs وIntegration/Ebs وWeb/Controllers/EbsController.cs |
| تحديثات الخارج | Web/Realtime/OutboxWorker.cs؛ يحتاج CallbackUrl فعلياً |
| المستخدمون | Infrastructure/Services/IdentityServices.cs وWeb/Security وAdminController.cs |
| التصميم | Web/Views/Shared/_Layout.cshtml وWeb/wwwroot/css/site.css |
| PWA | Web/wwwroot/sw.js وmanifest.webmanifest وjs/field-drafts.js |
| خريطة محلية | Web/wwwroot/js/map.js وdata/egypt.geojson وjs/vendor/leaflet |

المسارات المختصرة أعلاه نسبية إلى `src/PHEOC.OMS.` للمشروع المسمى. لا تفترض أسماء أو حقولاً غير موجودة؛ افتح الملف ذي الصلة قبل التعديل.

## التحقق والتوثيق

تواريخ المؤشر من EgyptEBS: Contracts/Analytics.cs يضيف CurrentDates وHasEgyptEbsSource وعقد IEgyptEbsKpiDateSource؛ Infrastructure/Services/EgyptEbsKpiDateSource.cs لقراءة تواريخ Signals وأقدم Responses.CreatedAt غير محذوف. AnalyticsService.KpiSourceDates يفوض حسب Reports.View ونطاق الحدث ويربط Inbox بالمصدر، دون حفظ. AnalyticsController.SourceDates تغذية JSON بلا تخزين، Views/Analytics/Event.cshtml وwwwroot/js/kpi-source-dates.js للمعاينة وملء الحقول الفارغة مع مرجع المصدر، وحفظها عبر إجراء Dates القائم. تنسيق الهاتف محصور بـdata-kpi-source في site.css. KpiSourceDatesTests وKpiSourceBrowserTests وscripts/browser-kpi-source-check.mjs للتحقق المعزول المتراجع؛ scripts/browser-kpi-source-live-check.mjs للحدث الحقيقي دون حفظ. مضاهاة المصدر وحدودها في API-Documentation.md.

أوامر التشغيل في README.md، ونتائج الفحص في docs/Verification.md. بيانات تعريف الجداول الحية في database/SchemaInventory.tsv. الاختبارات الآلية تستخدم `PHEOC_TEST_SQL`، واختبار قارئ المصدر يستخدم `PHEOC_EBS_SQL`؛ سكربت Test-Application.ps1 يحمّلهما من الأسرار دون طباعتهما.

الحالة التفصيلية والحدود في DevelopmentProgress.md؛ وتراخيص المكتبات في ThirdPartyNotices.md. تبقى خطتا المرحلة صفر في InitialArchitecturePlan.md وInitialDatabasePlan.md كمرجع تاريخي، وليستا وصفاً للمخطط المطبق.
### اختبارات مراجعة المزامنة (2026-11-06)

- tests/PHEOC.OMS.IntegrationTests/AutomaticReceptionTests.cs: إرسال نموذج التفعيل والإيقاف الفعلي عبر HTTP.
- tests/PHEOC.OMS.IntegrationTests/EgyptEbsSyncTests.cs: الدفعات والتقدم والفشل وإعادة المحاولة، مع SQL ومعاملات Rollback واستعلام المصدر الحقيقي للقراءة فقط.
- Application/Contracts/EgyptEbs.cs: منفذ IEgyptEbsSyncSource للعامل، مستقل عن صلاحيات واجهة القارئ التفاعلية.
- docs/AutomaticReceptionReview.md: النتيجة والقيود وطريقة تطبيق الإصلاح.

## تنبيهات وصول الأحداث (2026-11-06)

- Application/Contracts/EventAlerts.cs وInfrastructure/Services/EventAlertService.cs: تغذية مقيدة بالصلاحية والمحافظة وقراءة محفوظة لكل مستخدم.
- Domain/Events/EventArrival.cs وEventIntake.cs وOmsService.cs: إنشاء إشعار الوصول ضمن معاملة الحدث.
- Web/Controllers/EventAlertsController.cs وRealtime/EventArrivalBroadcast.cs: التغذية والقراءة والبث عبر Outbox/SignalR.
- Web/Views/Shared/_EventAlerts.cshtml وwwwroot/js/event-alerts.js وcss/event-alerts.css: الجرس والعداد والقائمة والحركة والصوت.
- EventAlertSqlTests وEventArrivalBroadcastTests وEventAlertBrowserTests وscripts/browser-event-alerts-check.mjs: SQL بمعاملات متراجعة ومتصفح بمضيف معزول.
- دليل السلوك والتحقق: docs/EventArrivalAlerts.md.

فحص browser-event-alerts-check.mjs يشمل Console errors بالإضافة إلى pageerror، لمنع رجوع خطأ SignalR الناتج عن قيمة معالج الإشعار (2026-11-06).

تحديث شاشة EgyptEBS الحي: EgyptEbsController.Status/Signals، IEgyptEbsReader.Status، Views/EgyptEbs/_Signals.cshtml وwwwroot/js/egypt-ebs.js. حالة كل 15 ثانية وقائمة عند تقدم الفحص؛ scripts/browser-egypt-ebs-check.mjs للتحقق الحي للقراءة فقط.

## إعداد عنوان خادم EgyptEBS

Application/Contracts/EgyptEbsServer.cs: عقد الإدخال والإعدادات؛ Infrastructure/Services/EgyptEbsServerSettings.cs: مخزن عنوان محلي ومصدر اتصال ديناميكي واختبار قراءة؛ Web/Controllers/EgyptEbsServerController.cs وViews/EgyptEbsServer/Index.cshtml: الشاشة. روابط من Admin/Settings وEgyptEbs. EgyptEbsReader يستخدم IEgyptEbsConnectionSource في القراءة اليدوية والتلقائية. الاختبارات: EgyptEbsServerTests وscripts/browser-egypt-ebs-server-check.mjs. دليل التشغيل: docs/EgyptEbsServerSettings.md.

EgyptEbsServerSettings وEgyptEbsConnectionSource يدعمان الآن ProtectedCredentials المشفرة بـData Protection؛ EgyptEbsConnectionProbe.Failure يصنف أخطاء SQL دون عرض النص الخام. Password يحذف من النموذج بعد POST. اختبارات EgyptEbsServerTests تشمل الحساب البديل والتشفير واستعادة المفاتيح.

التشغيل المحلي: Start-PHEOC.cmd يستدعي scripts/Start-Development.ps1 بنافذة مستمرة. السكربت يعرض البناء ويتحقق من النسخة الموجودة والمنفذ ثم يشغل التطبيق على 5268؛ Program.cs يعرض سجل بدء الاستماع وlaunchSettings متوافق مع العنوان.

EgyptEbsConnectionProbe: مهلة اتصال الاختبار 20 ثانية وسقف 30 ثانية، ورسائل مخصصة لرفض الشبكة 5/10013 ولمهلة الفتح/القراءة -2. اختبارات الإعدادات 20 حالة.

الأدوار الافتراضية: Application/Security/RolePermissionDefaults.cs؛ Web/Views/Admin/Users.cshtml وwwwroot/js/user-permissions.js للاختيار والتعديل والاستعادة. Infrastructure/Services/IdentityServices.cs يحفظ الاختيار الصريح للحسابات الجديدة، وWeb/Security/UserClaimsFactory.cs يطبقه دون وراثة صلاحيات إضافية. RolePermissionTests وscripts/browser-role-permissions-check.mjs للتحقق. دليل القوائم: docs/RolePermissionDefaults.md.

تمييز استقبال EgyptEBS: EgyptEbsSignal.ReceivedAtUtc من Inbox المحلي عبر EgyptEbsReader؛ Views/EgyptEbs/_Signals.cshtml تحمل معرف الإشارة وحالة الاستقبال ووقته، وwwwroot/js/egypt-ebs.js يتابع الانتقالات والرسائل ويحتفظ بمدة التلاشي خلال استبدال الجدول. CSS محصور في #ebs-signals داخل site.css. سكربت browser-egypt-ebs-highlight-check.mjs يراجع مدة الخمس ثواني والتدرج والحركة ومنع التكرار والهواتف وتقليل الحركة.

بحث وصفحات EgyptEBS: EgyptEbsFilter وEgyptEbsBoard في Application/Contracts/EgyptEbs.cs، والقراءة في Infrastructure/Services/EgyptEbsBrowse.cs (جزء من EgyptEbsReader)، وواجهة الفلاتر Views/EgyptEbs/_Filters.cshtml وترقيم الصفحات في _Signals.cshtml. Index وSignals يمرران الفلاتر إلى Browse؛ JavaScript يحتفظ بمعاملات URL عند التحديث. EgyptEbsBrowseTests وAutomaticReceptionTests وscripts/browser-egypt-ebs-browse-check.mjs للتحقق. دليل السلوك: docs/EgyptEbsBrowsing.md.

طريقة عرض EgyptEBS: EgyptEbsFilter.Display بقيم table/cards؛ Views/EgyptEbs/_Signals.cshtml يرسم التخطيط المختار و_SignalAction.cshtml يوحّد إجراء الاستقبال. ViewSupport/EgyptEbsDisplay.cs يحسب أمس/اليوم بتوقيت مصر عند غياب طلب تاريخ صريح؛ المتحكم يطبقه على الصفحة والجزء فقط. URL helpers تحتفظ بالتاريخ الفارغ، وegypt-ebs.js يثبت تواريخ فتح الصفحة أثناء التحديث. الاختبارات: EgyptEbsDisplayTests وAutomaticReceptionTests وscripts/browser-egypt-ebs-display-check.mjs، وفحص highlight يقبل PHEOC_EBS_HIGHLIGHT_VIEW=cards.

السجل الزمني المرئي: Views/Events/_Timeline.cshtml بدلاً من _Activity في قسم #timeline من Details؛ ViewSupport/TimelinePresentation.cs للتسميات والألوان والأيقونات وتحويل توقيت العرض، وViews/Shared/_TimelineIcon.cshtml لأيقونات SVG المحلية، وتنسيق event-timeline في site.css. اختبار EventAlertBrowserTests.TimelineLayoutEgyptDatesEncodingAndMobileRenderTogether يرسم الجزء ببيانات معزولة عبر سكربت browser-event-timeline-check.mjs؛ فحص حي للقراءة فقط عبر browser-event-timeline-live-check.mjs. لا تغيير للخدمة أو بيانات TimelineEntry.

## QIRA

سجل الإصدارات الملون وحذف المسودة (2026-11-07): Views/Qira/Event.cshtml يعرض شارات وإطارات لمراحل الإعداد/التقييم/انتظار المراجعة/انتظار الاعتماد/المعتمد، عبر QiraText.StageClass/StageLabel وcss/qira.css. Views/Qira/Delete.cshtml وQiraController.Delete/ConfirmDelete وIQiraService.DeleteQiraDraft تنفذ تأكيداً ثم حذفاً إدارياً للمسودة الخاصة بالمُعد فقط، مع صلاحية الإنشاء ونطاق الحدث وRowVersion. QiraState.Deleted=5 يخزن في عمود State القائم؛ لا ترقية مخطط. تبقى المسودة والأجوبة وQiraHistories وEventTimeline، وتستبعد من السجل والملخص والتحميل المباشر، مع استمرار ترقيم الإصدارات دون إعادة استخدام. OmsDbContext يمنع حذف السجل الفعلي أو تغيير مسودة محذوفة أو حذف تقييم مكتمل. اختبارات QiraSqlTests وQiraBrowserTests وسكربتا browser-qira-check وbrowser-qira-live-check تغطي الحذف والمراحل.

- Domain/Qira/QiraAlgorithm.cs: شجرة WHO دون أوزان؛ QiraRecords.cs: كيانات التعريف والتقييم والإجابة والإجراءات والتاريخ.
- Application/Contracts/Qira.cs وIQiraService، Infrastructure/Services/QiraService.cs، Web/Controllers/QiraController.cs وViews/Qira وViewSupport/QiraText.cs وwwwroot/css/qira.css وjs/qira-settings.js.
- تفاصيل الحدث تستخدم Shared/_QiraSummary؛ الإعدادات تربط Qira/Configuration. الطريقة القديمة للقراءة التاريخية فقط.
- database/003-Qira.sql وscripts/Update-QiraDatabase.ps1: ترقية إضافية صريحة. المنهجية والتشغيل: [QiraMethodology.md](QiraMethodology.md)، الترخيص: [QiraSourceNotice.md](QiraSourceNotice.md). الاختبارات: QiraAlgorithmTests وQiraSqlTests وQiraBrowserTests وscripts/browser-qira-check.mjs.

واجهة QIRA المبسطة: Views/Qira/Details.cshtml تجمع _Scope و_Result و_Actions و_Evidence؛ wwwroot/js/qira-steps.js يدعم تصحيح الخطر والاختيار الصريح بلوحة المفاتيح. الأزرار ترسل Qira/Answer مباشرة وتستخدم نفس الخدمة والفروع. scripts/browser-qira-check.mjs يغطي التتابع والشاشة المبسطة والرجوع والاختصارات؛ لا تغيير للمخطط.

شجرة QIRA: Views/Qira/_Tree.cshtml وViewSupport/QiraTree.cs وwwwroot/js/qira-tree.js وcss/qira-tree.css. HTML للبطاقات والنماذج وSVG للأسهم؛ جميع الوصلات مشتقة من Definition.Nodes المحفوظة. QiraDetail.ActionTemplates من مرجع التقييم المثبت تعرض الإجراءات قبل الإكمال. يدعم renderer الاتجاهين والمسار والتكبير دون مكتبة أو CDN. اختبار browser-qira-check يراجع الرسم وسير العمل على SQL بمعاملة متراجعة.

الرجوع في شجرة QIRA: _Tree.cshtml يعرض الخطوة السابقة وروابط تعديل البطاقات ونموذج Qira/Undo. IQiraService.UndoQiraAnswer وOmsService في QiraService.cs يلغيان آخر إجابة نشطة مع سجل تدقيق وRowVersion، دون حذف أو تعديل تقييم مكتمل. التحقق في QiraSqlTests وbrowser-qira-check.mjs.

تعديل صلاحيات حساب موجود: Application/Contracts/UserPermissions.cs لعقود التعديل، Infrastructure/Services/IdentityServices.cs لخدمتي EditPermissions/UpdatePermissions، Web/Controllers/AdminController.cs وViews/Admin/EditPermissions.cshtml وwwwroot/js/user-permissions.js؛ تنسيق الهاتف في site.css. الاختبارات RolePermissionTests وUserPermissionsBrowserTests وscripts/browser-edit-user-permissions-check.mjs تستخدم حسابات ضمن معاملات متراجعة وتراجع cookies الحقيقية. دليل السلوك وحدود التحقق في [RolePermissionDefaults.md](RolePermissionDefaults.md).

حذف المستخدم الإداري: Application/Security/UserAccountStatus.cs لعلامة الحذف في claims، UserAccountDeleteInput في Contracts/UserPermissions.cs، وUserAdministration.Delete في IdentityServices.cs. Views/Admin/DeleteUser.cshtml صفحة التأكيد وAdminController.DeleteUser/ConfirmDeleteUser مسارا العرض والحفظ. قائمة Users تستبعد الحسابات الموسومة، ويُرفض تعديلها أو تفعيلها. لا حذف لسجلات Identity أو التدقيق ولا تغيير مخطط. الفحص الحي للقراءة فقط scripts/browser-user-management-live-check.mjs؛ اختبارات SQL والمتصفح المرتبطة ضمن RolePermissionTests وUserPermissionsBrowserTests.
