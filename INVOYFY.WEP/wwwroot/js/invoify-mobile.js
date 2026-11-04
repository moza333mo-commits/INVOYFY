/* File: wwwroot/js/invoify-mobile.js */
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
        var linkOrBtn = e.target.closest('a, button, [role="button"], li');
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
        btn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><line x1="3" y1="6" x2="21" y2="6"></line><line x1="3" y1="12" x2="21" y2="12"></line><line x1="3" y1="18" x2="21" y2="18"></line></svg>';
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
