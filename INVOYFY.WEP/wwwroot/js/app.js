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