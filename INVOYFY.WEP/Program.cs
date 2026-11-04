// File: Program.cs
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
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

        // مزامنة ملفات التنسيقات والسكريبت المتجاوب مع الهواتف المحمولة عند بدء التشغيل
        EnsureResponsiveWebAssets(app.Environment.ContentRootPath, app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"));

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
                    "connect-src 'self'; " +
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

                    var errorPagePath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "404.html");
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

        // 3. معالجة الصفحات غير الموجودة (404) مع ضمان التوافق مع StaticFileMiddleware
        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
            {
                var notFoundPagePath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "404.html");
                if (File.Exists(notFoundPagePath))
                {
                    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                    {
                        context.Request.Method = HttpMethods.Get;
                    }
                }
            }
        });

        app.UseStatusCodePagesWithReExecute("/404.html");

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

                if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
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
                return;
            }

            var cssDir = Path.Combine(webRootPath, "css");
            Directory.CreateDirectory(cssDir);

            // مزامنة style.cs إلى style.css في حال قامت أداة الدمج بحفظه بامتداد .cs
            var styleCsPath = Path.Combine(cssDir, "style.cs");
            var styleCssPath = Path.Combine(cssDir, "style.css");
            if (File.Exists(styleCsPath))
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

            // مزامنة input.cs إلى input.css إن وجد
            var inputCsPath = Path.Combine(cssDir, "input.cs");
            var inputCssPath = Path.Combine(cssDir, "input.css");
            if (File.Exists(inputCsPath))
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

            // مزامنة tailwind.config إلى tailwind.config.js
            var tailwindConfigRaw = Path.Combine(contentRootPath, "tailwind.config");
            var tailwindConfigJs = Path.Combine(contentRootPath, "tailwind.config.js");
            if (File.Exists(tailwindConfigRaw))
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

            // توليد سكريبت التجاوب الذكي للهواتف المحمولة وربطه بملف index.html دون المساس بالمنطق المحاسبي
            var jsDir = Path.Combine(webRootPath, "js");
            Directory.CreateDirectory(jsDir);
            var mobileJsPath = Path.Combine(jsDir, "invoify-mobile.js");

            var mobileJsCode = @"/* File: wwwroot/js/invoify-mobile.js */
(function () {
  'use strict';

  function initMobileExperience() {
    document.documentElement.setAttribute('dir', 'rtl');

    // 1. إعداد القائمة الجانبية المنزلقة (Drawer) وزر الهامبرغر على الموبايل
    var sidebar = document.querySelector('.sidebar-gradient, aside, .app-sidebar, #sidebar');
    var header = document.querySelector('header, nav.top-nav, .app-header');

    if (sidebar) {
      sidebar.classList.add('invoify-responsive-sidebar');

      var backdrop = document.querySelector('.invoify-drawer-backdrop');
      if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.className = 'invoify-drawer-backdrop';
        backdrop.setAttribute('aria-hidden', 'true');
        document.body.appendChild(backdrop);
      }

      var closeDrawer = function () {
        sidebar.classList.remove('is-open');
        backdrop.classList.remove('is-open');
        document.body.classList.remove('drawer-lock');
      };

      var toggleDrawer = function (e) {
        if (e) {
          e.stopPropagation();
        }
        var willOpen = !sidebar.classList.contains('is-open');
        sidebar.classList.toggle('is-open', willOpen);
        backdrop.classList.toggle('is-open', willOpen);
        document.body.classList.toggle('drawer-lock', willOpen);
      };

      backdrop.addEventListener('click', closeDrawer);

      // إغلاق القائمة عند اختيار أي عنصر أو رابط داخلها في شاشات الموبايل
      sidebar.addEventListener('click', function (e) {
        var linkOrBtn = e.target.closest('a, button, [role=""button""], li');
        if (linkOrBtn && window.innerWidth < 1024) {
          setTimeout(closeDrawer, 120);
        }
      });

      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
          closeDrawer();
        }
      });

      // التحقق من وجود زر فتح القائمة أو إنشاؤه في الهيدر
      var existingToggle = document.querySelector('[data-drawer-toggle], #mobile-menu-btn, #sidebar-toggle, .hamburger-btn');
      if (existingToggle) {
        existingToggle.addEventListener('click', toggleDrawer);
      } else if (header) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'invoify-hamburger-btn';
        btn.setAttribute('aria-label', 'فتح القائمة');
        btn.innerHTML = '<svg width=""22"" height=""22"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round""><line x1=""3"" y1=""6"" x2=""21"" y2=""6""></line><line x1=""3"" y1=""12"" x2=""21"" y2=""12""></line><line x1=""3"" y1=""18"" x2=""21"" y2=""18""></line></svg>';
        btn.addEventListener('click', toggleDrawer);
        header.insertBefore(btn, header.firstChild);
      }
    }

    // 2. تحويل الجداول تلقائياً إلى بطاقات متجاوبة (Responsive Cards) مع الحفاظ على كافة الأعمدة والبيانات المحاسبية
    function enhanceTables() {
      var tables = document.querySelectorAll('table');
      tables.forEach(function (table) {
        table.classList.add('invoify-responsive-table');
        var headers = Array.from(table.querySelectorAll('thead th')).map(function (th) {
          return th.textContent.trim();
        });

        var rows = table.querySelectorAll('tbody tr');
        rows.forEach(function (tr) {
          var cells = tr.querySelectorAll('td');
          cells.forEach(function (td, idx) {
            if (!td.hasAttribute('data-label') && headers[idx]) {
              td.setAttribute('data-label', headers[idx]);
            }
          });
        });
      });
    }

    enhanceTables();

    // مراقبة أي تحديث ديناميكي على الجداول أو الفواتير لضمان استمرار عمل البطاقات المتجاوبة
    var observer = new MutationObserver(function (mutations) {
      var shouldEnhance = false;
      for (var i = 0; i < mutations.length; i++) {
        if (mutations[i].addedNodes.length > 0) {
          shouldEnhance = true;
          break;
        }
      }
      if (shouldEnhance) {
        enhanceTables();
      }
    });

    observer.observe(document.body, { childList: true, subtree: true });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initMobileExperience);
  } else {
    initMobileExperience();
  }
})();
";
            File.WriteAllText(mobileJsPath, mobileJsCode, new UTF8Encoding(false));

            // تضمين ملف التنسيقات وسكريبت التجاوب في ملفات HTML داخل wwwroot إن لم تكن مضمنة
            var htmlFiles = Directory.GetFiles(webRootPath, "*.html", SearchOption.AllDirectories);
            foreach (var htmlFile in htmlFiles)
            {
                var html = File.ReadAllText(htmlFile, Encoding.UTF8);
                var modified = false;

                if (!html.Contains("/css/style.css", StringComparison.OrdinalIgnoreCase) &&
                    !html.Contains("css/style.css", StringComparison.OrdinalIgnoreCase) &&
                    html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</head>", "  <link rel=\"stylesheet\" href=\"/css/style.css\" />\n</head>", StringComparison.OrdinalIgnoreCase);
                    modified = true;
                }

                if (!html.Contains("invoify-mobile.js", StringComparison.OrdinalIgnoreCase) &&
                    html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</body>", "  <script src=\"/js/invoify-mobile.js\" defer></script>\n</body>", StringComparison.OrdinalIgnoreCase);
                    modified = true;
                }

                if (modified)
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