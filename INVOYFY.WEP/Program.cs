// File: Program.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// توليد ملفات wwwroot تلقائياً داخل مجلد المشروع عند التشغيل لأول مرة
string projectRoot = builder.Environment.ContentRootPath;
WebsiteAssetsInitializer.EnsureWebFilesExist(projectRoot);

app.UseDefaultFiles();
app.UseStaticFiles();

// عرض الصفحة الرئيسية مباشرة مع قراءة أي تعديل من wwwroot/index.html
app.MapGet("/", async (HttpContext context) =>
{
    string indexPath = Path.Combine(projectRoot, "wwwroot", "index.html");
    if (File.Exists(indexPath))
    {
        string diskHtml = await File.ReadAllTextAsync(indexPath, Encoding.UTF8);
        return Results.Content(diskHtml, "text/html; charset=utf-8");
    }

    return Results.Content(WebsiteAssetsInitializer.DefaultHtml, "text/html; charset=utf-8");
});

// فتح المتصفح تلقائياً عند التشغيل
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5050";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
    }
});

app.Run();

public static class WebsiteAssetsInitializer
{
    public static void EnsureWebFilesExist(string contentRootPath)
    {
        try
        {
            string wwwrootDir = Path.Combine(contentRootPath, "wwwroot");
            string cssDir = Path.Combine(wwwrootDir, "css");
            string jsDir = Path.Combine(wwwrootDir, "js");

            Directory.CreateDirectory(wwwrootDir);
            Directory.CreateDirectory(cssDir);
            Directory.CreateDirectory(jsDir);

            string indexPath = Path.Combine(wwwrootDir, "index.html");
            if (!File.Exists(indexPath))
            {
                File.WriteAllText(indexPath, DefaultHtml, new UTF8Encoding(false));
            }

            string cssPath = Path.Combine(cssDir, "style.css");
            if (!File.Exists(cssPath))
            {
                File.WriteAllText(cssPath, DefaultCss, new UTF8Encoding(false));
            }

            string jsPath = Path.Combine(jsDir, "app.js");
            if (!File.Exists(jsPath))
            {
                File.WriteAllText(jsPath, DefaultJs, new UTF8Encoding(false));
            }
        }
        catch
        {
        }
    }

    public static readonly string DefaultCss = @"body {
            font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif;
        }
        .sidebar-gradient {
            background: linear-gradient(180deg, #064E3B 0%, #032E22 100%);
        }
        .hero-pattern {
            background-image: radial-gradient(rgba(16, 185, 129, 0.14) 1px, transparent 1px);
            background-size: 24px 24px;
        }";

    public static readonly string DefaultJs = @"(function () {
            // 1. تبديل الوضع الليلي والنهاري
            const themeToggleBtn = document.getElementById('themeToggleBtn');
            const themeIcon = document.getElementById('themeIcon');
            const htmlEl = document.documentElement;

            function applyTheme(isDark) {
                if (isDark) {
                    htmlEl.classList.add('dark');
                    themeIcon.textContent = '☀️';
                } else {
                    htmlEl.classList.remove('dark');
                    themeIcon.textContent = '🌙';
                }
                try {
                    localStorage.setItem('invoify_web_dark', isDark ? '1' : '0');
                } catch (e) {}
            }

            try {
                if (localStorage.getItem('invoify_web_dark') === '1') {
                    applyTheme(true);
                }
            } catch (e) {}

            themeToggleBtn.addEventListener('click', function () {
                const isDarkNow = !htmlEl.classList.contains('dark');
                applyTheme(isDarkNow);
            });

            // 2. التبديل بين شاشات البرنامج في قسم المعاينة
            const tabButtons = document.querySelectorAll('.screen-tab-btn');
            const panels = {
                invoices: document.getElementById('screen-panel-invoices'),
                products: document.getElementById('screen-panel-products'),
                customers: document.getElementById('screen-panel-customers'),
                currencies: document.getElementById('screen-panel-currencies'),
                settings: document.getElementById('screen-panel-settings')
            };

            tabButtons.forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const target = btn.getAttribute('data-screen');
                    tabButtons.forEach(function (b) {
                        b.className = 'screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition';
                    });
                    btn.className = 'screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-emeraldBrand-800 text-white shadow-sm transition';

                    Object.keys(panels).forEach(function (k) {
                        if (panels[k]) {
                            panels[k].classList.toggle('hidden', k !== target);
                        }
                    });
                });
            });

            // 3. النوافذ المنبثقة للتحميل وشراء الترخيص
            const downloadModal = document.getElementById('downloadModal');
            const licenseModal = document.getElementById('licenseModal');

            function openDownloadModal() {
                downloadModal.classList.remove('hidden');
            }
            function closeDownloadModal() {
                downloadModal.classList.add('hidden');
            }
            function openLicenseModal() {
                licenseModal.classList.remove('hidden');
            }
            function closeLicenseModal() {
                licenseModal.classList.add('hidden');
            }

            document.getElementById('heroDownloadBtn').addEventListener('click', openDownloadModal);
            document.getElementById('cardDownloadBtn').addEventListener('click', openDownloadModal);
            document.getElementById('closeDownloadModalBtn').addEventListener('click', closeDownloadModal);

            document.getElementById('heroBuyLicenseBtn').addEventListener('click', openLicenseModal);
            document.getElementById('cardBuyLicenseBtn').addEventListener('click', openLicenseModal);
            document.getElementById('closeLicenseModalBtn').addEventListener('click', closeLicenseModal);

            document.getElementById('topProductsTrigger').addEventListener('click', function () {
                const suiteEl = document.getElementById('suite');
                if (suiteEl) suiteEl.scrollIntoView({ behavior: 'smooth' });
            });

            // 4. إرسال طلب الترخيص عبر واتساب
            const toastBanner = document.getElementById('toastBanner');
            const toastMessage = document.getElementById('toastMessage');

            function showToast(msg) {
                toastMessage.textContent = msg;
                toastBanner.classList.remove('hidden');
                setTimeout(function () {
                    toastBanner.classList.add('hidden');
                }, 3500);
            }

            document.getElementById('sendWhatsappLicenseBtn').addEventListener('click', function () {
                const machineId = (document.getElementById('customerMachineIdInput').value || '').trim();
                const text = machineId
                    ? 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج Invoify. معرف الجهاز الخاص بي هو: ' + machineId
                    : 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج إدارة الفواتير Invoify.';
                const url = 'https://wa.me/?text=' + encodeURIComponent(text);
                window.open(url, '_blank', 'noopener,noreferrer');
                closeLicenseModal();
                showToast('تم تجهيز رسالة طلب الترخيص بنجاح.');
            });

            // 5. أزرار التنبيه بالمنتجات المستقبلية
            document.querySelectorAll('.notify-product-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const pName = btn.getAttribute('data-notify-product') || 'المنتج';
                    showToast('تم تسجيل اهتمامك بـ (' + pName + ') — سنخبرك فور إطلاقه!');
                });
            });
        })();";

    public static readonly string DefaultHtml = @"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"" class=""scroll-smooth"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invoify — نظام إدارة فواتير المبيعات والمستودعات والعملاء</title>
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
    <style>
        body {
            font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif;
        }
        .sidebar-gradient {
            background: linear-gradient(180deg, #064E3B 0%, #032E22 100%);
        }
        .hero-pattern {
            background-image: radial-gradient(rgba(16, 185, 129, 0.14) 1px, transparent 1px);
            background-size: 24px 24px;
        }
    </style>
</head>
<body class=""bg-[#F4F7F5] dark:bg-[#0F172A] text-[#0F172A] dark:text-[#F8FAFC] transition-colors duration-200 antialiased selection:bg-emeraldBrand-500 selection:text-white"">

    <!-- شريط الإشعار العلوي -->
    <div class=""bg-emeraldBrand-900 text-emeraldBrand-100 text-xs sm:text-sm py-2 px-4 border-b border-emeraldBrand-800"">
        <div class=""max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-2"">
            <div class=""flex items-center gap-2"">
                <span class=""inline-block w-2 h-2 rounded-full bg-emeraldBrand-400 animate-pulse""></span>
                <span class=""font-bold"">إصدار 2026 متوفر الآن لنظام Windows</span>
                <span class=""hidden md:inline text-emeraldBrand-200"">— يعمل بدون إنترنت بسرعة فائقة مع نسخ احتياطي ذري تلقائي.</span>
            </div>
            <div class=""flex items-center gap-4"">
                <button type=""button"" id=""topProductsTrigger"" class=""underline hover:text-white font-semibold"">منتجاتنا البرمجية</button>
                <a href=""#download-section"" class=""text-emeraldBrand-400 hover:text-white font-bold"">جرّبه مجاناً 30 يوماً &larr;</a>
            </div>
        </div>
    </div>

    <!-- شريط التنقل الرئيسي -->
    <header class=""sticky top-0 z-40 bg-white/95 dark:bg-[#1E293B]/95 backdrop-blur border-b border-slate-200 dark:border-slate-700"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between"">
            <!-- الشعار المطابق لتطبيق WPF -->
            <a href=""#hero"" class=""flex items-center gap-3 group"">
                <div class=""w-10 h-10 rounded-xl sidebar-gradient flex items-center justify-center shadow-sm border border-emeraldBrand-700"">
                    <svg class=""w-7 h-7"" viewBox=""0 0 40 40"" fill=""none"">
                        <defs>
                            <linearGradient id=""invoifyLogoGrad"" x1=""0"" y1=""0"" x2=""1"" y2=""1"">
                                <stop offset=""0%"" stop-color=""#34D399""/>
                                <stop offset=""100%"" stop-color=""#10B981""/>
                            </linearGradient>
                        </defs>
                        <path fill=""url(#invoifyLogoGrad)"" d=""M7,6 C10,6 12,8 15,16 L20,30 L27,10 C28.5,6.5 31,6 34,6 C36.5,6 38,8 36.5,11.5 L25,35 C23.5,38 21,39 18.5,39 C16,39 13.5,37.5 12,34 L4.5,11.5 C3.5,8.5 4.8,6 7,6 Z""/>
                        <circle cx=""30"" cy=""8"" r=""4"" fill=""#D1FAE5"" opacity=""0.9""/>
                    </svg>
                </div>
                <div>
                    <div class=""flex items-center gap-2"">
                        <span class=""text-xl font-black tracking-tight text-emeraldBrand-800 dark:text-emeraldBrand-400"">Invoify</span>
                        <span class=""text-[11px] font-bold px-2 py-0.5 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/60 text-emeraldBrand-800 dark:text-emeraldBrand-200 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">Desktop ERP</span>
                    </div>
                    <p class=""text-[11px] font-semibold text-slate-500 dark:text-slate-400 -mt-1"">نظام إدارة المبيعات والفواتير</p>
                </div>
            </a>

            <!-- روابط التنقل -->
            <nav class=""hidden md:flex items-center gap-6 text-sm font-bold text-slate-700 dark:text-slate-200"">
                <a href=""#features"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">الميزات الفعلية</a>
                <a href=""#live-screens"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">معاينة الواجهات</a>
                <a href=""#invoice-preview"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">تصميم الفاتورة</a>
                <a href=""#pricing"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">التجربة والترخيص</a>
                <a href=""#suite"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">حزمة البرامج</a>
                <a href=""#faq"" class=""hover:text-emeraldBrand-700 dark:hover:text-emeraldBrand-400 transition"">الأسئلة الشائعة</a>
            </nav>

            <!-- أزرار التحكم -->
            <div class=""flex items-center gap-2.5"">
                <button type=""button"" id=""themeToggleBtn"" class=""p-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-amber-400 hover:border-emeraldBrand-500 transition"" title=""تبديل الوضع الليلي / النهاري"">
                    <span id=""themeIcon"">🌙</span>
                </button>
                <a href=""#download-section"" class=""px-4 py-2 rounded-lg bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-bold text-xs sm:text-sm shadow-sm transition flex items-center gap-1.5"">
                    <span>⬇️</span>
                    <span>تحميل النسخة التجريبية</span>
                </a>
            </div>
        </div>
    </header>

    <!-- القسم الرئيسي (Hero Section) -->
    <section id=""hero"" class=""relative overflow-hidden hero-pattern pt-10 pb-16 lg:pt-14 lg:pb-20 border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""grid lg:grid-cols-12 gap-10 items-center"">
                <!-- النص التسويقي المبني على الميزات الحقيقية -->
                <div class=""lg:col-span-6 space-y-6"">
                    <div class=""inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 border border-emeraldBrand-200 dark:border-emeraldBrand-700 text-emeraldBrand-800 dark:text-emeraldBrand-200 text-xs font-bold"">
                        <span>✅</span>
                        <span>مصمم للمعامل وتجار الجملة والمفرق والشركات التجارية</span>
                    </div>

                    <h1 class=""text-3xl sm:text-4xl lg:text-[42px] font-black leading-[1.25] text-slate-900 dark:text-white"">
                        أدِر فواتيرك، مخزونك، وكشوف حسابات عملائك بدقة محاسبية عبر
                        <span class=""text-emeraldBrand-800 dark:text-emeraldBrand-400"">Invoify</span>
                    </h1>

                    <p class=""text-base sm:text-lg text-slate-600 dark:text-slate-300 leading-relaxed font-medium"">
                        تطبيق مكتبي احترافي يعمل على نظام Windows بدون الحاجة لاتصال بالإنترنت. يدعم الوحدات المترابطة هرمياً (كغ &larr; طرد &larr; صندوق)، تعدد العملات، الفواتير النقدية والآجلة، تعدد المعامل بقواعد بيانات مستقلة، وطباعة A4 بختم رسمي مفرغ ورمز QR.
                    </p>

                    <!-- أزرار التحميل والشراء -->
                    <div class=""flex flex-wrap items-center gap-3 pt-2"">
                        <button type=""button"" id=""heroDownloadBtn"" class=""px-6 py-3.5 rounded-xl bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-black text-base shadow-lg shadow-emeraldBrand-900/15 transition flex items-center gap-2.5"">
                            <span>⬇️</span>
                            <span>تحميل مجاني (تجربة 30 يوماً)</span>
                        </button>
                        <button type=""button"" id=""heroBuyLicenseBtn"" class=""px-5 py-3.5 rounded-xl bg-white dark:bg-slate-800 hover:bg-emeraldBrand-50 dark:hover:bg-slate-700 text-emeraldBrand-800 dark:text-emeraldBrand-300 font-bold text-base border border-slate-300 dark:border-slate-600 transition flex items-center gap-2"">
                            <span>🔑</span>
                            <span>شراء وتفعيل الترخيص</span>
                        </button>
                    </div>

                    <!-- شريط الثقة والمواصفات -->
                    <div class=""grid grid-cols-3 gap-3 pt-4 border-t border-slate-200 dark:border-slate-800 text-xs sm:text-sm"">
                        <div class=""bg-white dark:bg-slate-800/80 p-3 rounded-xl border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-emeraldBrand-800 dark:text-emeraldBrand-400 text-base"">30 يوماً</div>
                            <div class=""text-slate-500 dark:text-slate-400 font-semibold"">فترة تجربة مجانية كاملة</div>
                        </div>
                        <div class=""bg-white dark:bg-slate-800/80 p-3 rounded-xl border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sky-700 dark:text-sky-400 text-base"">100% محلي</div>
                            <div class=""text-slate-500 dark:text-slate-400 font-semibold"">بياناتك محفوظة على جهازك</div>
                        </div>
                        <div class=""bg-white dark:bg-slate-800/80 p-3 rounded-xl border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-amber-600 dark:text-amber-400 text-base"">1200 DPI</div>
                            <div class=""text-slate-500 dark:text-slate-400 font-semibold"">طباعة وتصدير PDF و PNG</div>
                        </div>
                    </div>
                </div>

                <!-- معاينة حية مطابقة 100% لواجهة تطبيق Invoify WPF -->
                <div class=""lg:col-span-6"">
                    <div class=""rounded-2xl overflow-hidden shadow-2xl border-2 border-slate-300 dark:border-slate-700 bg-[#F4F7F5] dark:bg-[#0F172A]"">
                        <!-- شريط عنوان نافذة ويندوز -->
                        <div class=""bg-white dark:bg-[#1E293B] px-4 py-2.5 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between"">
                            <div class=""flex items-center gap-3"">
                                <span class=""w-7 h-7 rounded-lg bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 flex items-center justify-center text-xs font-bold border border-emeraldBrand-200 dark:border-emeraldBrand-700"">👤</span>
                                <span class=""w-7 h-7 rounded-lg bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-xs relative"">
                                    🔔
                                    <span class=""absolute -top-1 -left-1 bg-red-600 text-white text-[9px] font-black px-1 rounded-full"">2</span>
                                </span>
                                <span class=""text-[11px] font-bold px-2.5 py-1 rounded-md bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">✓ نظام المبيعات متصل وجاهز</span>
                            </div>
                            <div class=""text-xs font-bold text-slate-500 dark:text-slate-400"">Invoify - نظام إدارة فواتير المبيعات</div>
                        </div>

                        <!-- جسم نافذة التطبيق (القائمة الجانبية الزمردية + شاشة الفواتير) -->
                        <div class=""grid grid-cols-12 min-h-[380px]"">
                            <!-- القائمة الجانبية الزمردية -->
                            <div class=""col-span-4 sm:col-span-3 sidebar-gradient p-3 text-emeraldBrand-100 flex flex-col justify-between"">
                                <div>
                                    <div class=""text-center py-2 border-b border-emeraldBrand-800 mb-3"">
                                        <div class=""font-black text-white text-base"">Invoify</div>
                                        <div class=""text-[10px] text-emeraldBrand-400 font-semibold"">نظام إدارة المبيعات</div>
                                    </div>
                                    <div class=""space-y-1 text-xs font-bold"">
                                        <div class=""px-2.5 py-2 rounded-lg bg-gradient-to-l from-emeraldBrand-400 to-emeraldBrand-600 text-white shadow-sm flex items-center gap-2"">
                                            <span>🧾</span><span>الفواتير</span>
                                        </div>
                                        <div class=""px-2.5 py-1.5 rounded-lg hover:bg-emeraldBrand-800/70 flex items-center gap-2"">
                                            <span>📦</span><span>المنتجات</span>
                                        </div>
                                        <div class=""px-2.5 py-1.5 rounded-lg hover:bg-emeraldBrand-800/70 flex items-center gap-2"">
                                            <span>👥</span><span>العملاء</span>
                                        </div>
                                        <div class=""px-2.5 py-1.5 rounded-lg hover:bg-emeraldBrand-800/70 flex items-center gap-2"">
                                            <span>📊</span><span>التقارير العامة</span>
                                        </div>
                                        <div class=""px-2.5 py-1.5 rounded-lg hover:bg-emeraldBrand-800/70 flex items-center gap-2"">
                                            <span>💱</span><span>إدارة العملات</span>
                                        </div>
                                    </div>
                                </div>
                                <div class=""pt-2 border-t border-emeraldBrand-800 text-xs font-bold"">
                                    <div class=""px-2.5 py-1.5 rounded-lg flex items-center gap-2 text-emeraldBrand-200"">
                                        <span>⚙️</span><span>الإعدادات</span>
                                    </div>
                                </div>
                            </div>

                            <!-- مساحة عمل الفاتورة -->
                            <div class=""col-span-8 sm:col-span-9 p-3 space-y-2.5 bg-[#F4F7F5] dark:bg-[#0F172A]"">
                                <!-- رأس الفاتورة -->
                                <div class=""bg-white dark:bg-[#1E293B] p-2.5 rounded-xl border border-slate-200 dark:border-slate-700 flex flex-wrap items-center justify-between gap-2"">
                                    <div class=""flex flex-wrap items-center gap-2 text-xs"">
                                        <span class=""px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-slate-800 font-bold"">👤 مؤسسة الشام للتجارة</span>
                                        <span class=""px-2.5 py-1 rounded-lg bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-700 font-bold"">📅 آجل (ذمة)</span>
                                        <span class=""px-2 py-1 rounded-lg bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 font-bold"">USD ($)</span>
                                    </div>
                                    <div class=""px-3 py-1 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs font-black"">
                                        <span class=""text-red-600"">N°</span> <span class=""text-emeraldBrand-700 dark:text-emeraldBrand-400"">0042</span>
                                    </div>
                                </div>

                                <!-- شريط التحويل الهرمي -->
                                <div class=""bg-emeraldBrand-50 dark:bg-emeraldBrand-950/50 border border-emeraldBrand-200 dark:border-emeraldBrand-800 rounded-lg px-3 py-1.5 text-[11px] font-bold text-emeraldBrand-800 dark:text-emeraldBrand-300 flex items-center justify-between"">
                                    <span>ℹ️ الوحدة: صندوق (10 طرد × 5 كغ = 50 كغ) | الإجمالي: 150 كغ</span>
                                    <span class=""text-[10px] opacity-80"">تحويل هرمي ذكي</span>
                                </div>

                                <!-- جدول مصغر للأصناف -->
                                <div class=""bg-white dark:bg-[#1E293B] rounded-xl border border-slate-200 dark:border-slate-700 overflow-hidden"">
                                    <table class=""w-full text-[11px] text-center"">
                                        <thead class=""bg-[#E6F4EA] dark:bg-[#1E3A34] text-slate-800 dark:text-emeraldBrand-200 font-bold"">
                                            <tr>
                                                <th class=""py-1.5 px-2"">#</th>
                                                <th class=""py-1.5 px-2 text-right"">البيان</th>
                                                <th class=""py-1.5 px-2"">الوحدة</th>
                                                <th class=""py-1.5 px-2"">المحتويات</th>
                                                <th class=""py-1.5 px-2"">الوزن</th>
                                                <th class=""py-1.5 px-2"">الإجمالي</th>
                                            </tr>
                                        </thead>
                                        <tbody class=""divide-y divide-slate-100 dark:divide-slate-800 font-semibold"">
                                            <tr>
                                                <td class=""py-2"">1</td>
                                                <td class=""py-2 text-right font-bold"">راحة فاخرة بالمكسرات</td>
                                                <td class=""py-2""><span class=""px-1.5 py-0.5 rounded bg-emeraldBrand-50 dark:bg-slate-800 text-emeraldBrand-800 dark:text-emeraldBrand-300 font-bold"">صندوق</span></td>
                                                <td class=""py-2 text-sky-700 dark:text-sky-400"">10 طرد × 5 كغ</td>
                                                <td class=""py-2 font-black text-emeraldBrand-700 dark:text-emeraldBrand-400"">150 كغ</td>
                                                <td class=""py-2 font-black"">$ 450.00</td>
                                            </tr>
                                            <tr class=""bg-[#FAFDFB] dark:bg-[#192334]"">
                                                <td class=""py-2"">2</td>
                                                <td class=""py-2 text-right font-bold"">نوغا سلطانية بالفستق</td>
                                                <td class=""py-2""><span class=""px-1.5 py-0.5 rounded bg-emeraldBrand-50 dark:bg-slate-800 text-emeraldBrand-800 dark:text-emeraldBrand-300 font-bold"">طرد</span></td>
                                                <td class=""py-2 text-sky-700 dark:text-sky-400"">10 كغ / طرد</td>
                                                <td class=""py-2 font-black text-emeraldBrand-700 dark:text-emeraldBrand-400"">40 كغ</td>
                                                <td class=""py-2 font-black"">$ 340.00</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>

                                <!-- بطاقات المجاميع السفلية -->
                                <div class=""grid grid-cols-3 gap-2 pt-1"">
                                    <div class=""bg-[#F0FDF4] dark:bg-[#13332D] border border-[#BBF7D0] dark:border-[#1F5E52] rounded-lg p-2 text-center"">
                                        <div class=""text-[10px] text-[#15803D] dark:text-[#6EE7B7] font-bold"">العدد الكلي</div>
                                        <div class=""text-sm font-black text-[#15803D] dark:text-[#6EE7B7]"">7 وحدات</div>
                                    </div>
                                    <div class=""bg-[#F0F9FF] dark:bg-[#152C44] border border-[#BAE6FD] dark:border-[#234E76] rounded-lg p-2 text-center"">
                                        <div class=""text-[10px] text-[#0369A1] dark:text-[#7DD3FC] font-bold"">إجمالي الوزن</div>
                                        <div class=""text-sm font-black text-[#0284C7] dark:text-[#38BDF8]"">190 كغ</div>
                                    </div>
                                    <div class=""bg-[#F9FBF7] dark:bg-[#1E2D3D] border border-[#E2E8D5] dark:border-[#334E68] rounded-lg p-2 text-center"">
                                        <div class=""text-[10px] text-slate-600 dark:text-slate-300 font-bold"">الصافي المطلوب</div>
                                        <div class=""text-sm font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">$ 790.00</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- قسم الميزات الحقيقية المستخرجة من الكود البرمجي -->
    <section id=""features"" class=""py-16 bg-white dark:bg-[#1E293B] border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""text-center max-w-3xl mx-auto mb-12"">
                <span class=""text-xs font-black px-3 py-1 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">ميزات حقيقية من قلب النظام</span>
                <h2 class=""text-2xl sm:text-3xl font-black mt-3 text-slate-900 dark:text-white"">كل ما يحتاجه معملك أو متجرك في برنامج واحد</h2>
                <p class=""text-slate-600 dark:text-slate-300 mt-2 text-sm sm:text-base"">تم تصميم كل نافذة في Invoify لتختصر وقت المحاسب وتمنع الأخطاء الحسابية والمخزنية.</p>
            </div>

            <div class=""grid sm:grid-cols-2 lg:grid-cols-4 gap-6"">
                <!-- ميزة 1 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-700 dark:text-emeraldBrand-400 flex items-center justify-center text-xl mb-4 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">⚖️</div>
                    <h3 class=""font-black text-base mb-2"">وحدات مترابطة هرمياً وتسعير مرن</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        عرّف وحدات الصنف (كغ &larr; طرد &larr; صندوق &larr; طبلية) مرة واحدة، وبع بالصندوق مع إمكانية التسعير بالكيلو أو بالطرد أو بالصندوق داخل الفاتورة بشكل مستقل.
                    </p>
                </div>

                <!-- ميزة 2 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-sky-50 dark:bg-sky-900/40 text-sky-700 dark:text-sky-400 flex items-center justify-center text-xl mb-4 border border-sky-200 dark:border-sky-800"">👥</div>
                    <h3 class=""font-black text-base mb-2"">حسابات عملاء وكشف حساب تراكمي</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        فصل محاسبي دقيق بين الفواتير النقدية والآجلة (الذمم)، سندات قبض، مرتجعات، حسم مكتسب، رصيد مدوّر (BF-000)، وتنبيه تلقائي عند تجاوز الحد الائتماني.
                    </p>
                </div>

                <!-- ميزة 3 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-amber-50 dark:bg-amber-900/40 text-amber-700 dark:text-amber-400 flex items-center justify-center text-xl mb-4 border border-amber-200 dark:border-amber-800"">💱</div>
                    <h3 class=""font-black text-base mb-2"">تعدد العملات وسجل أسعار الصرف</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        دعم الدولار، الليرة السورية، الليرة التركية، اليورو وغيرها مع اتجاهي تسعير، محول عملات فوري مدمج، وأرصدة عملاء مفصولة لكل عملة دون خلط.
                    </p>
                </div>

                <!-- ميزة 4 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-purple-50 dark:bg-purple-900/40 text-purple-700 dark:text-purple-400 flex items-center justify-center text-xl mb-4 border border-purple-200 dark:border-purple-800"">🖨️</div>
                    <h3 class=""font-black text-base mb-2"">طباعة A4 مزدوجة وختم رسمي مفرغ</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        طباعة نسختين جنباً إلى جنب (نسخة العميل + نسخة الإدارة) مع خيار إخفاء الأسعار عن نسخة العميل، رمز QR، وتفريغ تلقائي لخلفية الختم البيضاء.
                    </p>
                </div>

                <!-- ميزة 5 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-700 dark:text-emeraldBrand-400 flex items-center justify-center text-xl mb-4 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">🏭</div>
                    <h3 class=""font-black text-base mb-2"">تعدد المعامل والحسابات المستقلة</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        أنشئ عدة حسابات (يوزرنيم) على نفس الكمبيوتر، ولكل حساب قاعدة بيانات مستقلة تماماً (أصناف وفواتير وعملاء منفصلون) مع تبديل فوري بضغطة زر.
                    </p>
                </div>

                <!-- ميزة 6 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-rose-50 dark:bg-rose-900/40 text-rose-700 dark:text-rose-400 flex items-center justify-center text-xl mb-4 border border-rose-200 dark:border-rose-800"">🛡️</div>
                    <h3 class=""font-black text-base mb-2"">صلاحيات الموظفين وسجل التدقيق</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        حماية كلمات السر بتشفير PBKDF2-SHA256، تحديد صلاحيات الموظف (منع الحذف، منع الخصم، منع الإعدادات)، وسجل تدقيق (Audit Log) يوثق كل عملية.
                    </p>
                </div>

                <!-- ميزة 7 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-900/40 text-teal-700 dark:text-teal-400 flex items-center justify-center text-xl mb-4 border border-teal-200 dark:border-teal-800"">💾</div>
                    <h3 class=""font-black text-base mb-2"">نسخ احتياطي ذري وفحص سلامة البيانات</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        نسخ احتياطي تلقائي عند الإغلاق، سياسة النسخة الواحدة المتجددة أو آخر N نسخ، مع فحص سلامة SQLite (Integrity Check) قبل أي استعادة.
                    </p>
                </div>

                <!-- ميزة 8 -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 hover:border-emeraldBrand-500 transition"">
                    <div class=""w-11 h-11 rounded-xl bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-400 flex items-center justify-center text-xl mb-4 border border-indigo-200 dark:border-indigo-800"">🔔</div>
                    <h3 class=""font-black text-base mb-2"">مركز تنبيهات ذكي وتقارير Excel</h3>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed"">
                        إشعارات فورية بالفواتير الآجلة المستحقة والأصناف التي قارب مخزونها على النفاد والمسودات المعلقة، مع تصدير جميع الجداول إلى Excel / CSV.
                    </p>
                </div>
            </div>
        </div>
    </section>

    <!-- قسم استعراض شاشات البرنامج الحية (Interactive Screens Showcase) -->
    <section id=""live-screens"" class=""py-16 bg-[#F4F7F5] dark:bg-[#0F172A] border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""text-center max-w-3xl mx-auto mb-8"">
                <span class=""text-xs font-black px-3 py-1 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">جولة داخل النظام</span>
                <h2 class=""text-2xl sm:text-3xl font-black mt-3"">تعرّف على شاشات Invoify قبل التحميل</h2>
                <p class=""text-slate-600 dark:text-slate-300 mt-2 text-sm sm:text-base"">انقر على أي قسم بالأسفل لمعاينة تصميم الواجهة الفعلية ومكوناتها.</p>
            </div>

            <!-- أزرار التبديل بين الشاشات -->
            <div class=""flex flex-wrap justify-center gap-2 mb-8"" id=""screenTabsContainer"">
                <button type=""button"" data-screen=""invoices"" class=""screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-emeraldBrand-800 text-white shadow-sm transition"">🧾 شاشة إصدار الفواتير</button>
                <button type=""button"" data-screen=""products"" class=""screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition"">📦 المنتجات والوحدات الهرمية</button>
                <button type=""button"" data-screen=""customers"" class=""screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition"">👥 ملف العميل وكشف الحساب</button>
                <button type=""button"" data-screen=""currencies"" class=""screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition"">💱 العملات وأسعار الصرف</button>
                <button type=""button"" data-screen=""settings"" class=""screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition"">⚙️ الإعدادات والختم والنسخ الاحتياطي</button>
            </div>

            <!-- حاوية عرض الشاشة المختارة -->
            <div class=""bg-white dark:bg-[#1E293B] rounded-2xl border border-slate-200 dark:border-slate-700 shadow-xl p-5 sm:p-8"">
                <!-- 1. شاشة الفواتير -->
                <div id=""screen-panel-invoices"" class=""screen-panel space-y-5"">
                    <div class=""flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-700 pb-4"">
                        <div>
                            <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">شاشة إصدار الفواتير السريعة (InvoicesView)</h3>
                            <p class=""text-xs sm:text-sm text-slate-500 dark:text-slate-400"">إدخال سريع بالكيبورد (Enter)، تعديل مباشر داخل الجدول، وفصل وحدة البيع عن وحدة التسعير.</p>
                        </div>
                        <span class=""text-xs font-bold px-3 py-1 rounded-lg bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300"">حفظ ذري + فحص المخزون والائتمان</span>
                    </div>
                    <div class=""grid md:grid-cols-3 gap-4 text-xs"">
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800/70 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1 text-emeraldBrand-800 dark:text-emeraldBrand-400"">1. شريط بيانات العميل والدفع</div>
                            <p class=""text-slate-600 dark:text-slate-300 leading-relaxed"">بحث فوري عن العميل أو إضافة عميل جديد، اختيار عملة الفاتورة وسعر الصرف، وتحديد نوع الدفع (نقدي أو آجل مع تاريخ استحقاق).</p>
                        </div>
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800/70 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1 text-sky-700 dark:text-sky-400"">2. محرك الوحدات والتسعير المستقل</div>
                            <p class=""text-slate-600 dark:text-slate-300 leading-relaxed"">اختر البيع بـ ""صندوق"" وحدد السعر لكل ""كغ"" أو لكل ""طرد"" أو لكل ""صندوق"" بضغطة واحدة من البطاقات المصغرة تحت حقل السعر.</p>
                        </div>
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800/70 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1 text-amber-700 dark:text-amber-400"">3. الحفظ، المسودات، والطباعة</div>
                            <p class=""text-slate-600 dark:text-slate-300 leading-relaxed"">حفظ وطباعة فورية، أو حفظ كمسودة لاستكمالها لاحقاً، أو معاينة الطباعة وتصدير PDF وصورة PNG عالية الدقة لمشاركتها عبر واتساب.</p>
                        </div>
                    </div>
                </div>

                <!-- 2. شاشة المنتجات -->
                <div id=""screen-panel-products"" class=""screen-panel hidden space-y-5"">
                    <div class=""flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-700 pb-4"">
                        <div>
                            <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">إدارة المنتجات والوحدات المترابطة هرمياً (ProductsView)</h3>
                            <p class=""text-xs sm:text-sm text-slate-500 dark:text-slate-400"">بناء شجرة وحدات المنتج تلقائياً مع تحديد الوحدة الافتراضية للبيع ومتابعة المخزون بالكيلوغرام.</p>
                        </div>
                        <span class=""text-xs font-bold px-3 py-1 rounded-lg bg-sky-50 dark:bg-sky-900/50 text-sky-800 dark:text-sky-300"">1 كغ ⬅️ 1 طرد (10 كغ) ⬅️ 1 صندوق (100 كغ)</span>
                    </div>
                    <div class=""overflow-x-auto"">
                        <table class=""w-full text-xs text-center border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden"">
                            <thead class=""bg-[#E6F4EA] dark:bg-[#1E3A34] font-bold"">
                                <tr>
                                    <th class=""p-2.5"">الوحدة</th>
                                    <th class=""p-2.5"">تحتوي على</th>
                                    <th class=""p-2.5"">من الوحدة</th>
                                    <th class=""p-2.5"">المكافئ بالكغ (تلقائي)</th>
                                    <th class=""p-2.5"">سعر البيع ($)</th>
                                    <th class=""p-2.5"">الوحدة الافتراضية</th>
                                </tr>
                            </thead>
                            <tbody class=""divide-y divide-slate-200 dark:divide-slate-700 font-semibold"">
                                <tr>
                                    <td class=""p-2.5 font-bold"">كغ</td>
                                    <td class=""p-2.5"">1</td>
                                    <td class=""p-2.5"">كغ</td>
                                    <td class=""p-2.5 text-emeraldBrand-700 dark:text-emeraldBrand-400 font-bold"">1 كغ (أساسية)</td>
                                    <td class=""p-2.5 font-black"">$ 1.34</td>
                                    <td class=""p-2.5"">تعيين كافتراضية</td>
                                </tr>
                                <tr>
                                    <td class=""p-2.5 font-bold"">طرد</td>
                                    <td class=""p-2.5"">10</td>
                                    <td class=""p-2.5"">كغ</td>
                                    <td class=""p-2.5 text-emeraldBrand-700 dark:text-emeraldBrand-400 font-bold"">1 طرد = 10 كغ</td>
                                    <td class=""p-2.5 font-black"">$ 13.40</td>
                                    <td class=""p-2.5""><span class=""px-2 py-0.5 rounded bg-emeraldBrand-800 text-white text-[11px]"">⭐ افتراضية</span></td>
                                </tr>
                                <tr>
                                    <td class=""p-2.5 font-bold"">صندوق</td>
                                    <td class=""p-2.5"">10</td>
                                    <td class=""p-2.5"">طرد</td>
                                    <td class=""p-2.5 text-emeraldBrand-700 dark:text-emeraldBrand-400 font-bold"">1 صندوق = 10 طرد = 100 كغ</td>
                                    <td class=""p-2.5 font-black"">$ 134.00</td>
                                    <td class=""p-2.5"">تعيين كافتراضية</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <!-- 3. شاشة العملاء -->
                <div id=""screen-panel-customers"" class=""screen-panel hidden space-y-5"">
                    <div class=""flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-700 pb-4"">
                        <div>
                            <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">ملف العميل الشامل وكشف الحساب (CustomersView)</h3>
                            <p class=""text-xs sm:text-sm text-slate-500 dark:text-slate-400"">6 تبويبات متكاملة لكل عميل: معلومات الاتصال، الفواتير، الدفعات، كشف الحساب، المنتجات المسحوبة، والحد الائتماني.</p>
                        </div>
                        <span class=""text-xs font-bold px-3 py-1 rounded-lg bg-rose-50 dark:bg-rose-900/50 text-rose-700 dark:text-rose-300"">طباعة كشف حساب A4 + تصدير Excel</span>
                    </div>
                    <div class=""grid sm:grid-cols-3 gap-4 text-xs"">
                        <div class=""p-4 rounded-xl bg-emeraldBrand-50/70 dark:bg-emeraldBrand-950/40 border border-emeraldBrand-200 dark:border-emeraldBrand-800"">
                            <div class=""font-bold text-slate-500 dark:text-slate-400"">إجمالي المبيعات (المدين)</div>
                            <div class=""text-xl font-black text-slate-900 dark:text-white mt-1"">$ 4,250.00</div>
                            <div class=""text-[11px] text-emeraldBrand-700 dark:text-emeraldBrand-400 mt-1"">الفواتير النقدية مسددة فوراً ولا تزيد المديونية</div>
                        </div>
                        <div class=""p-4 rounded-xl bg-sky-50/70 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800"">
                            <div class=""font-bold text-slate-500 dark:text-slate-400"">إجمالي الدفعات والمرتجعات (الدائن)</div>
                            <div class=""text-xl font-black text-emeraldBrand-700 dark:text-emeraldBrand-400 mt-1"">$ 3,100.00</div>
                            <div class=""text-[11px] text-sky-700 dark:text-sky-400 mt-1"">سندات قبض + مرتجع مبيعات + حسم مكتسب</div>
                        </div>
                        <div class=""p-4 rounded-xl bg-rose-50/70 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800"">
                            <div class=""font-bold text-slate-500 dark:text-slate-400"">صافي الرصيد المستحق</div>
                            <div class=""text-xl font-black text-rose-600 dark:text-rose-400 mt-1"">$ 1,150.00</div>
                            <div class=""text-[11px] text-rose-700 dark:text-rose-300 mt-1"">مع دعم الرصيد المدوّر (BF-000) عند الفلترة بالتاريخ</div>
                        </div>
                    </div>
                </div>

                <!-- 4. شاشة العملات -->
                <div id=""screen-panel-currencies"" class=""screen-panel hidden space-y-5"">
                    <div class=""flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-700 pb-4"">
                        <div>
                            <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">إدارة العملات وسجل تغييرات الصرف (CurrenciesView)</h3>
                            <p class=""text-xs sm:text-sm text-slate-500 dark:text-slate-400"">تعديل مباشر لسعر الصرف مع حفظ التاريخ والوقت والمستخدم، وعدم تأثر الفواتير التاريخية السابقة.</p>
                        </div>
                        <span class=""text-xs font-bold px-3 py-1 rounded-lg bg-amber-50 dark:bg-amber-900/50 text-amber-800 dark:text-amber-300"">حاسبة تحويل عملات مدمجة</span>
                    </div>
                    <div class=""grid sm:grid-cols-2 gap-4 text-xs"">
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-2"">اتجاهان مرنان لسعر الصرف</div>
                            <p class=""text-slate-600 dark:text-slate-300"">يدعم النظام اتجاه (1 USD = 14,000 SYP) أو اتجاه (1 EUR = 1.10 USD) لتسعير أي عملة محلية أو أجنبية بسهولة تامة.</p>
                        </div>
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-2"">حماية الارتباط المحاسبي</div>
                            <p class=""text-slate-600 dark:text-slate-300"">يمنع النظام حذف أي عملة مرتبطة بفواتير أو حركات عملاء سابقة، ويعرض خيار تعطيلها للحفاظ على دقة التقارير التاريخية.</p>
                        </div>
                    </div>
                </div>

                <!-- 5. شاشة الإعدادات -->
                <div id=""screen-panel-settings"" class=""screen-panel hidden space-y-5"">
                    <div class=""flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-700 pb-4"">
                        <div>
                            <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">الإعدادات المتقدمة والختم والصيانة (SettingsView)</h3>
                            <p class=""text-xs sm:text-sm text-slate-500 dark:text-slate-400"">6 أقسام منظمة: هوية المؤسسة، الطباعة والختم، المالية، النسخ الاحتياطي، الصيانة، والمستخدمون.</p>
                        </div>
                        <span class=""text-xs font-bold px-3 py-1 rounded-lg bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300"">35 اختباراً ذاتياً مدمجاً</span>
                    </div>
                    <div class=""grid sm:grid-cols-3 gap-4 text-xs"">
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1"">🔏 تفريغ خلفية الختم الرسمي</div>
                            <p class=""text-slate-600 dark:text-slate-300"">ارفع صورة ختم شركتك حتى لو كانت بخلفيّة بيضاء، وسيقوم النظام بتحويل الخلفية إلى شفافة تلقائياً مع التحكم بالحجم والميلان والشفافية.</p>
                        </div>
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1"">🔢 التحكم برقم الفاتورة</div>
                            <p class=""text-slate-600 dark:text-slate-300"">عدّل رقم الفاتورة القادم في أي وقت أو صفّر سجل الفواتير لبدء سنة مالية جديدة مع أخذ نسخة احتياطية وقائية تلقائياً.</p>
                        </div>
                        <div class=""p-4 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"">
                            <div class=""font-black text-sm mb-1"">🛠️ الفحص الذاتي وضغط القاعدة</div>
                            <p class=""text-slate-600 dark:text-slate-300"">زر فحص سلامة قاعدة البيانات يشغّل 35 اختباراً محاسبياً وأمنياً آلياً، مع أداة ضغط القاعدة (VACUUM) وإصلاح السجلات.</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- قسم معاينة فاتورة الطباعة المزدوجة (A4 Landscape Dual Copy) -->
    <section id=""invoice-preview"" class=""py-16 bg-white dark:bg-[#1E293B] border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""grid lg:grid-cols-12 gap-8 items-center"">
                <div class=""lg:col-span-5 space-y-4"">
                    <span class=""text-xs font-black px-3 py-1 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">ميزة حصرية في Invoify</span>
                    <h2 class=""text-2xl sm:text-3xl font-black"">طباعة نسختين (عميل + إدارة) في ورقة A4 واحدة</h2>
                    <p class=""text-sm sm:text-base text-slate-600 dark:text-slate-300 leading-relaxed"">
                        صُمم قالب الطباعة بدقة متجهية (1200 DPI Vector) ليطبع نسخة العميل على اليمين ونسخة الإدارة على اليسار مع خط قص أوسط، مما يوفر 50% من استهلاك الورق.
                    </p>
                    <ul class=""space-y-2.5 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200"">
                        <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>خيار إخفاء الأسعار ورمز QR عن نسخة العميل فقط (لإرسالها كإشعار استلام مستودعي) مع بقاء الأسعار كاملة في نسخة الإدارة.</span></li>
                        <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>رمز QR ذكي يتضمن بيانات الشركة، رقم الفاتورة، العميل، الأصناف، الأوزان، والصافي المطلوب.</span></li>
                        <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>طبقة الختم الرسمي المفرغ الشفاف فوق جدول الأصناف دون تغطية الأرقام.</span></li>
                    </ul>
                </div>

                <!-- تمثيل مرئي لورقة A4 المزدوجة -->
                <div class=""lg:col-span-7"">
                    <div class=""bg-slate-100 dark:bg-slate-900 p-4 rounded-2xl border border-slate-300 dark:border-slate-700 shadow-inner"">
                        <div class=""grid grid-cols-11 gap-2 bg-white text-slate-900 p-4 rounded-xl shadow border border-slate-200 text-[10px]"">
                            <!-- نسخة العميل -->
                            <div class=""col-span-5 border border-emeraldBrand-200 rounded-lg p-2.5 relative"">
                                <div class=""flex justify-between items-start border-b border-emeraldBrand-100 pb-2 mb-2"">
                                    <div>
                                        <div class=""font-black text-emeraldBrand-900 text-xs"">شركة الحلويات الفاخرة</div>
                                        <div class=""text-[9px] text-slate-500"">نسخة العميل • نقدي</div>
                                    </div>
                                    <div class=""px-1.5 py-0.5 rounded bg-emeraldBrand-900 text-white font-bold text-[8px]"">🛡️ موثقة ومعتمدة</div>
                                </div>
                                <div class=""bg-emeraldBrand-900 text-white font-bold grid grid-cols-4 text-center py-1 rounded-t"">
                                    <span>البيان</span><span>العدد</span><span>الوزن</span><span>الإجمالي</span>
                                </div>
                                <div class=""divide-y divide-slate-100 text-center py-1"">
                                    <div class=""grid grid-cols-4 py-1""><span>راحة مكسرات</span><span>3 صندوق</span><span>150 Kg</span><span class=""font-bold"">$450</span></div>
                                    <div class=""grid grid-cols-4 py-1 bg-emeraldBrand-50/40""><span>نوغا فستق</span><span>4 طرد</span><span>40 Kg</span><span class=""font-bold"">$340</span></div>
                                </div>
                                <!-- ختم مفرغ شفاف -->
                                <div class=""mt-3 pt-2 border-t border-emeraldBrand-100 flex justify-between items-center font-bold text-emeraldBrand-900"">
                                    <span>الوزن: 190 Kg</span>
                                    <span>الصافي: $ 790.00</span>
                                </div>
                            </div>

                            <!-- خط القص -->
                            <div class=""col-span-1 flex flex-col items-center justify-center text-slate-400"">
                                <div class=""h-full border-r border-dashed border-slate-400""></div>
                                <span class=""text-xs my-1"">✂️</span>
                                <div class=""h-full border-r border-dashed border-slate-400""></div>
                            </div>

                            <!-- نسخة الإدارة -->
                            <div class=""col-span-5 border border-emeraldBrand-200 rounded-lg p-2.5"">
                                <div class=""flex justify-between items-start border-b border-emeraldBrand-100 pb-2 mb-2"">
                                    <div>
                                        <div class=""font-black text-emeraldBrand-900 text-xs"">شركة الحلويات الفاخرة</div>
                                        <div class=""text-[9px] text-slate-500"">نسخة الإدارة • N° 0042</div>
                                    </div>
                                    <div class=""px-1.5 py-0.5 rounded bg-emeraldBrand-900 text-white font-bold text-[8px]"">🛡️ موثقة ومعتمدة</div>
                                </div>
                                <div class=""bg-emeraldBrand-900 text-white font-bold grid grid-cols-4 text-center py-1 rounded-t"">
                                    <span>البيان</span><span>العدد</span><span>الوزن</span><span>الإجمالي</span>
                                </div>
                                <div class=""divide-y divide-slate-100 text-center py-1"">
                                    <div class=""grid grid-cols-4 py-1""><span>راحة مكسرات</span><span>3 صندوق</span><span>150 Kg</span><span class=""font-bold"">$450</span></div>
                                    <div class=""grid grid-cols-4 py-1 bg-emeraldBrand-50/40""><span>نوغا فستق</span><span>4 طرد</span><span>40 Kg</span><span class=""font-bold"">$340</span></div>
                                </div>
                                <div class=""mt-3 pt-2 border-t border-emeraldBrand-100 flex justify-between items-center font-bold text-emeraldBrand-900"">
                                    <span>الوزن: 190 Kg</span>
                                    <span>الصافي: $ 790.00</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- قسم التحميل والتجربة المجانية والترخيص -->
    <section id=""pricing"" class=""py-16 bg-[#F4F7F5] dark:bg-[#0F172A] border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"" id=""download-section"">
            <div class=""text-center max-w-3xl mx-auto mb-12"">
                <span class=""text-xs font-black px-3 py-1 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">ابدأ مجاناً اليوم</span>
                <h2 class=""text-2xl sm:text-3xl font-black mt-3"">حمّل النسخة التجريبية مجاناً لمدة 30 يوماً</h2>
                <p class=""text-slate-600 dark:text-slate-300 mt-2 text-sm sm:text-base"">جرّب كافة ميزات البرنامج بدون أي قيود لمدة شهر كامل. وعند انتهاء الفترة التجريبية تبقى جميع فواتيرك وبياناتك محفوظة وتستكمل عملك فور إدخال مفتاح التفعيل.</p>
            </div>

            <div class=""grid md:grid-cols-2 gap-8 max-w-5xl mx-auto"">
                <!-- بطاقة النسخة التجريبية المجانية -->
                <div class=""bg-white dark:bg-[#1E293B] rounded-2xl border-2 border-slate-200 dark:border-slate-700 p-7 flex flex-col justify-between shadow-sm"">
                    <div>
                        <div class=""flex items-center justify-between mb-4"">
                            <span class=""px-3 py-1 rounded-full text-xs font-black bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300"">تجربة مجانية</span>
                            <span class=""text-xs font-bold text-slate-400"">Windows 10 / 11 (64-bit)</span>
                        </div>
                        <h3 class=""text-xl font-black"">النسخة التجريبية الكاملة (30 يوماً)</h3>
                        <div class=""mt-3 flex items-baseline gap-2"">
                            <span class=""text-3xl font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">مجاناً</span>
                            <span class=""text-xs text-slate-500 dark:text-slate-400 font-semibold"">/ لمدة 30 يوماً من تاريخ التثبيت</span>
                        </div>

                        <ul class=""mt-6 space-y-3 text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-300"">
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>جميع الميزات مفتوحة بالكامل (فواتير، منتجات، عملاء، عملات، طباعة).</span></li>
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>لا يتطلب بطاقة ائتمان أو اشتراك مسبق.</span></li>
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>جميع البيانات التي تدخلها خلال الـ 30 يوماً تبقى محفوظة عند التفعيل.</span></li>
                        </ul>
                    </div>

                    <div class=""mt-8 pt-4 border-t border-slate-100 dark:border-slate-700"">
                        <button type=""button"" id=""cardDownloadBtn"" class=""w-full py-3.5 rounded-xl bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-black text-sm shadow transition flex items-center justify-center gap-2"">
                            <span>⬇️</span>
                            <span>تحميل برنامج Invoify للكمبيوتر (v2.6)</span>
                        </button>
                        <p class=""text-[11px] text-center text-slate-400 mt-2"">رابط تحميل مباشر وآمن • يعمل فوراً على نظام Windows</p>
                    </div>
                </div>

                <!-- بطاقة الترخيص الكامل المدفوع -->
                <div class=""bg-white dark:bg-[#1E293B] rounded-2xl border-2 border-emeraldBrand-600 p-7 flex flex-col justify-between shadow-xl relative"">
                    <div class=""absolute -top-3.5 left-6 px-3 py-1 rounded-full bg-emeraldBrand-700 text-white text-xs font-black shadow"">النسخة التجارية المعتمدة</div>
                    <div>
                        <div class=""flex items-center justify-between mb-4"">
                            <span class=""px-3 py-1 rounded-full text-xs font-black bg-amber-50 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300"">ترخيص دائم</span>
                            <span class=""text-xs font-bold text-emeraldBrand-700 dark:text-emeraldBrand-400"">تفعيل فوري بمفتاح الرقم التسلسلي</span>
                        </div>
                        <h3 class=""text-xl font-black"">الترخيص الكامل غير المحدود</h3>
                        <div class=""mt-3 flex items-baseline gap-2"">
                            <span class=""text-3xl font-black text-slate-900 dark:text-white"">ترخيص مدى الحياة</span>
                            <span class=""text-xs text-slate-500 dark:text-slate-400 font-semibold"">/ دفعة واحدة بدون اشتراك شهري</span>
                        </div>

                        <ul class=""mt-6 space-y-3 text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-300"">
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>مفتاح تفعيل رقمي رسمي مرتبط بجهازك يفتح البرنامج مدى الحياة.</span></li>
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>عدد غير محدود من الفواتير، الأصناف، العملاء، والمعامل المستقلة.</span></li>
                            <li class=""flex items-center gap-2""><span class=""text-emeraldBrand-600"">✔</span><span>دعم فني مباشر ومساعدة في إعداد شعار وختم المؤسسة وتصميم الفاتورة.</span></li>
                        </ul>
                    </div>

                    <div class=""mt-8 pt-4 border-t border-slate-100 dark:border-slate-700"">
                        <button type=""button"" id=""cardBuyLicenseBtn"" class=""w-full py-3.5 rounded-xl bg-slate-900 dark:bg-emeraldBrand-600 hover:bg-slate-800 dark:hover:bg-emeraldBrand-500 text-white font-black text-sm shadow transition flex items-center justify-center gap-2"">
                            <span>🔑</span>
                            <span>طلب شراء مفتاح الترخيص الآن</span>
                        </button>
                        <p class=""text-[11px] text-center text-slate-400 mt-2"">تواصل مباشر لاستلام مفتاح التفعيل خلال دقائق</p>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- قسم التوسع المستقبلي (حزمة البرامج والمنتجات القادمة) -->
    <section id=""suite"" class=""py-16 bg-white dark:bg-[#1E293B] border-b border-slate-200 dark:border-slate-800"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""flex flex-wrap items-end justify-between gap-4 mb-10"">
                <div>
                    <span class=""text-xs font-black px-3 py-1 rounded-full bg-emeraldBrand-50 dark:bg-emeraldBrand-900/50 text-emeraldBrand-800 dark:text-emeraldBrand-300 border border-emeraldBrand-200 dark:border-emeraldBrand-700"">منظومة حلول متكاملة</span>
                    <h2 class=""text-2xl sm:text-3xl font-black mt-3"">منتجاتنا البرمجية</h2>
                    <p class=""text-slate-600 dark:text-slate-300 mt-1 text-sm"">بنية الموقع مجهزة لإضافة وإطلاق إصدارات وبرامج مكتبية جديدة بجانب Invoify.</p>
                </div>
            </div>

            <div class=""grid md:grid-cols-3 gap-6"">
                <!-- المنتج الحالي -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border-2 border-emeraldBrand-600 flex flex-col justify-between"">
                    <div>
                        <div class=""flex items-center justify-between mb-3"">
                            <span class=""px-2.5 py-0.5 rounded-full text-[11px] font-black bg-emeraldBrand-800 text-white"">متاح للتحميل الآن</span>
                            <span class=""text-xs font-bold text-slate-400"">v2.6</span>
                        </div>
                        <h3 class=""text-lg font-black text-emeraldBrand-800 dark:text-emeraldBrand-400"">Invoify — إدارة الفواتير والمبيعات</h3>
                        <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-2 leading-relaxed"">النظام المتكامل لإصدار الفواتير، إدارة المستودع بالوحدات الهرمية، وكشوف حسابات العملاء متعددة العملات.</p>
                    </div>
                    <a href=""#download-section"" class=""mt-5 inline-block text-center py-2.5 rounded-lg bg-emeraldBrand-800 text-white font-bold text-xs hover:bg-emeraldBrand-700 transition"">تحميل النسخة التجريبية</a>
                </div>

                <!-- منتج مستقبلي 1 (جاهز للتوسع) -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 flex flex-col justify-between opacity-90"">
                    <div>
                        <div class=""flex items-center justify-between mb-3"">
                            <span class=""px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300"">قريباً</span>
                            <span class=""text-xs font-bold text-slate-400"">المنتج القادم</span>
                        </div>
                        <h3 class=""text-lg font-black"">نظام تصميم وطباعة اللصاقات التجارية</h3>
                        <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-2 leading-relaxed"">تصميم وطباعة لصاقات المنتجات الغذائية والباركود وتواريخ الصلاحية بدقة عالية.</p>
                    </div>
                    <button type=""button"" data-notify-product=""نظام اللصاقات التجارية"" class=""notify-product-btn mt-5 py-2.5 rounded-lg border border-slate-300 dark:border-slate-600 font-bold text-xs hover:border-emeraldBrand-500 transition"">أعلمني عند الإطلاق</button>
                </div>

                <!-- منتج مستقبلي 2 (جاهز للتوسع) -->
                <div class=""p-6 rounded-2xl bg-[#F8FAF9] dark:bg-[#0F172A] border border-slate-200 dark:border-slate-700 flex flex-col justify-between opacity-90"">
                    <div>
                        <div class=""flex items-center justify-between mb-3"">
                            <span class=""px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-sky-100 dark:bg-sky-900/40 text-sky-800 dark:text-sky-300"">قيد التطوير</span>
                            <span class=""text-xs font-bold text-slate-400"">إصدار مستقبلي</span>
                        </div>
                        <h3 class=""text-lg font-black"">نظام المحاسبة والتصنيع للمعامل</h3>
                        <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-2 leading-relaxed"">حساب تكاليف المواد الأولية وخلطات الإنتاج والربط المباشر مع مستودع البضاعة الجاهزة.</p>
                    </div>
                    <button type=""button"" data-notify-product=""نظام المحاسبة والتصنيع"" class=""notify-product-btn mt-5 py-2.5 rounded-lg border border-slate-300 dark:border-slate-600 font-bold text-xs hover:border-emeraldBrand-500 transition"">أعلمني عند الإطلاق</button>
                </div>
            </div>
        </div>
    </section>

    <!-- الأسئلة الشائعة (FAQ) -->
    <section id=""faq"" class=""py-16 bg-[#F4F7F5] dark:bg-[#0F172A]"">
        <div class=""max-w-4xl mx-auto px-4 sm:px-6 lg:px-8"">
            <div class=""text-center mb-10"">
                <h2 class=""text-2xl sm:text-3xl font-black"">الأسئلة الشائعة</h2>
                <p class=""text-slate-600 dark:text-slate-300 mt-2 text-sm"">إجابات واضحة حول تشغيل البرنامج والتجربة المجانية والتفعيل.</p>
            </div>

            <div class=""space-y-3"">
                <details class=""group bg-white dark:bg-[#1E293B] rounded-xl border border-slate-200 dark:border-slate-700 p-4"">
                    <summary class=""font-bold text-sm sm:text-base cursor-pointer list-none flex items-center justify-between"">
                        <span>هل يحتاج البرنامج إلى اتصال بالإنترنت ليعمل؟</span>
                        <span class=""text-emeraldBrand-600 group-open:rotate-180 transition"">▼</span>
                    </summary>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-3 leading-relaxed"">لا، يعمل برنامج Invoify محلياً بنسبة 100% على جهاز الكمبيوتر الخاص بك دون الحاجة لأي اتصال بالإنترنت، وتُحفظ قاعدة البيانات والنسخ الاحتياطية على جهازك بأمان تام.</p>
                </details>

                <details class=""group bg-white dark:bg-[#1E293B] rounded-xl border border-slate-200 dark:border-slate-700 p-4"">
                    <summary class=""font-bold text-sm sm:text-base cursor-pointer list-none flex items-center justify-between"">
                        <span>ماذا يحدث لبياناتي وفواتيري بعد انتهاء فترة التجربة المجانية (30 يوماً)؟</span>
                        <span class=""text-emeraldBrand-600 group-open:rotate-180 transition"">▼</span>
                    </summary>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-3 leading-relaxed"">جميع الفواتير والمنتجات والعملاء الذين قمت بإدخالهم خلال فترة الـ 30 يوماً تبقى محفوظة بالكامل على جهازك. بمجرد شراء وإدخال مفتاح التفعيل، يفتح البرنامج فوراً وتتابع عملك من حيث توقفت دون ضياع أي معلومة.</p>
                </details>

                <details class=""group bg-white dark:bg-[#1E293B] rounded-xl border border-slate-200 dark:border-slate-700 p-4"">
                    <summary class=""font-bold text-sm sm:text-base cursor-pointer list-none flex items-center justify-between"">
                        <span>ما هي متطلبات تشغيل البرنامج على الكمبيوتر؟</span>
                        <span class=""text-emeraldBrand-600 group-open:rotate-180 transition"">▼</span>
                    </summary>
                    <p class=""text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-3 leading-relaxed"">يعمل البرنامج على أي جهاز كمبيوتر أو لابتوب بنظام Windows 10 أو Windows 11 (64-bit)، ولا يتطلب مواصفات عالية، ويدعم جميع الطابعات العادية وطابعات الليزر A4/A5.</p>
                </details>
            </div>
        </div>
    </section>

    <!-- التذييل السفلي (Footer) -->
    <footer class=""bg-[#032E22] text-emeraldBrand-100 py-10 border-t border-emeraldBrand-900 text-xs sm:text-sm"">
        <div class=""max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-4"">
            <div class=""flex items-center gap-3"">
                <span class=""font-black text-white text-lg"">Invoify</span>
                <span class=""text-emeraldBrand-400"">•</span>
                <span class=""text-emeraldBrand-200"">نظام إدارة فواتير المبيعات والمستودعات</span>
            </div>
            <div class=""text-emeraldBrand-300"">
                جميع الحقوق محفوظة &copy; 2026 — صُمم بعناية لقطاع الأعمال والتجارة.
            </div>
        </div>
    </footer>

    <!-- نافذة التحميل المنبثقة (Download Modal) -->
    <div id=""downloadModal"" class=""fixed inset-0 z-50 hidden bg-slate-900/70 backdrop-blur-sm flex items-center justify-center p-4"">
        <div class=""bg-white dark:bg-[#1E293B] rounded-2xl max-w-lg w-full p-6 border border-slate-200 dark:border-slate-700 shadow-2xl space-y-4"">
            <div class=""flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-3"">
                <h3 class=""font-black text-lg text-emeraldBrand-800 dark:text-emeraldBrand-400"">⬇️ تحميل النسخة التجريبية (30 يوماً)</h3>
                <button type=""button"" id=""closeDownloadModalBtn"" class=""w-8 h-8 rounded-lg bg-slate-100 dark:bg-slate-800 font-bold"">✕</button>
            </div>
            <div class=""space-y-3 text-xs sm:text-sm text-slate-600 dark:text-slate-300"">
                <p class=""font-bold text-slate-900 dark:text-white"">تفاصيل ملف التثبيت:</p>
                <div class=""bg-slate-50 dark:bg-slate-800 p-3.5 rounded-xl border border-slate-200 dark:border-slate-700 space-y-1.5 font-semibold"">
                    <div>📦 الإصدار: <span class=""font-black text-emeraldBrand-700 dark:text-emeraldBrand-400"">Invoify v2.6 (Windows 64-bit)</span></div>
                    <div>⏱️ مدة التجربة: <span class=""font-bold"">30 يوماً كاملة الصلاحيات</span></div>
                    <div>🔒 الفحص الأمني: <span class=""font-bold"">خالٍ من الفيروسات ويعمل بدون إنترنت</span></div>
                </div>
                <p class=""text-xs text-slate-500 dark:text-slate-400"">ملاحظة: بعد فك الضغط وتشغيل ملف <code class=""px-1.5 py-0.5 bg-slate-200 dark:bg-slate-700 rounded"">SalesInvoiceManager.exe</code>، سيطلب منك البرنامج في المرة الأولى فقط اختيار اسم مستخدم وكلمة سر لحساب المدير الخاص بك.</p>
            </div>
            <div class=""flex items-center justify-end gap-2 pt-2"">
                <a id=""directDownloadLink"" href=""https://github.com/YOUR-USERNAME/invoify-releases/releases/latest/download/Invoify_Setup.zip"" target=""_blank"" rel=""noopener noreferrer"" class=""w-full text-center py-3 rounded-xl bg-emeraldBrand-800 hover:bg-emeraldBrand-700 text-white font-black text-sm shadow transition"">
                    بدء التحميل المباشر الآن (Invoify_Setup.zip)
                </a>
            </div>
        </div>
    </div>

    <!-- نافذة شراء وتفعيل الترخيص (License Purchase Modal) -->
    <div id=""licenseModal"" class=""fixed inset-0 z-50 hidden bg-slate-900/70 backdrop-blur-sm flex items-center justify-center p-4"">
        <div class=""bg-white dark:bg-[#1E293B] rounded-2xl max-w-lg w-full p-6 border border-slate-200 dark:border-slate-700 shadow-2xl space-y-4"">
            <div class=""flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-3"">
                <h3 class=""font-black text-lg text-emeraldBrand-800 dark:text-emeraldBrand-400"">🔑 شراء وتفعيل ترخيص Invoify</h3>
                <button type=""button"" id=""closeLicenseModalBtn"" class=""w-8 h-8 rounded-lg bg-slate-100 dark:bg-slate-800 font-bold"">✕</button>
            </div>
            <div class=""space-y-3 text-xs sm:text-sm text-slate-600 dark:text-slate-300"">
                <p>لتفعيل البرنامج مدى الحياة بعد انتهاء فترة الـ 30 يوماً التجريبية، اتبع الخطوات الثلاث البسيطة:</p>
                <ol class=""list-decimal list-inside space-y-2 bg-slate-50 dark:bg-slate-800 p-3.5 rounded-xl border border-slate-200 dark:border-slate-700 font-semibold"">
                    <li>افتح نافذة <strong>تفعيل الترخيص</strong> داخل برنامج Invoify على جهازك.</li>
                    <li>انسخ <strong>معرّف الجهاز (Machine ID)</strong> الظاهر أمامك في الشاشة.</li>
                    <li>أرسله لنا عبر واتساب لاستلام <strong>مفتاح التفعيل الرسمي (License Key)</strong> الخاص بجهازك فوراً.</li>
                </ol>
                <div class=""pt-1"">
                    <label for=""customerMachineIdInput"" class=""block text-xs font-bold mb-1"">أدخل معرّف الجهاز (اختياري لتجهيز رسالة الواتساب تلقائياً):</label>
                    <input id=""customerMachineIdInput"" type=""text"" placeholder=""مثال: INV-8F4A-2026-XXXX"" dir=""ltr"" class=""w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 text-center font-mono text-sm"">
                </div>
            </div>
            <div class=""flex flex-col sm:flex-row items-center gap-2 pt-2"">
                <button type=""button"" id=""sendWhatsappLicenseBtn"" class=""w-full py-3 rounded-xl bg-emeraldBrand-700 hover:bg-emeraldBrand-600 text-white font-black text-sm shadow transition flex items-center justify-center gap-2"">
                    <span>💬</span>
                    <span>إرسال طلب الترخيص عبر واتساب</span>
                </button>
            </div>
        </div>
    </div>

    <!-- إشعار منبثق (Toast) -->
    <div id=""toastBanner"" class=""fixed bottom-5 left-5 z-50 hidden bg-emeraldBrand-900 text-white text-xs sm:text-sm font-bold px-4 py-3 rounded-xl shadow-lg border border-emeraldBrand-600"">
        <span id=""toastMessage"">تمت العملية بنجاح</span>
    </div>

    <script>
        (function () {
            // 1. تبديل الوضع الليلي والنهاري
            const themeToggleBtn = document.getElementById('themeToggleBtn');
            const themeIcon = document.getElementById('themeIcon');
            const htmlEl = document.documentElement;

            function applyTheme(isDark) {
                if (isDark) {
                    htmlEl.classList.add('dark');
                    themeIcon.textContent = '☀️';
                } else {
                    htmlEl.classList.remove('dark');
                    themeIcon.textContent = '🌙';
                }
                try {
                    localStorage.setItem('invoify_web_dark', isDark ? '1' : '0');
                } catch (e) {}
            }

            try {
                if (localStorage.getItem('invoify_web_dark') === '1') {
                    applyTheme(true);
                }
            } catch (e) {}

            themeToggleBtn.addEventListener('click', function () {
                const isDarkNow = !htmlEl.classList.contains('dark');
                applyTheme(isDarkNow);
            });

            // 2. التبديل بين شاشات البرنامج في قسم المعاينة
            const tabButtons = document.querySelectorAll('.screen-tab-btn');
            const panels = {
                invoices: document.getElementById('screen-panel-invoices'),
                products: document.getElementById('screen-panel-products'),
                customers: document.getElementById('screen-panel-customers'),
                currencies: document.getElementById('screen-panel-currencies'),
                settings: document.getElementById('screen-panel-settings')
            };

            tabButtons.forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const target = btn.getAttribute('data-screen');
                    tabButtons.forEach(function (b) {
                        b.className = 'screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition';
                    });
                    btn.className = 'screen-tab-btn px-4 py-2.5 rounded-xl font-bold text-xs sm:text-sm bg-emeraldBrand-800 text-white shadow-sm transition';

                    Object.keys(panels).forEach(function (k) {
                        if (panels[k]) {
                            panels[k].classList.toggle('hidden', k !== target);
                        }
                    });
                });
            });

            // 3. النوافذ المنبثقة للتحميل وشراء الترخيص
            const downloadModal = document.getElementById('downloadModal');
            const licenseModal = document.getElementById('licenseModal');

            function openDownloadModal() {
                downloadModal.classList.remove('hidden');
            }
            function closeDownloadModal() {
                downloadModal.classList.add('hidden');
            }
            function openLicenseModal() {
                licenseModal.classList.remove('hidden');
            }
            function closeLicenseModal() {
                licenseModal.classList.add('hidden');
            }

            document.getElementById('heroDownloadBtn').addEventListener('click', openDownloadModal);
            document.getElementById('cardDownloadBtn').addEventListener('click', openDownloadModal);
            document.getElementById('closeDownloadModalBtn').addEventListener('click', closeDownloadModal);

            document.getElementById('heroBuyLicenseBtn').addEventListener('click', openLicenseModal);
            document.getElementById('cardBuyLicenseBtn').addEventListener('click', openLicenseModal);
            document.getElementById('closeLicenseModalBtn').addEventListener('click', closeLicenseModal);

            document.getElementById('topProductsTrigger').addEventListener('click', function () {
                const suiteEl = document.getElementById('suite');
                if (suiteEl) suiteEl.scrollIntoView({ behavior: 'smooth' });
            });

            // 4. إرسال طلب الترخيص عبر واتساب
            const toastBanner = document.getElementById('toastBanner');
            const toastMessage = document.getElementById('toastMessage');

            function showToast(msg) {
                toastMessage.textContent = msg;
                toastBanner.classList.remove('hidden');
                setTimeout(function () {
                    toastBanner.classList.add('hidden');
                }, 3500);
            }

            document.getElementById('sendWhatsappLicenseBtn').addEventListener('click', function () {
                const machineId = (document.getElementById('customerMachineIdInput').value || '').trim();
                const text = machineId
                    ? 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج Invoify. معرف الجهاز الخاص بي هو: ' + machineId
                    : 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج إدارة الفواتير Invoify.';
                const url = 'https://wa.me/?text=' + encodeURIComponent(text);
                window.open(url, '_blank', 'noopener,noreferrer');
                closeLicenseModal();
                showToast('تم تجهيز رسالة طلب الترخيص بنجاح.');
            });

            // 5. أزرار التنبيه بالمنتجات المستقبلية
            document.querySelectorAll('.notify-product-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const pName = btn.getAttribute('data-notify-product') || 'المنتج';
                    showToast('تم تسجيل اهتمامك بـ (' + pName + ') — سنخبرك فور إطلاقه!');
                });
            });
        })();
    </script>
</body>
</html>";
}