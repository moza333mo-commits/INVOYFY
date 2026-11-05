// File: Program.cs
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvoifyWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // إخفاء ترويسة Server: Kestrel لمنع كشف معلومات الخادم
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        // إعداد ضغط الاستجابات باستخدام Brotli و Gzip مع دعم ملفات SVG والبيانات
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[]
                {
                    "image/svg+xml",
                    "application/manifest+json",
                    "font/woff2"
                });
        });

        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        var app = builder.Build();

        var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

        // مزامنة ملفات التنسيقات والسكريبت المتجاوب وتبديل اللغة والوضع الليلي عند بدء التشغيل
        EnsureResponsiveWebAssets(app.Environment.ContentRootPath, webRootPath);

        // 1. وسيط ترويسات الأمان في بداية خط الأنابيب لضمان إرفاقها مع جميع الاستجابات بما فيها صفحات الخطأ
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com data:; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self' https://fonts.googleapis.com https://fonts.gstatic.com; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';";

                return System.Threading.Tasks.Task.CompletedTask;
            });

            await next();
        });

        // 2. إعدادات الحماية ومعالجة الأخطاء في بيئة الإنتاج
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/html; charset=utf-8";

                    var errorPagePath = Path.Combine(webRootPath, "404.html");
                    if (File.Exists(errorPagePath))
                    {
                        await context.Response.SendFileAsync(errorPagePath);
                    }
                    else
                    {
                        await context.Response.WriteAsync(
                            "<!DOCTYPE html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>خطأ في الخادم</title></head>" +
                            "<body style='font-family:sans-serif;text-align:center;padding:3rem;'><h1>حدث خطأ غير متوقع</h1><p>يرجى المحاولة مرة أخرى لاحقاً.</p></body></html>");
                    }
                });
            });

            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseResponseCompression();

        // 3. معالجة الصفحات غير الموجودة (404) مع ضمان تحويل الطلبات غير القياسية إلى GET قبل إعادة التنفيذ عبر StaticFileMiddleware
        app.UseStatusCodePagesWithReExecute("/404.html");

        app.Use(async (context, next) =>
        {
            if (context.Features.Get<IStatusCodeReExecuteFeature>() != null &&
                !HttpMethods.IsGet(context.Request.Method) &&
                !HttpMethods.IsHead(context.Request.Method))
            {
                context.Request.Method = HttpMethods.Get;
            }

            await next();

            if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
            {
                if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                {
                    context.Request.Method = HttpMethods.Get;
                }
            }
        });

        // تقديم wwwroot/index.html كصفحة افتراضية أساسية للموقع
        app.UseDefaultFiles();

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".svg"] = "image/svg+xml";
        contentTypeProvider.Mappings[".webp"] = "image/webp";
        contentTypeProvider.Mappings[".woff2"] = "font/woff2";

        var cachedAssetExtensions = new[]
        {
            ".css", ".js", ".svg", ".png", ".webp",
            ".jpg", ".jpeg", ".gif", ".ico", ".avif",
            ".woff", ".woff2", ".ttf"
        };

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider,
            OnPrepareResponse = ctx =>
            {
                var fileName = ctx.File.Name;

                if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith("invoify-mobile.js", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith("app.js", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith("style.css", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
                else if (!app.Environment.IsDevelopment() &&
                         cachedAssetExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=86400";
                }
            }
        });

        // فتح المتصفح تلقائياً في بيئة التطوير فقط مع التحقق الأمني من صحة الرابط
        if (app.Environment.IsDevelopment())
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5050";
                    if (Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) &&
                        (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = parsedUri.ToString(),
                            UseShellExecute = true
                        });
                    }
                }
                catch
                {
                    // تجاهل أخطاء فتح المتصفح في بيئات التشغيل غير الرسومية
                }
            });
        }

        app.Run();
    }

    private static void EnsureResponsiveWebAssets(string contentRootPath, string webRootPath)
    {
        try
        {
            if (!Directory.Exists(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
            }

            var cssDir = Path.Combine(webRootPath, "css");
            Directory.CreateDirectory(cssDir);

            // تنظيف الملفات المكررة ذات الامتداد الخاطئ (.cs) بعد التأكد من وجود ملفات .css الصحيحة
            var styleCsPath = Path.Combine(cssDir, "style.cs");
            var styleCssPath = Path.Combine(cssDir, "style.css");
            if (File.Exists(styleCsPath))
            {
                if (!File.Exists(styleCssPath))
                {
                    var cssContent = File.ReadAllText(styleCsPath, Encoding.UTF8);
                    if (cssContent.StartsWith("// File:", StringComparison.OrdinalIgnoreCase))
                    {
                        var firstNewLine = cssContent.IndexOf('\n');
                        if (firstNewLine > 0)
                        {
                            cssContent = cssContent.Substring(firstNewLine + 1);
                        }
                    }
                    File.WriteAllText(styleCssPath, cssContent, new UTF8Encoding(false));
                }
                File.Delete(styleCsPath);
            }

            var inputCsPath = Path.Combine(cssDir, "input.cs");
            var inputCssPath = Path.Combine(cssDir, "input.css");
            if (File.Exists(inputCsPath))
            {
                if (!File.Exists(inputCssPath))
                {
                    var inputContent = File.ReadAllText(inputCsPath, Encoding.UTF8);
                    if (inputContent.StartsWith("// File:", StringComparison.OrdinalIgnoreCase))
                    {
                        var firstNewLine = inputContent.IndexOf('\n');
                        if (firstNewLine > 0)
                        {
                            inputContent = inputContent.Substring(firstNewLine + 1);
                        }
                    }
                    File.WriteAllText(inputCssPath, inputContent, new UTF8Encoding(false));
                }
                File.Delete(inputCsPath);
            }

            var tailwindConfigRaw = Path.Combine(contentRootPath, "tailwind.config");
            var tailwindConfigJs = Path.Combine(contentRootPath, "tailwind.config.js");
            if (File.Exists(tailwindConfigRaw))
            {
                if (!File.Exists(tailwindConfigJs))
                {
                    var twContent = File.ReadAllText(tailwindConfigRaw, Encoding.UTF8);
                    if (twContent.StartsWith("<!--", StringComparison.OrdinalIgnoreCase))
                    {
                        var firstNewLine = twContent.IndexOf('\n');
                        if (firstNewLine > 0)
                        {
                            twContent = twContent.Substring(firstNewLine + 1);
                        }
                    }
                    File.WriteAllText(tailwindConfigJs, twContent, new UTF8Encoding(false));
                }
                File.Delete(tailwindConfigRaw);
            }

            var jsDir = Path.Combine(webRootPath, "js");
            Directory.CreateDirectory(jsDir);
            var mobileJsPath = Path.Combine(jsDir, "invoify-mobile.js");

            var mobileJsCode = @"// File: wwwroot/js/invoify-mobile.js
(function () {
  'use strict';

  var currentLang = 'ar';
  try {
    var savedLang = localStorage.getItem('invoify_lang');
    if (savedLang === 'en' || savedLang === 'ar') {
      currentLang = savedLang;
    }
  } catch (e) {}

  // حفظ النصوص العربية الأصلية في الذاكرة دون تعديل خصائص DOM لمنع أي تعارض مع Tailwind CDN
  var origTextMap = typeof WeakMap !== 'undefined' ? new WeakMap() : null;
  var origAttrMap = typeof WeakMap !== 'undefined' ? new WeakMap() : null;
  var origDocTitle = null;

  var trialRegexWithDuration = /لمدة\s*(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?/g;
  var trialRegexGeneral = /(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?/g;
  var trialRegexMonth = /لمدة\s*شهر\s*كامل/g;

  // قاموس العبارات والمصطلحات المحاسبية والتسويقية الدقيق مرتب من الأطول إلى الأقصر
  var exactTranslations = [
    ['تطبيق مكتبي احترافي يعمل على نظام Windows بدون الحاجة لاتصال بالإنترنت. يدعم الوحدات المترابطة هرمياً (كغ ← طرد ← صندوق)، تعدد العملات، الفواتير النقدية والآجلة، تعدد المعامل بقواعد بيانات مستقلة، وطباعة A4 بختم رسمي مفرغ ورمز QR.', 'A professional Windows desktop application that runs completely offline. Supports hierarchical units (kg → parcel → box), multi-currency accounting, cash & credit invoices, multi-workspace independent databases, and dual A4 printing with transparent official stamp & QR code.'],
    ['جميع الفواتير والمنتجات والعملاء الذين قمت بإدخالهم خلال فترة الـ 20 فاتورة فقط تبقى محفوظة بالكامل على جهازك. بمجرد شراء وإدخال مفتاح التفعيل، يفتح البرنامج فوراً وتتابع عملك من حيث توقفت دون ضياع أي معلومة.', 'All invoices, products, and customers you entered during the 20-invoice trial remain completely saved on your PC. Once you purchase and enter your activation key, the software unlocks immediately and you continue right where you left off without losing any data.'],
    ['جرّب كافة ميزات البرنامج بدون أي قيود لغاية 20 فاتورة مجانية. وعند انتهاء الفترة التجريبية تبقى جميع فواتيرك وبياناتك محفوظة وتستكمل عملك فور إدخال مفتاح التفعيل.', 'Experience all software features without restrictions for up to 20 free invoices. When the trial finishes, all your invoices and data remain safely preserved and you continue seamlessly once you enter your activation key.'],
    ['لا، يعمل برنامج Invoify محلياً بنسبة 100% على جهاز الكمبيوتر الخاص بك دون الحاجة لأي اتصال بالإنترنت، وتُحفظ قاعدة البيانات والنسخ الاحتياطية على جهازك بأمان تام.', 'No, Invoify runs 100% locally on your PC without requiring any internet connection, and your database and backups are stored safely on your machine.'],
    ['يعمل البرنامج على أي جهاز كمبيوتر أو لابتوب بنظام Windows 10 أو Windows 11 (64-bit)، ولا يتطلب مواصفات عالية، ويدعم جميع الطابعات العادية وطابعات الليزر A4/A5.', 'The software runs smoothly on any desktop or laptop with Windows 10 or Windows 11 (64-bit), requires no high-end hardware, and supports all standard and laser A4/A5 printers.'],
    ['جرّب كافة ميزات البرنامج بدون أي قيود لمدة شهر كامل. وعند انتهاء الفترة التجريبية تبقى جميع فواتيرك وبياناتك محفوظة وتستكمل عملك فور إدخال مفتاح التفعيل.', 'Experience all software features without restrictions for up to 20 free invoices. When the trial finishes, all your invoices and data remain safely preserved and you continue seamlessly once you enter your activation key.'],
    ['صُمم قالب الطباعة بدقة متجهية (1200 DPI Vector) ليطبع نسخة العميل على اليمين ونسخة الإدارة على اليسار مع خط قص أوسط، مما يوفر 50% من استهلاك الورق.', 'Engineered at 1200 DPI vector precision to print the Customer Copy on one side and the Admin Copy on the other with a center cut line, saving 50% of paper consumption.'],
    ['فصل محاسبي دقيق بين الفواتير النقدية والآجلة (الذمم)، سندات قبض، مرتجعات، حسم مكتسب، رصيد مدوّر (BF-000)، وتنبيه تلقائي عند تجاوز الحد الائتماني.', 'Strict accounting separation between cash and credit invoices, receipt vouchers, returns, earned discounts, brought-forward balance (BF-000), and automatic credit-limit alerts.'],
    ['عرّف وحدات الصنف (كغ ← طرد ← صندوق ← طبلية) مرة واحدة، وبع بالصندوق مع إمكانية التسعير بالكيلو أو بالطرد أو بالصندوق داخل الفاتورة بشكل مستقل.', 'Define item units (kg → parcel → box → pallet) once, sell by box, and price independently by kg, parcel, or box directly inside the invoice.'],
    ['طباعة نسختين جنباً إلى جنب (نسخة العميل + نسخة الإدارة) مع خيار إخفاء الأسعار عن نسخة العميل، رمز QR، وتفريغ تلقائي لخلفية الختم البيضاء.', 'Print two copies side by side (Customer Copy + Admin Copy) with optional price hiding on the customer copy, smart QR code, and automatic white-background removal for stamps.'],
    ['أنشئ عدة حسابات (يوزرنيم) على نفس الكمبيوتر، ولكل حساب قاعدة بيانات مستقلة تماماً (أصناف وفواتير وعملاء منفصلون) مع تبديل فوري بضغطة زر.', 'Create multiple workspaces (usernames) on the same PC, each with a completely isolated database (separate items, invoices, and customers) and instant one-click switching.'],
    ['حماية كلمات السر بتشفير PBKDF2-SHA256، تحديد صلاحيات الموظف (منع الحذف، منع الخصم، منع الإعدادات)، وسجل تدقيق (Audit Log) يوثق كل عملية.', 'PBKDF2-SHA256 password encryption, granular staff permissions (restrict deletion, discounts, or settings), and a comprehensive Audit Log recording every action.'],
    ['دعم الدولار، الليرة السورية، الليرة التركية، اليورو وغيرها مع اتجاهي تسعير، محول عملات فوري مدمج، وأرصدة عملاء مفصولة لكل عملة دون خلط.', 'Supports USD, SYP, TRY, EUR, and more with bidirectional rate modes, built-in instant currency converter, and isolated customer balances per currency.'],
    ['إشعارات فورية بالفواتير الآجلة المستحقة والأصناف التي قارب مخزونها على النفاد والمسودات المعلقة، مع تصدير جميع الجداول إلى Excel / CSV.', 'Instant alerts for due credit invoices, low-stock items, and pending drafts, plus one-click export of all tables to Excel / CSV.'],
    ['ارفع صورة ختم شركتك حتى لو كانت بخلفيّة بيضاء، وسيقوم النظام بتحويل الخلفية إلى شفافة تلقائياً مع التحكم بالحجم والميلان والشفافية.', 'Upload your company stamp image even with a white background; the system automatically makes it transparent with size, rotation, and opacity controls.'],
    ['نسخ احتياطي تلقائي عند الإغلاق، سياسة النسخة الواحدة المتجددة أو آخر N نسخ، مع فحص سلامة SQLite (Integrity Check) قبل أي استعادة.', 'Automatic backup on exit, rolling single-backup or last-N-backups retention policy, with full SQLite Integrity Check verification before any restore.'],
    ['حفظ وطباعة فورية، أو حفظ كمسودة لاستكمالها لاحقاً، أو معاينة الطباعة وتصدير PDF وصورة PNG عالية الدقة لمشاركتها عبر واتساب.', 'Instant save & print, save as draft to resume later, or open print preview to export high-resolution PDF and PNG images for WhatsApp sharing.'],
    ['بحث فوري عن العميل أو إضافة عميل جديد، اختيار عملة الفاتورة وسعر الصرف، وتحديد نوع الدفع (نقدي أو آجل مع تاريخ استحقاق).', 'Instant customer search or quick-add, invoice currency & exchange rate selection, and payment type (Cash or Credit with due date).'],
    ['خيار إخفاء الأسعار ورمز QR عن نسخة العميل فقط (لإرسالها كإشعار استلام مستودعي) مع بقاء الأسعار كاملة في نسخة الإدارة.', 'Option to hide prices and QR code on the Customer Copy only (to use as a warehouse delivery note) while keeping full prices on the Admin Copy.'],
    ['اختر البيع بـ ""صندوق"" وحدد السعر لكل ""كغ"" أو لكل ""طرد"" أو لكل ""صندوق"" بضغطة واحدة من البطاقات المصغرة تحت حقل السعر.', 'Choose to sell by ""Box"" while setting the unit price per ""kg"", per ""Parcel"", or per ""Box"" with a single click using the quick-select chips below the price field.'],
    ['يمنع النظام حذف أي عملة مرتبطة بفواتير أو حركات عملاء سابقة، ويعرض خيار تعطيلها للحفاظ على دقة التقارير التاريخية.', 'Prevents deleting any currency linked to existing invoices or customer transactions, offering safe deactivation to preserve historical reports.'],
    ['عدّل رقم الفاتورة القادم في أي وقت أو صفّر سجل الفواتير لبدء سنة مالية جديدة مع أخذ نسخة احتياطية وقائية تلقائياً.', 'Adjust the next invoice number anytime or reset the invoice log for a new fiscal year with an automatic safety backup.'],
    ['زر فحص سلامة قاعدة البيانات يشغّل 35 اختباراً محاسبياً وأمنياً آلياً، مع أداة ضغط القاعدة (VACUUM) وإصلاح السجلات.', 'One-click database health check runs 35 automated accounting and security tests, alongside SQLite VACUUM compression and record repair.'],
    ['6 تبويبات متكاملة لكل عميل: معلومات الاتصال، الفواتير، الدفعات، كشف الحساب، المنتجات المسحوبة، والحد الائتماني.', '6 integrated tabs per customer: Contact Info, Invoices, Payments, Account Statement, Purchased Products, and Credit Limit.'],
    ['يدعم النظام اتجاه (1 USD = 14,000 SYP) أو اتجاه (1 EUR = 1.10 USD) لتسعير أي عملة محلية أو أجنبية بسهولة تامة.', 'Supports both (1 USD = 14,000 SYP) and (1 EUR = 1.10 USD) directions to price any local or foreign currency effortlessly.'],
    ['قد يكون الرابط غير صحيح أو تم نقل الصفحة. يمكنك العودة إلى الصفحة الرئيسية لمتابعة استعراض برنامج Invoify.', 'The link may be mistyped or the page may have moved. You can return to the home page to continue exploring Invoify.'],
    ['النظام المتكامل لإصدار الفواتير، إدارة المستودع بالوحدات الهرمية، وكشوف حسابات العملاء متعددة العملات.', 'The complete system for issuing invoices, managing warehouse stock with hierarchical units, and multi-currency customer ledgers.'],
    ['لتفعيل البرنامج مدى الحياة بعد انتهاء فترة الـ 20 فاتورة فقط التجريبية، اتبع الخطوات الثلاث البسيطة:', 'To activate the software for life after the 20-invoice trial period, follow these 3 simple steps:'],
    ['بناء شجرة وحدات المنتج تلقائياً مع تحديد الوحدة الافتراضية للبيع ومتابعة المخزون بالكيلوغرام.', 'Automatically build each product unit tree, set the default selling unit, and track inventory accurately in kilograms.'],
    ['تعديل مباشر لسعر الصرف مع حفظ التاريخ والوقت والمستخدم، وعدم تأثر الفواتير التاريخية السابقة.', 'Inline exchange rate updates with timestamp and user logging, without altering historical invoices.'],
    ['6 أقسام منظمة: هوية المؤسسة، الطباعة والختم، المالية، النسخ الاحتياطي، الصيانة، والمستخدمون.', '6 organized sections: Company Identity, Printing & Stamp, Financials, Backup, Database Maintenance, and Users.'],
    ['، سيطلب منك البرنامج في المرة الأولى فقط اختيار اسم مستخدم وكلمة سر لحساب المدير الخاص بك.', ', the program will ask you on first launch only to choose a username and password for your administrator account.'],
    ['رمز QR ذكي يتضمن بيانات الشركة، رقم الفاتورة، العميل، الأصناف، الأوزان، والصافي المطلوب.', 'Smart QR code containing company info, invoice number, customer, items, weights, and net amount due.'],
    ['إدخال سريع بالكيبورد (Enter)، تعديل مباشر داخل الجدول، وفصل وحدة البيع عن وحدة التسعير.', 'Rapid keyboard entry (Enter), inline grid editing, and independent selling unit vs. pricing unit selection.'],
    ['حساب تكاليف المواد الأولية وخلطات الإنتاج والربط المباشر مع مستودع البضاعة الجاهزة.', 'Calculate raw material costs, production formulas, and sync directly with finished goods inventory.'],
    ['تم تصميم كل نافذة في Invoify لتختصر وقت المحاسب وتمنع الأخطاء الحسابية والمخزنية.', 'Every screen in Invoify is engineered to save the accountant time and eliminate calculation and inventory errors.'],
    ['ماذا يحدث لبياناتي وفواتيري بعد انتهاء فترة التجربة المجانية (20 فاتورة فقط)؟', 'What happens to my data and invoices after the free trial (20 invoices only) ends?'],
    ['تصميم وطباعة لصاقات المنتجات الغذائية والباركود وتواريخ الصلاحية بدقة عالية.', 'Design and print food product labels, barcodes, and expiration dates with high precision.'],
    ['بنية الموقع مجهزة لإضافة وإطلاق إصدارات وبرامج مكتبية جديدة بجانب Invoify.', 'Our platform is built to deliver and expand desktop business solutions alongside Invoify.'],
    ['جميع البيانات التي تدخلها خلال الـ 20 فاتورة فقط تبقى محفوظة عند التفعيل.', 'All data entered during your 20-invoice trial remains intact upon activation.'],
    ['طبقة الختم الرسمي المفرغ الشفاف فوق جدول الأصناف دون تغطية الأرقام.', 'Transparent official stamp layer rendered cleanly over the items table without obscuring numbers.'],
    ['جميع الميزات مفتوحة بالكامل (فواتير، منتجات، عملاء، عملات، طباعة).', 'All features unlocked (invoices, products, customers, currencies, printing).'],
    ['دعم فني مباشر ومساعدة في إعداد شعار وختم المؤسسة وتصميم الفاتورة.', 'Direct technical support and assistance in setting up your company logo, stamp, and invoice layout.'],
    ['انقر على أي قسم بالأسفل لمعاينة تصميم الواجهة الفعلية ومكوناتها.', 'Click any tab below to preview the actual interface layout and components.'],
    ['عدد غير محدود من الفواتير، الأصناف، العملاء، والمعامل المستقلة.', 'Unlimited invoices, items, customers, and independent workspaces.'],
    ['جميع الحقوق محفوظة © 2026 — صُمم بعناية لقطاع الأعمال والتجارة.', 'All rights reserved © 2026 — Crafted with care for business and commerce.'],
    ['مفتاح تفعيل رقمي رسمي مرتبط بجهازك يفتح البرنامج مدى الحياة.', 'Official digital activation key bound to your PC that unlocks the software for life.'],
    ['ℹ️ الوحدة: صندوق (10 طرد × 5 كغ = 50 كغ) | الإجمالي: 150 كغ', 'ℹ️ Unit: Box (10 Parcels × 5 kg = 50 kg) | Total: 150 kg'],
    ['إجابات واضحة حول تشغيل البرنامج والتجربة المجانية والتفعيل.', 'Clear answers about running the software, the free trial, and lifetime activation.'],
    ['أدخل معرّف الجهاز (اختياري لتجهيز رسالة الواتساب تلقائياً):', 'Enter Machine ID (optional, to pre-fill the WhatsApp message):'],
    ['أدِر فواتيرك، مخزونك، وكشوف حسابات عملائك بدقة محاسبية عبر', 'Manage your invoices, inventory, and customer statements with accounting precision via'],
    ['Invoify — نظام إدارة فواتير المبيعات والمستودعات والعملاء', 'Invoify — Sales Invoicing, Inventory & Customer Management ERP'],
    ['— يعمل بدون إنترنت بسرعة فائقة مع نسخ احتياطي ذري تلقائي.', '— Works 100% offline at blazing speed with automatic atomic backup.'],
    ['إدارة المنتجات والوحدات المترابطة هرمياً (ProductsView)', 'Products & Hierarchical Units Management (ProductsView)'],
    ['مصمم للمعامل وتجار الجملة والمفرق والشركات التجارية', 'Built for Factories, Wholesalers, Retailers & Trading Companies'],
    ['مع دعم الرصيد المدوّر (BF-000) عند الفلترة بالتاريخ', 'Supports Brought-Forward Balance (BF-000) when filtering by date'],
    ['رابط تحميل مباشر وآمن • يعمل فوراً على نظام Windows', 'Direct & secure download link • Runs immediately on Windows'],
    ['إدارة العملات وسجل تغييرات الصرف (CurrenciesView)', 'Currency Management & Exchange Rate Log (CurrenciesView)'],
    ['الإعدادات المتقدمة والختم والصيانة (SettingsView)', 'Advanced Settings, Official Stamp & Maintenance (SettingsView)'],
    ['نظام إدارة فواتير المبيعات والمستودعات والعملاء', 'Sales Invoicing, Inventory & Customer Management ERP'],
    ['الفواتير النقدية مسددة فوراً ولا تزيد المديونية', 'Cash invoices are settled immediately and do not increase debt'],
    ['ملف العميل الشامل وكشف الحساب (CustomersView)', 'Comprehensive Customer Profile & Ledger (CustomersView)'],
    ['طباعة نسختين (عميل + إدارة) في ورقة A4 واحدة', 'Print Two Copies (Customer + Admin) on a Single A4 Sheet'],
    ['تواصل مباشر لاستلام مفتاح التفعيل خلال دقائق', 'Direct contact to receive your activation key within minutes'],
    ['هل يحتاج البرنامج إلى اتصال بالإنترنت ليعمل؟', 'Does the software require an internet connection to run?'],
    ['بدء التحميل المباشر الآن (Invoify_Setup.zip)', 'Start Direct Download Now (Invoify_Setup.zip)'],
    ['ما هي متطلبات تشغيل البرنامج على الكمبيوتر؟', 'What are the PC system requirements to run the software?'],
    ['كل ما يحتاجه معملك أو متجرك في برنامج واحد', 'Everything Your Factory or Store Needs in One Software'],
    ['شاشة إصدار الفواتير السريعة (InvoicesView)', 'Fast Invoice Issuance Screen (InvoicesView)'],
    ['حمّل النسخة التجريبية مجاناً 20 فاتورة فقط', 'Download the Free Trial (20 Invoices Only)'],
    ['إدارة فواتيرك ومبيعاتك بكل سهولة واحترافية', 'Manage your invoices and sales with ease and precision'],
    ['1 كغ ⬅️ 1 طرد (10 كغ) ⬅️ 1 صندوق (100 كغ)', '1 kg ⬅️ 1 Parcel (10 kg) ⬅️ 1 Box (100 kg)'],
    ['⬇️ تحميل النسخة التجريبية (20 فاتورة فقط)', '⬇️ Download Free Trial (20 Invoices Only)'],
    ['النسخة التجريبية الكاملة (20 فاتورة فقط)', 'Full Trial Version (20 Invoices Only)'],
    ['عذراً، الصفحة التي تبحث عنها غير موجودة', 'Sorry, the page you are looking for was not found'],
    ['نظام إدارة فواتير المبيعات والمستودعات', 'Sales Invoicing & Inventory Management System'],
    ['لا يتطلب بطاقة ائتمان أو اشتراك مسبق.', 'No credit card or prior subscription required.'],
    ['تحميل برنامج Invoify للكمبيوتر (v2.6)', 'Download Invoify for Windows PC (v2.6)'],
    ['Invoify - نظام إدارة فواتير المبيعات', 'Invoify - Sales Invoice Management System'],
    ['⚙️ الإعدادات والختم والنسخ الاحتياطي', '⚙️ Settings, Stamp & Backup'],
    ['سندات قبض + مرتجع مبيعات + حسم مكتسب', 'Receipt Vouchers + Sales Returns + Earned Discount'],
    ['إصدار 2026 متوفر الآن لنظام Windows', '2026 Edition Available Now for Windows'],
    ['نسخ احتياطي ذري وفحص سلامة البيانات', 'Atomic Backup & Database Integrity Check'],
    ['تعرّف على شاشات Invoify قبل التحميل', 'Explore Invoify Screens Before Downloading'],
    ['نظام تصميم وطباعة اللصاقات التجارية', 'Commercial Label Design & Printing System'],
    ['خالٍ من الفيروسات ويعمل بدون إنترنت', 'Virus-free & works 100% offline'],
    ['إجمالي الدفعات والمرتجعات (الدائن)', 'Total Payments & Returns (Credit)'],
    ['Invoify — إدارة الفواتير والمبيعات', 'Invoify — Sales & Invoice Management'],
    ['مفتاح التفعيل الرسمي (License Key)', 'License Key'],
    ['تحميل مجاني (تجربة 20 فاتورة فقط)', 'Free Download (20-Invoice Trial)'],
    ['تم تجهيز رسالة طلب الترخيص بنجاح.', 'License request message prepared successfully.'],
    ['2. محرك الوحدات والتسعير المستقل', '2. Independent Unit & Pricing Engine'],
    ['/ 20 فاتورة فقط من تاريخ التثبيت', '/ 20 free invoices from installation'],
    ['تفعيل فوري بمفتاح الرقم التسلسلي', 'Instant activation via serial key'],
    ['وحدات مترابطة هرمياً وتسعير مرن', 'Hierarchical Linked Units & Flexible Pricing'],
    ['تعدد المعامل والحسابات المستقلة', 'Multi-Workspace & Independent Databases'],
    ['حفظ ذري + فحص المخزون والائتمان', 'Atomic Save + Stock & Credit Check'],
    ['طباعة كشف حساب A4 + تصدير Excel', 'A4 Statement Print + Excel Export'],
    ['ملاحظة: بعد فك الضغط وتشغيل ملف', 'Note: After extracting the archive and running'],
    ['طباعة A4 مزدوجة وختم رسمي مفرغ', 'Dual A4 Printing & Transparent Official Stamp'],
    ['مركز تنبيهات ذكي وتقارير Excel', 'Smart Notification Center & Excel Reports'],
    ['نظام المحاسبة والتصنيع للمعامل', 'Factory Accounting & Manufacturing ERP'],
    ['داخل برنامج Invoify على جهازك.', 'window inside Invoify on your PC.'],
    ['نظام إدارة المبيعات والفواتير', 'Sales & Invoicing Management System'],
    ['حسابات عملاء وكشف حساب تراكمي', 'Customer Accounts & Cumulative Ledger Statement'],
    ['تعدد العملات وسجل أسعار الصرف', 'Multi-Currency & Exchange Rate History'],
    ['صلاحيات الموظفين وسجل التدقيق', 'Role Permissions & Security Audit Log'],
    ['/ دفعة واحدة بدون اشتراك شهري', '/ One-time payment, no monthly fees'],
    ['20 فاتورة فقط كاملة الصلاحيات', '20 Invoices Only (Full Features)'],
    ['تحميل النسخة التجريبية مجاناً', 'Download Free Trial'],
    ['جرّبه مجاناً 20 فاتورة فقط ←', 'Try Free (20 Invoices Only) →'],
    ['1. شريط بيانات العميل والدفع', '1. Customer & Payment Header Bar'],
    ['3. الحفظ، المسودات، والطباعة', '3. Save, Drafts & High-DPI Printing'],
    ['🛠️ الفحص الذاتي وضغط القاعدة', '🛠️ Self-Diagnostics & Database Vacuum'],
    ['أرسله لنا عبر واتساب لاستلام', 'Send it to us via WhatsApp to receive your official'],
    ['إرسال طلب الترخيص عبر واتساب', 'Send License Request via WhatsApp'],
    ['تبديل الوضع الليلي / النهاري', 'Toggle Dark / Light Mode'],
    ['📦 المنتجات والوحدات الهرمية', '📦 Products & Hierarchical Units'],
    ['طلب شراء مفتاح الترخيص الآن', 'Request License Key Now'],
    ['🔑 شراء وتفعيل ترخيص Invoify', '🔑 Buy & Activate Invoify License'],
    ['الصفحة غير موجودة — Invoify', 'Page Not Found — Invoify'],
    ['جرّبه مجاناً 20 فاتورة فقط', 'Try Free (20 Invoices Only)'],
    ['✓ نظام المبيعات متصل وجاهز', '✓ Sales System Ready & Active'],
    ['ميزات حقيقية من قلب النظام', 'Real Built-In ERP Capabilities'],
    ['🔏 تفريغ خلفية الختم الرسمي', '🔏 Automatic Stamp Background Removal'],
    ['الترخيص الكامل غير المحدود', 'Unlimited Full License'],
    ['العودة إلى الصفحة الرئيسية', 'Return to Home Page'],
    ['1 صندوق = 10 طرد = 100 كغ', '1 Box = 10 Parcels = 100 kg'],
    ['35 اختباراً ذاتياً مدمجاً', '35 Built-In Self-Diagnostics'],
    ['معرّف الجهاز (Machine ID)', 'Machine ID'],
    ['النسخة التجريبية المجانية', 'Free Trial Version'],
    ['بياناتك محفوظة على جهازك', 'Your data stays on your PC'],
    ['👥 ملف العميل وكشف الحساب', '👥 Customer Profile & Statement'],
    ['إجمالي المبيعات (المدين)', 'Total Sales (Debit)'],
    ['اتجاهان مرنان لسعر الصرف', 'Bidirectional Exchange Rate Modes'],
    ['النسخة التجارية المعتمدة', 'Certified Commercial Edition'],
    ['مثال: INV-8F4A-2026-XXXX', 'Example: INV-8F4A-2026-XXXX'],
    ['فترة تجربة مجانية كاملة', 'Full-Featured Free Trial'],
    ['حاسبة تحويل عملات مدمجة', 'Built-In Currency Converter'],
    ['حماية الارتباط المحاسبي', 'Accounting Integrity Protection'],
    ['الظاهر أمامك في الشاشة.', 'displayed on your screen.'],
    ['طباعة وتصدير PDF و PNG', 'Print & Export PDF / PNG'],
    ['💱 العملات وأسعار الصرف', '💱 Currencies & Exchange Rates'],
    ['المكافئ بالكغ (تلقائي)', 'kg Equivalent (Auto)'],
    ['🔢 التحكم برقم الفاتورة', '🔢 Invoice Sequence Control'],
    ['نسخة الإدارة • N° 0042', 'Admin Copy • N° 0042'],
    ['نظام اللصاقات التجارية', 'Commercial Label System'],
    ['نظام المحاسبة والتصنيع', 'Accounting & Manufacturing ERP'],
    ['تحميل النسخة التجريبية', 'Download Free Trial'],
    ['تنزيل النسخة التجريبية', 'Download Free Trial'],
    ['👤 مؤسسة الشام للتجارة', '👤 Al-Sham Trading Est.'],
    ['رائحة فاخرة بالمكسرات', 'Premium Nut Delight'],
    ['🧾 شاشة إصدار الفواتير', '🧾 Invoice Issuance Screen'],
    ['ميزة حصرية في Invoify', 'Exclusive Invoify Feature'],
    ['شركة الحلويات الفاخرة', 'Fine Sweets Co.'],
    ['راحة فاخرة بالمكسرات', 'Premium Nut Delight'],
    ['نوغا سلطانية بالفستق', 'Royal Pistachio Nougat'],
    ['مرحباً بك في Invoify', 'Welcome to Invoify'],
    ['نظام إدارة المبيعات', 'Sales Management System'],
    ['شراء وتفعيل الترخيص', 'Buy & Activate License'],
    ['صافي الرصيد المستحق', 'Net Balance Due'],
    ['منظومة حلول متكاملة', 'Integrated Software Ecosystem'],
    ['تفاصيل ملف التثبيت:', 'Installer Package Details:'],
    ['الخاص بجهازك فوراً.', 'for your PC immediately.'],
    ['مرحبا بك في Invoify', 'Welcome to Invoify'],
    ['مؤسسة الشام للتجارة', 'Al-Sham Trading Est.'],
    ['شراء النسخة الكاملة', 'Buy Full Version'],
    ['نسخة العميل • نقدي', 'Customer Copy • Cash'],
    ['أعلمني عند الإطلاق', 'Notify Me on Launch'],
    ['نسخ احتياطي تلقائي', 'Automatic Backup'],
    ['جميع الحقوق محفوظة', 'All rights reserved'],
    ['الوحدة الافتراضية', 'Default Unit'],
    ['ابدأ مجاناً اليوم', 'Start Free Today'],
    ['منتجاتنا البرمجية', 'Our Software Suite'],
    ['متاح للتحميل الآن', 'Available for Download Now'],
    ['تمت العملية بنجاح', 'Operation completed successfully'],
    ['جولة داخل النظام', 'Interactive System Tour'],
    ['🛡️ موثقة ومعتمدة', '🛡️ Verified & Certified'],
    ['الصافي: $ 790.00', 'Net: $ 790.00'],
    ['ترخيص مدى الحياة', 'Lifetime License'],
    ['النسخة التجريبية', 'Trial Version'],
    ['20 فاتورة مجانية', '20 Free Invoices'],
    ['التجربة والترخيص', 'Trial & Licensing'],
    ['الإجمالي النهائي', 'Grand Total'],
    ['الفترة التجريبية', 'Trial Limit'],
    ['الطباعة الحرارية', 'Thermal Printing'],
    ['التقارير العامة', 'General Reports'],
    ['تعيين كافتراضية', 'Set as Default'],
    ['⏱️ مدة التجربة:', '⏱️ Trial Limit:'],
    ['🔒 الفحص الأمني:', '🔒 Security Check:'],
    ['الميزات الفعلية', 'Core Features'],
    ['معاينة الواجهات', 'UI Previews'],
    ['الأسئلة الشائعة', 'FAQ'],
    ['المدة التجريبية', 'Trial Limit'],
    ['تحويل هرمي ذكي', 'Smart Hierarchical Conversion'],
    ['تصميم الفاتورة', 'Invoice Design'],
    ['فاتورة مشتريات', 'Purchase Invoice'],
    ['تفاصيل الأصناف', 'Item Details'],
    ['الصافي المطلوب', 'Net Amount Due'],
    ['المبلغ المطلوب', 'Amount Due'],
    ['معاينة الطباعة', 'Print Preview'],
    ['النسخة الكاملة', 'Full Version'],
    ['قاعدة البيانات', 'Database'],
    ['السعر الإفرادي', 'Unit Price'],
    ['المبلغ المدفوع', 'Paid Amount'],
    ['المبلغ المتبقي', 'Remaining Balance'],
    ['إدارة العملات', 'Currency Management'],
    ['10 طرد × 5 كغ', '10 Parcels × 5 kg'],
    ['سعر البيع ($)', 'Selling Price ($)'],
    ['1 كغ (أساسية)', '1 kg (Base Unit)'],
    ['1 طرد = 10 كغ', '1 Parcel = 10 kg'],
    ['الوزن: 190 Kg', 'Weight: 190 Kg'],
    ['المنتج القادم', 'Upcoming Product'],
    ['إصدار مستقبلي', 'Future Release'],
    ['تفعيل الترخيص', 'Activate License'],
    ['20 فاتورة فقط', '20 Invoices Only'],
    ['٢٠ فاتورة فقط', '20 Invoices Only'],
    ['فاتورة مبيعات', 'Sales Invoice'],
    ['مرتجع مشتريات', 'Purchase Return'],
    ['ملخص الفاتورة', 'Invoice Summary'],
    ['حزمة البرامج', 'Software Suite'],
    ['مرتجع مبيعات', 'Sales Return'],
    ['رقم الفاتورة', 'Invoice No.'],
    ['إجمالي الوزن', 'Total Weight'],
    ['إنشاء فاتورة', 'Create Invoice'],
    ['فاتورة جديدة', 'New Invoice'],
    ['تجربة مجانية', 'Free Trial'],
    ['تعدد العملات', 'Multi-Currency'],
    ['تعدد الوحدات', 'Multi-Unit System'],
    ['📅 آجل (ذمة)', '📅 Credit (Receivable)'],
    ['10 كغ / طرد', '10 kg / Parcel'],
    ['راحة مكسرات', 'Nut Delight'],
    ['قيد التطوير', 'In Development'],
    ['العدد الكلي', 'Total Units'],
    ['لوحة التحكم', 'Dashboard'],
    ['الدعم الفني', 'Technical Support'],
    ['نسخ احتياطي', 'Backup'],
    ['طريقة الدفع', 'Payment Method'],
    ['⭐ افتراضية', '⭐ Default'],
    ['ترخيص دائم', 'Perpetual License'],
    ['📦 الإصدار:', '📦 Version:'],
    ['افتح نافذة', 'Open the'],
    ['آجل (دفعة)', 'Credit (Installment)'],
    ['تحويل هرمي', 'Hierarchical Conversion'],
    ['المستودعات', 'Warehouses'],
    ['ترخيص سنوي', 'Annual License'],
    ['تواصل معنا', 'Contact Us'],
    ['نقاط البيع', 'Point of Sale (POS)'],
    ['اسم العميل', 'Customer Name'],
    ['100% محلي', '100% Local & Offline'],
    ['نوغا فستق', 'Pistachio Nougat'],
    ['20 فاتورة', '20 Invoices'],
    ['تحتوي على', 'Contains'],
    ['من الوحدة', 'Of Unit'],
    ['المشتريات', 'Purchases'],
    ['الإعدادات', 'Settings'],
    ['رصيد سابق', 'Previous Balance'],
    ['المحتويات', 'Contents'],
    ['الرئيسية', 'Home'],
    ['المنتجات', 'Products'],
    ['المبيعات', 'Sales'],
    ['الموردين', 'Suppliers'],
    ['الموردون', 'Suppliers'],
    ['الحسابات', 'Accounts'],
    ['التقارير', 'Reports'],
    ['الفواتير', 'Invoices'],
    ['نقطة بيع', 'Point of Sale (POS)'],
    ['الإجمالي', 'Total'],
    ['7 وحدات', '7 Units'],
    ['3 صندوق', '3 Boxes'],
    ['الأصناف', 'Items'],
    ['المخزون', 'Inventory'],
    ['العملاء', 'Customers'],
    ['المالية', 'Finance'],
    ['الضريبة', 'Tax'],
    ['التاريخ', 'Date'],
    ['150 كغ', '150 kg'],
    ['190 كغ', '190 kg'],
    ['100 كغ', '100 kg'],
    ['قريباً', 'Coming Soon'],
    ['الوحدة', 'Unit'],
    ['البيان', 'Description'],
    ['الكمية', 'Quantity'],
    ['العملة', 'Currency'],
    ['مجاناً', 'Free'],
    ['40 كغ', '40 kg'],
    ['10 كغ', '10 kg'],
    ['4 طرد', '4 Parcels'],
    ['الوصف', 'Description'],
    ['العدد', 'Qty'],
    ['السعر', 'Price'],
    ['الوزن', 'Weight'],
    ['الخصم', 'Discount'],
    ['الحسم', 'Discount'],
    ['وحدات', 'Units'],
    ['صندوق', 'Box'],
    ['طباعة', 'Print'],
    ['تنزيل', 'Download'],
    ['تحميل', 'Download'],
    ['إلغاء', 'Cancel'],
    ['إغلاق', 'Close'],
    ['مجانا', 'Free'],
    ['انسخ', 'Copy the'],
    ['وحدة', 'Unit'],
    ['نقدي', 'Cash'],
    ['طرد', 'Parcel'],
    ['آجل', 'Credit'],
    ['حفظ', 'Save'],
    ['بحث', 'Search'],
    ['ذكي', 'Smart'],
    ['كغ', 'kg']
  ];

  exactTranslations.sort(function (a, b) {
    return b[0].length - a[0].length;
  });

  var wordDictionary = {
    'نظام': 'System',
    'برنامج': 'Software',
    'تطبيق': 'Application',
    'إدارة': 'Management',
    'محاسبة': 'Accounting',
    'محاسبي': 'Accounting',
    'متكامل': 'Integrated',
    'احترافي': 'Professional',
    'احترافية': 'Professional',
    'سريع': 'Fast',
    'سهولة': 'Ease',
    'بكل': 'with',
    'سهلة': 'Easy',
    'واجهة': 'Interface',
    'واجهات': 'Interfaces',
    'عربية': 'Arabic',
    'عصري': 'Modern',
    'عصرية': 'Modern',
    'دقيق': 'Accurate',
    'دقيقة': 'Accurate',
    'شامل': 'Comprehensive',
    'شاملة': 'Comprehensive',
    'مبيعات': 'Sales',
    'مشتريات': 'Purchases',
    'فواتير': 'Invoices',
    'فاتورة': 'Invoice',
    'مخزون': 'Inventory',
    'مستودع': 'Warehouse',
    'مستودعات': 'Warehouses',
    'أصناف': 'Items',
    'صنف': 'Item',
    'منتجات': 'Products',
    'منتج': 'Product',
    'عملاء': 'Customers',
    'عميل': 'Customer',
    'موردين': 'Suppliers',
    'مورد': 'Supplier',
    'تقارير': 'Reports',
    'تقرير': 'Report',
    'إحصائيات': 'Analytics',
    'أرباح': 'Profits',
    'خسائر': 'Losses',
    'مصاريف': 'Expenses',
    'صندوق': 'Box',
    'دفعات': 'Payments',
    'دفعة': 'Payment',
    'ديون': 'Debts',
    'ذمم': 'Receivables',
    'كشف': 'Statement',
    'حساب': 'Account',
    'حسابات': 'Accounts',
    'عملات': 'Currencies',
    'عملة': 'Currency',
    'دولار': 'USD',
    'ليرة': 'Lira',
    'سعر': 'Price',
    'أسعار': 'Prices',
    'صرف': 'Exchange',
    'تحويل': 'Conversion',
    'وزن': 'Weight',
    'أوزان': 'Weights',
    'كمية': 'Quantity',
    'كميات': 'Quantities',
    'عدد': 'Count',
    'قطعة': 'Piece',
    'قطع': 'Pieces',
    'كرتونة': 'Carton',
    'علبة': 'Pack',
    'طباعة': 'Printing',
    'معاينة': 'Preview',
    'تصدير': 'Export',
    'استيراد': 'Import',
    'نسخة': 'Version',
    'تجريبية': 'Trial',
    'كاملة': 'Full',
    'مجانية': 'Free',
    'تفعيل': 'Activation',
    'ترخيص': 'License',
    'مفتاح': 'Key',
    'حماية': 'Security',
    'أمان': 'Safety',
    'سرعة': 'Speed',
    'أداء': 'Performance',
    'عالي': 'High',
    'عالية': 'High',
    'بدون': 'Without',
    'انترنت': 'Internet',
    'إنترنت': 'Internet',
    'محلي': 'Local',
    'محلية': 'Local',
    'سحابي': 'Cloud',
    'ويندوز': 'Windows',
    'كمبيوتر': 'Desktop',
    'موبايل': 'Mobile',
    'هاتف': 'Phone',
    'شاشة': 'Screen',
    'شاشات': 'Screens',
    'كامل': 'Full',
    'تفاصيل': 'Details',
    'ملخص': 'Summary',
    'إجمالي': 'Total',
    'صافي': 'Net',
    'مطلوب': 'Due',
    'مدفوع': 'Paid',
    'متبقي': 'Remaining',
    'خصم': 'Discount',
    'ضريبة': 'Tax',
    'ملاحظات': 'Notes',
    'إضافة': 'Add',
    'تعديل': 'Edit',
    'حذف': 'Delete',
    'عرض': 'View',
    'اختيار': 'Select',
    'تحديد': 'Select',
    'الكل': 'All',
    'نعم': 'Yes',
    'لا': 'No',
    'من': 'From',
    'إلى': 'To',
    'في': 'in',
    'على': 'on',
    'مع': 'with',
    'أو': 'or',
    'كل': 'Every',
    'أي': 'Any',
    'فقط': 'Only',
    'الآن': 'Now',
    'ابدأ': 'Start',
    'جرب': 'Try',
    'لماذا': 'Why',
    'كيف': 'How',
    'متى': 'When',
    'هل': 'Does',
    'يعمل': 'Works',
    'يدعم': 'Supports',
    'يتيح': 'Allows',
    'يوفر': 'Provides',
    'يضمن': 'Ensures',
    'حزمة': 'Suite',
    'برامج': 'Programs',
    'ميزات': 'Features',
    'ميزة': 'Feature',
    'أسئلة': 'Questions',
    'شائعة': 'Common',
    'سؤال': 'Question',
    'جواب': 'Answer',
    'تحديث': 'Update',
    'تحديثات': 'Updates',
    'دعم': 'Support',
    'فني': 'Technical',
    'مباشر': 'Direct',
    'تلقائي': 'Automatic',
    'يدوي': 'Manual',
    'باركود': 'Barcode',
    'قارئ': 'Scanner',
    'طابعة': 'Printer',
    'حرارية': 'Thermal',
    'ورق': 'Paper',
    'قالب': 'Template',
    'قوالب': 'Templates',
    'تخصيص': 'Customization',
    'شعار': 'Logo',
    'شركة': 'Company',
    'مؤسسة': 'Enterprise',
    'متجر': 'Store',
    'محل': 'Shop',
    'مستلزمات': 'Supplies',
    'تجارة': 'Trading',
    'عامة': 'General',
    'جملة': 'Wholesale',
    'مفرق': 'Retail',
    'تجزئة': 'Retail',
    'نسخ': 'Copy',
    'العميل': 'Customer',
    'الإدارة': 'Administration',
    'اليمين': 'Right',
    'اليسار': 'Left',
    'خط': 'Line',
    'قص': 'Cut',
    'إشعار': 'Notice',
    'استلام': 'Receipt',
    'مستودعي': 'Warehouse',
    'بقاء': 'Keeping',
    'الأسعار': 'Prices',
    'الأرقام': 'Numbers',
    'جدول': 'Table',
    'تغطية': 'Covering',
    'شفاف': 'Transparent'
  };

  // 1. إزالة بطاقة Desktop ERP المزعجة إن وجدت
  function removeDesktopErpBadge() {
    var candidates = document.querySelectorAll('header span, header div, header small, header a, span, div');
    for (var i = 0; i < candidates.length; i++) {
      var el = candidates[i];
      if (el.children.length === 0) {
        var txt = (el.textContent || '').replace(/\s+/g, ' ').trim();
        if (/^desktop\s*erp$/i.test(txt) || /^desctop\s*erp$/i.test(txt)) {
          el.remove();
        }
      }
    }
  }

  // 2. توحيد عبارة الفترة التجريبية إلى 20 فاتورة فقط
  function normalizeTrialText(text) {
    if (!text) return text;
    var updated = text;
    if ((updated.indexOf('30') !== -1 || updated.indexOf('٣٠') !== -1) && updated.indexOf('يوم') !== -1) {
      updated = updated
        .replace(trialRegexWithDuration, '20 فاتورة فقط')
        .replace(trialRegexGeneral, '20 فاتورة فقط');
    }
    if (updated.indexOf('لمدة شهر كامل') !== -1) {
      updated = updated.replace(trialRegexMonth, 'لغاية 20 فاتورة مجانية');
    }
    return updated;
  }

  // 3. ترجمة النص العربي إلى الإنجليزية بدقة
  function translateArabicToEnglish(arText) {
    if (!arText) return arText;
    var leadingSpace = /^\s/.test(arText) ? ' ' : '';
    var trailingSpace = /\s$/.test(arText) ? ' ' : '';
    var result = normalizeTrialText(arText.replace(/\s+/g, ' ').trim());
    if (!result) return arText;

    for (var i = 0; i < exactTranslations.length; i++) {
      var arPhrase = exactTranslations[i][0];
      var enPhrase = exactTranslations[i][1];
      if (result === arPhrase) {
        return leadingSpace + enPhrase + trailingSpace;
      }
      if (result.indexOf(arPhrase) !== -1) {
        result = result.split(arPhrase).join(enPhrase);
      }
    }

    if (/[\u0600-\u06FF]/.test(result)) {
      result = result.replace(/[\u0600-\u06FF]+/g, function (word) {
        if (wordDictionary[word]) {
          return wordDictionary[word];
        }
        var stripped = word;
        var prefixEn = '';
        if (stripped.indexOf('وال') === 0) {
          prefixEn = 'and ';
          stripped = stripped.substring(3);
        } else if (stripped.indexOf('بال') === 0) {
          prefixEn = 'by ';
          stripped = stripped.substring(3);
        } else if (stripped.indexOf('لل') === 0) {
          prefixEn = 'for ';
          stripped = stripped.substring(2);
        } else if (stripped.indexOf('ال') === 0) {
          stripped = stripped.substring(2);
        } else if (stripped.indexOf('و') === 0 && stripped.length > 3) {
          prefixEn = 'and ';
          stripped = stripped.substring(1);
        }
        if (wordDictionary[stripped]) {
          return prefixEn + wordDictionary[stripped];
        }
        return word;
      });
    }

    return leadingSpace + result + trailingSpace;
  }

  // 4. تطبيق اللغة المختارة بدون أي حلقات مراقبة (Zero Freeze)
  function applyLanguage(lang) {
    currentLang = lang === 'en' ? 'en' : 'ar';
    try {
      localStorage.setItem('invoify_lang', currentLang);
    } catch (e) {}

    var targetDir = currentLang === 'en' ? 'ltr' : 'rtl';
    if (document.documentElement.getAttribute('lang') !== currentLang) {
      document.documentElement.setAttribute('lang', currentLang);
    }
    if (document.documentElement.getAttribute('dir') !== targetDir) {
      document.documentElement.setAttribute('dir', targetDir);
    }

    if (origDocTitle === null) {
      origDocTitle = normalizeTrialText(document.title || '');
    }
    if (origDocTitle) {
      document.title = currentLang === 'en' ? translateArabicToEnglish(origDocTitle).trim() : origDocTitle;
    }

    var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
      acceptNode: function (node) {
        var parent = node.parentNode;
        if (!parent) return NodeFilter.FILTER_REJECT;
        var tag = parent.tagName ? parent.tagName.toUpperCase() : '';
        if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT') {
          return NodeFilter.FILTER_REJECT;
        }
        if (parent.id === 'invoify-lang-btn' || (parent.closest && parent.closest('#invoify-lang-btn'))) {
          return NodeFilter.FILTER_REJECT;
        }
        return NodeFilter.FILTER_ACCEPT;
      }
    });

    var node;
    while ((node = walker.nextNode())) {
      var raw = node.nodeValue || '';
      if (!raw.trim()) continue;

      var arOrig = origTextMap ? origTextMap.get(node) : node.__arOrig;
      if (typeof arOrig !== 'string') {
        arOrig = normalizeTrialText(raw);
        if (origTextMap) {
          origTextMap.set(node, arOrig);
        } else {
          node.__arOrig = arOrig;
        }
      }

      var desired = currentLang === 'en' ? translateArabicToEnglish(arOrig) : arOrig;
      if (node.nodeValue !== desired) {
        node.nodeValue = desired;
      }
    }

    var attrElements = document.querySelectorAll('[placeholder], [title], [aria-label], [data-label], [data-notify-product]');
    for (var j = 0; j < attrElements.length; j++) {
      var el = attrElements[j];
      if (el.id === 'invoify-lang-btn') continue;

      var savedAttrs = origAttrMap ? origAttrMap.get(el) : el.__origAttrs;
      if (!savedAttrs) {
        savedAttrs = {};
        if (origAttrMap) {
          origAttrMap.set(el, savedAttrs);
        } else {
          el.__origAttrs = savedAttrs;
        }
      }

      var attrs = ['placeholder', 'title', 'aria-label', 'data-label', 'data-notify-product'];
      for (var k = 0; k < attrs.length; k++) {
        var attr = attrs[k];
        if (el.hasAttribute(attr)) {
          if (typeof savedAttrs[attr] !== 'string') {
            savedAttrs[attr] = normalizeTrialText(el.getAttribute(attr) || '');
          }
          var arVal = savedAttrs[attr];
          var newVal = currentLang === 'en' ? translateArabicToEnglish(arVal).trim() : arVal;
          if (el.getAttribute(attr) !== newVal) {
            el.setAttribute(attr, newVal);
          }
        }
      }
    }

    var langBtnLabel = document.querySelector('#invoify-lang-btn .lang-btn-text');
    var langBtn = document.getElementById('invoify-lang-btn');
    if (langBtnLabel) {
      var btnTxt = currentLang === 'en' ? 'عربي' : 'EN';
      if (langBtnLabel.textContent !== btnTxt) {
        langBtnLabel.textContent = btnTxt;
      }
    }
    if (langBtn) {
      var tip = currentLang === 'en' ? 'التبديل إلى العربية' : 'Switch to English';
      if (langBtn.getAttribute('title') !== tip) {
        langBtn.setAttribute('title', tip);
        langBtn.setAttribute('aria-label', tip);
      }
    }
  }

  // 5. إعداد القائمة المنزلقة للموبايل وأزرار الهيدر العلوية بدون تحريك زر الوضع الليلي من مكانه الأصلي
  var drawerEl = null;
  var backdropEl = null;

  function closeMobileDrawer() {
    if (drawerEl) drawerEl.classList.remove('is-open');
    if (backdropEl) backdropEl.classList.remove('is-open');
    document.body.classList.remove('drawer-lock');
  }

  function syncMobileDrawerLinks() {
    if (!drawerEl) return;
    var bodyContainer = drawerEl.querySelector('.invoify-drawer-links');
    if (!bodyContainer) return;

    var header = document.querySelector('header, nav.top-nav, .app-header');
    if (!header) return;

    bodyContainer.innerHTML = '';
    var headerLinks = header.querySelectorAll('a[href]');
    for (var i = 0; i < headerLinks.length; i++) {
      var a = headerLinks[i];
      var href = a.getAttribute('href') || '';
      var text = (a.textContent || '').replace(/\s+/g, ' ').trim();
      if (!text) continue;
      // استثناء رابط الشعار الرئيسي من التكرار داخل القائمة المنزلقة
      if (href === '#hero' || a.querySelector('svg') || /^invoify\b/i.test(text)) continue;

      var clone = a.cloneNode(true);
      var isCta = a.className.indexOf('bg-') !== -1 &&
        (text.indexOf('تحميل') !== -1 || text.indexOf('Download') !== -1 || text.indexOf('نسخة') !== -1 || text.indexOf('Trial') !== -1);
      clone.className = isCta ? 'invoify-drawer-cta' : 'invoify-drawer-item';
      clone.addEventListener('click', function () {
        setTimeout(closeMobileDrawer, 100);
      });
      bodyContainer.appendChild(clone);
    }
  }

  function toggleMobileDrawer(e) {
    if (e) e.stopPropagation();
    if (!drawerEl || !backdropEl) return;
    var willOpen = !drawerEl.classList.contains('is-open');
    if (willOpen) {
      syncMobileDrawerLinks();
    }
    drawerEl.classList.toggle('is-open', willOpen);
    backdropEl.classList.toggle('is-open', willOpen);
    document.body.classList.toggle('drawer-lock', willOpen);
  }

  function setupHeaderAndDrawer() {
    removeDesktopErpBadge();

    var header = document.querySelector('header, nav.top-nav, .app-header');
    if (!header) return;

    var headerInner = header.querySelector('div.flex, div[class*=""justify-between""], nav.flex') || header;
    headerInner.classList.add('invoify-header-inner');

    // تمييز قائمة الروابط الأفقية لإخفائها على شاشات الموبايل فقط
    var navCandidates = headerInner.querySelectorAll('nav, ul, div.flex');
    for (var n = 0; n < navCandidates.length; n++) {
      var cand = navCandidates[n];
      var linksCount = cand.querySelectorAll('a[href]').length;
      if (linksCount >= 3 && !cand.querySelector('button')) {
        cand.classList.add('invoify-desktop-nav');
      }
    }

    // إيجاد زر الوضع الليلي وإبقائه في حاويته الأصلية مع تنسيقه ووضع زر اللغة بجانبه مباشرة
    var darkBtn = null;
    var headerButtons = header.querySelectorAll('button, [role=""button""]');
    for (var i = 0; i < headerButtons.length; i++) {
      var b = headerButtons[i];
      if (b.id === 'invoify-lang-btn' || b.id === 'invoify-hamburger-btn') continue;
      var txt = (b.textContent || '').trim();
      var aria = (b.getAttribute('aria-label') || '') + ' ' + (b.getAttribute('title') || '') + ' ' + (b.id || '') + ' ' + (b.className || '');
      if (txt.indexOf('🌙') !== -1 || txt.indexOf('☀️') !== -1 || txt.indexOf('🌞') !== -1 ||
          /theme|dark|mode|ليلي|داكن|مظهر/i.test(aria) ||
          (b.querySelector('svg') && txt.length <= 2)) {
        darkBtn = b;
        break;
      }
    }

    var targetContainer = (darkBtn && darkBtn.parentNode) ? darkBtn.parentNode : headerInner;
    targetContainer.classList.add('invoify-actions-group');

    if (darkBtn) {
      darkBtn.classList.add('invoify-utility-btn', 'invoify-dark-btn');
    }

    if (!document.getElementById('invoify-lang-btn')) {
      var langBtn = document.createElement('button');
      langBtn.type = 'button';
      langBtn.id = 'invoify-lang-btn';
      langBtn.className = 'invoify-utility-btn invoify-lang-btn';
      langBtn.setAttribute('title', 'Switch to English');
      langBtn.setAttribute('aria-label', 'Switch to English');
      langBtn.innerHTML =
        '<svg class=""lang-globe-icon"" width=""16"" height=""16"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"">' +
        '<circle cx=""12"" cy=""12"" r=""10""></circle>' +
        '<line x1=""2"" y1=""12"" x2=""22"" y2=""12""></line>' +
        '<path d=""M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z""></path>' +
        '</svg>' +
        '<span class=""lang-btn-text"">' + (currentLang === 'en' ? 'عربي' : 'EN') + '</span>';

      langBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        applyLanguage(currentLang === 'ar' ? 'en' : 'ar');
      });

      if (darkBtn && darkBtn.parentNode === targetContainer) {
        targetContainer.insertBefore(langBtn, darkBtn);
      } else {
        targetContainer.insertBefore(langBtn, targetContainer.firstChild);
      }
    }

    if (!document.getElementById('invoify-hamburger-btn')) {
      var menuBtn = document.createElement('button');
      menuBtn.type = 'button';
      menuBtn.id = 'invoify-hamburger-btn';
      menuBtn.className = 'invoify-utility-btn invoify-hamburger-btn';
      menuBtn.setAttribute('aria-label', 'القائمة');
      menuBtn.innerHTML =
        '<svg width=""20"" height=""20"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round"">' +
        '<line x1=""3"" y1=""6"" x2=""21"" y2=""6""></line>' +
        '<line x1=""3"" y1=""12"" x2=""21"" y2=""12""></line>' +
        '<line x1=""3"" y1=""18"" x2=""21"" y2=""18""></line>' +
        '</svg>';
      menuBtn.addEventListener('click', toggleMobileDrawer);
      targetContainer.appendChild(menuBtn);
    }

    if (!document.getElementById('invoify-mobile-drawer')) {
      backdropEl = document.createElement('div');
      backdropEl.id = 'invoify-drawer-backdrop';
      backdropEl.className = 'invoify-drawer-backdrop';
      backdropEl.addEventListener('click', closeMobileDrawer);

      drawerEl = document.createElement('aside');
      drawerEl.id = 'invoify-mobile-drawer';
      drawerEl.className = 'invoify-mobile-drawer';
      drawerEl.innerHTML =
        '<div class=""invoify-drawer-header"">' +
        '<div class=""invoify-drawer-brand"">' +
        '<span class=""invoify-drawer-logo"">V</span>' +
        '<div><strong class=""invoify-drawer-title"">Invoify</strong></div>' +
        '</div>' +
        '<button type=""button"" class=""invoify-drawer-close"" aria-label=""Close"">' +
        '<svg width=""20"" height=""20"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""></line><line x1=""6"" y1=""6"" x2=""18"" y2=""18""></line></svg>' +
        '</button>' +
        '</div>' +
        '<div class=""invoify-drawer-links""></div>';

      drawerEl.querySelector('.invoify-drawer-close').addEventListener('click', closeMobileDrawer);
      document.body.appendChild(backdropEl);
      document.body.appendChild(drawerEl);

      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeMobileDrawer();
      });
    } else {
      drawerEl = document.getElementById('invoify-mobile-drawer');
      backdropEl = document.getElementById('invoify-drawer-backdrop');
    }
  }

  // 6. تحويل الجداول القياسية إلى بطاقات متجاوبة على الموبايل مع استثناء جداول المعاينة المصغرة
  function enhanceTables() {
    var tables = document.querySelectorAll('table');
    for (var t = 0; t < tables.length; t++) {
      var table = tables[t];
      if (table.getAttribute('data-no-card-table') === 'true') {
        table.classList.remove('invoify-responsive-table');
        continue;
      }
      if (!table.classList.contains('invoify-responsive-table')) {
        table.classList.add('invoify-responsive-table');
      }
      var ths = table.querySelectorAll('thead th');
      var headers = [];
      for (var h = 0; h < ths.length; h++) {
        headers.push((ths[h].textContent || '').trim());
      }
      var rows = table.querySelectorAll('tbody tr');
      for (var r = 0; r < rows.length; r++) {
        var cells = rows[r].querySelectorAll('td');
        for (var c = 0; c < cells.length; c++) {
          if (headers[c] && cells[c].getAttribute('data-label') !== headers[c]) {
            cells[c].setAttribute('data-label', headers[c]);
          }
        }
      }
    }
  }

  function initMobileExperience() {
    removeDesktopErpBadge();
    setupHeaderAndDrawer();
    enhanceTables();
    applyLanguage(currentLang);

    // تحديث خفيف وآمن عند النقر على التبويبات التفاعلية دون استخدام MutationObserver نهائياً
    var clickTimer = null;
    document.addEventListener('click', function (e) {
      var target = e.target;
      if (!target) return;
      if (target.closest && (target.closest('#invoify-lang-btn') || target.closest('#invoify-hamburger-btn') || target.closest('#invoify-mobile-drawer'))) {
        return;
      }
      var interactive = target.closest ? target.closest('button, a, [role=""tab""], [role=""button""]') : null;
      if (interactive) {
        if (clickTimer) clearTimeout(clickTimer);
        clickTimer = setTimeout(function () {
          enhanceTables();
          if (currentLang === 'en') {
            applyLanguage('en');
          }
        }, 60);
      }
    }, { passive: true });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initMobileExperience);
  } else {
    initMobileExperience();
  }
})();
";
            File.WriteAllText(mobileJsPath, mobileJsCode, new UTF8Encoding(false));

            var notFoundPagePath = Path.Combine(webRootPath, "404.html");
            if (!File.Exists(notFoundPagePath))
            {
                var notFoundHtmlContent = @"<!-- File: wwwroot/404.html -->
<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"" class=""scroll-smooth"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>الصفحة غير موجودة — Invoify</title>
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
    <link href=""https://fonts.googleapis.com/css2?family=Cairo:wght@400;500;600;700;800;900&display=swap"" rel=""stylesheet"">
    <script src=""https://cdn.tailwindcss.com/3.4.17""></script>
    <script>
        tailwind.config = {
            darkMode: 'class',
            theme: {
                extend: {
                    fontFamily: {
                        cairo: ['Cairo', 'Segoe UI', 'Tahoma', 'sans-serif'],
                    },
                    colors: {
                        emeraldBrand: {
                            50: '#ECFDF5',
                            100: '#D1FAE5',
                            200: '#A7F3D0',
                            400: '#34D399',
                            500: '#10B981',
                            600: '#059669',
                            700: '#047857',
                            800: '#065F46',
                            900: '#064E3B',
                            950: '#032E22',
                        }
                    }
                }
            }
        }
    </script>
    <link rel=""stylesheet"" href=""/css/style.css"" />
</head>
<body class=""bg-[#F4F7F5] dark:bg-[#0F172A] text-[#0F172A] dark:text-[#F8FAFC] min-h-screen flex flex-col justify-between transition-colors duration-200 antialiased"">
    <header class=""bg-white/95 dark:bg-[#1E293B]/95 backdrop-blur border-b border-slate-200 dark:border-slate-700"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between"">
            <a href=""/"" class=""flex items-center gap-3 group"">
                <div class=""w-10 h-10 rounded-xl sidebar-gradient flex items-center justify-center shadow-sm border border-emeraldBrand-700"">
                    <svg class=""w-7 h-7"" viewBox=""0 0 40 40"" fill=""none"">
                        <defs>
                            <linearGradient id=""invoifyLogoGrad404"" x1=""0"" y1=""0"" x2=""1"" y2=""1"">
                                <stop offset=""0%"" stop-color=""#34D399""/>
                                <stop offset=""100%"" stop-color=""#10B981""/>
                            </linearGradient>
                        </defs>
                        <path fill=""url(#invoifyLogoGrad404)"" d=""M7,6 C10,6 12,8 15,16 L20,30 L27,10 C28.5,6.5 31,6 34,6 C36.5,6 38,8 36.5,11.5 L25,35 C23.5,38 21,39 18.5,39 C16,39 13.5,37.5 12,34 L4.5,11.5 C3.5,8.5 4.8,6 7,6 Z""/>
                        <circle cx=""30"" cy=""8"" r=""4"" fill=""#D1FAE5"" opacity=""0.9""/>
                    </svg>
                </div>
                <div>
                    <span class=""text-xl font-black tracking-tight text-emeraldBrand-800 dark:text-emeraldBrand-400"">Invoify</span>
                    <p class=""text-[11px] font-semibold text-slate-500 dark:text-slate-400 -mt-1"">نظام إدارة المبيعات والفواتير</p>
                </div>
            </a>
            <div class=""flex items-center gap-2.5"">
                <button type=""button"" id=""themeToggleBtn"" class=""p-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-amber-400 hover:border-emeraldBrand-500 transition"" title=""تبديل الوضع الليلي / النهاري"">
                    <span id=""themeIcon"">🌙</span>
                </button>
                <a href=""/"" class=""px-4 py-2 rounded-lg bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-bold text-xs sm:text-sm shadow-sm transition"">الرئيسية</a>
            </div>
        </div>
    </header>

    <main class=""flex-grow flex items-center justify-center px-4 py-16 hero-pattern"">
        <div class=""max-w-lg w-full bg-white dark:bg-[#1E293B] rounded-2xl border border-slate-200 dark:border-slate-700 shadow-xl p-8 text-center space-y-5"">
            <div class=""inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 font-black text-3xl border border-emeraldBrand-200 dark:border-emeraldBrand-700"">
                404
            </div>
            <h1 class=""text-2xl sm:text-3xl font-black text-slate-900 dark:text-white"">عذراً، الصفحة التي تبحث عنها غير موجودة</h1>
            <p class=""text-sm sm:text-base text-slate-600 dark:text-slate-300 leading-relaxed"">
                قد يكون الرابط غير صحيح أو تم نقل الصفحة. يمكنك العودة إلى الصفحة الرئيسية لمتابعة استعراض برنامج Invoify.
            </p>
            <div class=""pt-2"">
                <a href=""/"" class=""inline-flex items-center justify-center gap-2 px-6 py-3.5 rounded-xl bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-black text-sm shadow-lg transition"">
                    <span>العودة إلى الصفحة الرئيسية</span>
                </a>
            </div>
        </div>
    </main>

    <footer class=""bg-[#032E22] text-emeraldBrand-100 py-6 border-t border-emeraldBrand-900 text-xs sm:text-sm"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-4"">
            <div class=""flex items-center gap-3"">
                <span class=""font-black text-white text-base"">Invoify</span>
                <span class=""text-emeraldBrand-400"">•</span>
                <span class=""text-emeraldBrand-200"">نظام إدارة فواتير المبيعات والمستودعات</span>
            </div>
            <div class=""text-emeraldBrand-300"">
                جميع الحقوق محفوظة &copy; 2026 — صُمم بعناية لقطاع الأعمال والتجارة.
            </div>
        </div>
    </footer>

    <script src=""/js/app.js"" defer></script>
    <script src=""/js/invoify-mobile.js"" defer></script>
</body>
</html>
";
                File.WriteAllText(notFoundPagePath, notFoundHtmlContent, new UTF8Encoding(false));
            }

            // تحديث ملفات HTML داخل wwwroot: إزالة بطاقة Desktop ERP، وتوحيد عبارة 20 فاتورة، وتضمين ملفات CSS/JS
            var htmlFiles = Directory.GetFiles(webRootPath, "*.html", SearchOption.AllDirectories);
            foreach (var htmlFile in htmlFiles)
            {
                var html = File.ReadAllText(htmlFile, Encoding.UTF8);
                var originalHtml = html;

                html = Regex.Replace(
                    html,
                    @"<(?:span|div|small|badge)\b[^>]*>\s*Desktop\s*ERP\s*</(?:span|div|small|badge)>",
                    string.Empty,
                    RegexOptions.IgnoreCase);

                html = Regex.Replace(html, @"لمدة\s*(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?", "20 فاتورة فقط");
                html = Regex.Replace(html, @"(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?", "20 فاتورة فقط");
                html = Regex.Replace(html, @"لمدة\s*شهر\s*كامل", "لغاية 20 فاتورة مجانية");

                if (!html.Contains("/css/style.css", StringComparison.OrdinalIgnoreCase) &&
                    !html.Contains("css/style.css", StringComparison.OrdinalIgnoreCase) &&
                    html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</head>", "  <link rel=\"stylesheet\" href=\"/css/style.css\" />\n</head>", StringComparison.OrdinalIgnoreCase);
                }

                if (!html.Contains("invoify-mobile.js", StringComparison.OrdinalIgnoreCase) &&
                    html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</body>", "  <script src=\"/js/invoify-mobile.js\" defer></script>\n</body>", StringComparison.OrdinalIgnoreCase);
                }

                if (!string.Equals(html, originalHtml, StringComparison.Ordinal))
                {
                    File.WriteAllText(htmlFile, html, new UTF8Encoding(false));
                }
            }
        }
        catch
        {
            // ضمان عدم توقف الخادم في حال كانت صلاحيات الكتابة مقيدة
        }
    }
}