// File: wwwroot/js/app.js
(function () {
    'use strict';

    if (window.__invoifyAppInitialized) {
        return;
    }
    window.__invoifyAppInitialized = true;

    // 1. تبديل الوضع الليلي والنهاري
    var themeToggleBtn = document.getElementById('themeToggleBtn');
    var themeIcon = document.getElementById('themeIcon');
    var htmlEl = document.documentElement;

    function applyTheme(isDark) {
        if (isDark) {
            htmlEl.classList.add('dark');
            if (themeIcon) themeIcon.textContent = '☀️';
        } else {
            htmlEl.classList.remove('dark');
            if (themeIcon) themeIcon.textContent = '🌙';
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

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            var isDarkNow = !htmlEl.classList.contains('dark');
            applyTheme(isDarkNow);
        });
    }

    // 2. التبديل بين شاشات البرنامج في قسم المعاينة
    var tabButtons = document.querySelectorAll('.screen-tab-btn');
    var panels = {
        invoices: document.getElementById('screen-panel-invoices'),
        products: document.getElementById('screen-panel-products'),
        customers: document.getElementById('screen-panel-customers'),
        currencies: document.getElementById('screen-panel-currencies'),
        settings: document.getElementById('screen-panel-settings')
    };

    tabButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = btn.getAttribute('data-screen');
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
    var downloadModal = document.getElementById('downloadModal');
    var licenseModal = document.getElementById('licenseModal');

    function openDownloadModal() {
        if (downloadModal) downloadModal.classList.remove('hidden');
    }
    function closeDownloadModal() {
        if (downloadModal) downloadModal.classList.add('hidden');
    }
    function openLicenseModal() {
        if (licenseModal) licenseModal.classList.remove('hidden');
    }
    function closeLicenseModal() {
        if (licenseModal) licenseModal.classList.add('hidden');
    }

    var heroDownloadBtn = document.getElementById('heroDownloadBtn');
    var cardDownloadBtn = document.getElementById('cardDownloadBtn');
    var closeDownloadModalBtn = document.getElementById('closeDownloadModalBtn');

    if (heroDownloadBtn) heroDownloadBtn.addEventListener('click', openDownloadModal);
    if (cardDownloadBtn) cardDownloadBtn.addEventListener('click', openDownloadModal);
    if (closeDownloadModalBtn) closeDownloadModalBtn.addEventListener('click', closeDownloadModal);

    var heroBuyLicenseBtn = document.getElementById('heroBuyLicenseBtn');
    var cardBuyLicenseBtn = document.getElementById('cardBuyLicenseBtn');
    var closeLicenseModalBtn = document.getElementById('closeLicenseModalBtn');

    if (heroBuyLicenseBtn) heroBuyLicenseBtn.addEventListener('click', openLicenseModal);
    if (cardBuyLicenseBtn) cardBuyLicenseBtn.addEventListener('click', openLicenseModal);
    if (closeLicenseModalBtn) closeLicenseModalBtn.addEventListener('click', closeLicenseModal);

    if (downloadModal) {
        downloadModal.addEventListener('click', function (e) {
            if (e.target === downloadModal) closeDownloadModal();
        });
    }
    if (licenseModal) {
        licenseModal.addEventListener('click', function (e) {
            if (e.target === licenseModal) closeLicenseModal();
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeDownloadModal();
            closeLicenseModal();
        }
    });

    var topProductsTrigger = document.getElementById('topProductsTrigger');
    if (topProductsTrigger) {
        topProductsTrigger.addEventListener('click', function () {
            var suiteEl = document.getElementById('suite');
            if (suiteEl) suiteEl.scrollIntoView({ behavior: 'smooth' });
        });
    }

    // 4. إرسال طلب الترخيص عبر واتساب
    var toastBanner = document.getElementById('toastBanner');
    var toastMessage = document.getElementById('toastMessage');
    var toastTimer = null;

    function isEnglishActive() {
        return htmlEl.getAttribute('lang') === 'en';
    }

    function showToast(msgAr, msgEn) {
        if (!toastBanner || !toastMessage) return;
        toastMessage.textContent = (isEnglishActive() && msgEn) ? msgEn : msgAr;
        toastBanner.classList.remove('hidden');
        if (toastTimer) clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            toastBanner.classList.add('hidden');
        }, 3500);
    }

    var sendWhatsappLicenseBtn = document.getElementById('sendWhatsappLicenseBtn');
    if (sendWhatsappLicenseBtn) {
        sendWhatsappLicenseBtn.addEventListener('click', function () {
            var inputEl = document.getElementById('customerMachineIdInput');
            var machineId = inputEl ? (inputEl.value || '').trim() : '';
            var text;
            if (isEnglishActive()) {
                text = machineId
                    ? 'Hello, I would like to purchase a lifetime activation key for Invoify. My Machine ID is: ' + machineId
                    : 'Hello, I would like to purchase a lifetime activation key for the Invoify invoicing software.';
            } else {
                text = machineId
                    ? 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج Invoify. معرف الجهاز الخاص بي هو: ' + machineId
                    : 'مرحباً، أرغب بشراء مفتاح تفعيل دائم لبرنامج إدارة الفواتير Invoify.';
            }
            var url = 'https://wa.me/?text=' + encodeURIComponent(text);
            window.open(url, '_blank', 'noopener,noreferrer');
            closeLicenseModal();
            showToast('تم تجهيز رسالة طلب الترخيص بنجاح.', 'License request message prepared successfully.');
        });
    }

    // 5. أزرار التنبيه بالمنتجات المستقبلية
    document.querySelectorAll('.notify-product-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var pName = btn.getAttribute('data-notify-product') || (isEnglishActive() ? 'Product' : 'المنتج');
            showToast(
                'تم تسجيل اهتمامك بـ (' + pName + ') — سنخبرك فور إطلاقه!',
                'Your interest in (' + pName + ') has been registered — we will notify you on launch!'
            );
        });
    });
})();