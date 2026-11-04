/* File: wwwroot/js/invoify-mobile.js */
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

  var trialRegexWithDuration = /لمدة\s*(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?/g;
  var trialRegexGeneral = /(?:30|٣٠)\s*يوم(?:اً|ًا)?(?:\s*فقط)?/g;

  // قاموس العبارات والمصطلحات المحاسبية والتسويقية الدقيق
  var exactTranslations = [
    ['نظام إدارة فواتير المبيعات والمستودعات والعملاء', 'Sales Invoicing, Inventory & Customer Management ERP'],
    ['نظام إدارة المبيعات والفواتير', 'Sales & Invoicing ERP System'],
    ['إدارة فواتيرك ومبيعاتك بكل سهولة واحترافية', 'Manage your invoices and sales with ease and precision'],
    ['مرحباً بك في Invoify', 'Welcome to Invoify'],
    ['مرحبا بك في Invoify', 'Welcome to Invoify'],
    ['تحميل النسخة التجريبية مجاناً', 'Download Free Trial'],
    ['تحميل النسخة التجريبية', 'Download Free Trial'],
    ['تنزيل النسخة التجريبية', 'Download Free Trial'],
    ['النسخة التجريبية المجانية', 'Free Trial Version'],
    ['النسخة التجريبية', 'Trial Version'],
    ['20 فاتورة فقط', '20 Invoices Only'],
    ['٢٠ فاتورة فقط', '20 Invoices Only'],
    ['20 فاتورة مجانية', '20 Free Invoices'],
    ['20 فاتورة', '20 Invoices'],
    ['الميزات الفعلية', 'Core Features'],
    ['معاينة الواجهات', 'UI Previews'],
    ['تصميم الفاتورة', 'Invoice Design'],
    ['التجربة والترخيص', 'Trial & Licensing'],
    ['حزمة البرامج', 'Software Suite'],
    ['الأسئلة الشائعة', 'FAQ'],
    ['مؤسسة الشام للتجارة', 'Al-Sham Trading Est.'],
    ['فاتورة مبيعات', 'Sales Invoice'],
    ['فاتورة مشتريات', 'Purchase Invoice'],
    ['مرتجع مبيعات', 'Sales Return'],
    ['مرتجع مشتريات', 'Purchase Return'],
    ['رقم الفاتورة', 'Invoice No.'],
    ['آجل (دفعة)', 'Credit (Installment)'],
    ['تفاصيل الأصناف', 'Item Details'],
    ['ملخص الفاتورة', 'Invoice Summary'],
    ['العدد الكلي', 'Total Units'],
    ['إجمالي الوزن', 'Total Weight'],
    ['الصافي المطلوب', 'Net Amount Due'],
    ['المبلغ المطلوب', 'Amount Due'],
    ['الإجمالي النهائي', 'Grand Total'],
    ['معاينة الطباعة', 'Print Preview'],
    ['رائحة فاخرة بالمكسرات', 'Premium Nut Delight'],
    ['نوغا سلطانية بالفستق', 'Royal Pistachio Nougat'],
    ['تحويل هرمي', 'Hierarchical Conversion'],
    ['الرئيسية', 'Home'],
    ['لوحة التحكم', 'Dashboard'],
    ['المنتجات', 'Products'],
    ['الأصناف', 'Items'],
    ['المستودعات', 'Warehouses'],
    ['المخزون', 'Inventory'],
    ['المبيعات', 'Sales'],
    ['المشتريات', 'Purchases'],
    ['العملاء', 'Customers'],
    ['الموردين', 'Suppliers'],
    ['الموردون', 'Suppliers'],
    ['الحسابات', 'Accounts'],
    ['المالية', 'Finance'],
    ['التقارير', 'Reports'],
    ['الإعدادات', 'Settings'],
    ['الفواتير', 'Invoices'],
    ['إنشاء فاتورة', 'Create Invoice'],
    ['فاتورة جديدة', 'New Invoice'],
    ['المدة التجريبية', 'Trial Limit'],
    ['الفترة التجريبية', 'Trial Limit'],
    ['تجربة مجانية', 'Free Trial'],
    ['ترخيص مدى الحياة', 'Lifetime License'],
    ['ترخيص سنوي', 'Annual License'],
    ['تفعيل الترخيص', 'Activate License'],
    ['شراء النسخة الكاملة', 'Buy Full Version'],
    ['النسخة الكاملة', 'Full Version'],
    ['تواصل معنا', 'Contact Us'],
    ['الدعم الفني', 'Technical Support'],
    ['نسخ احتياطي تلقائي', 'Automatic Backup'],
    ['نسخ احتياطي', 'Backup'],
    ['قاعدة البيانات', 'Database'],
    ['الطباعة الحرارية', 'Thermal Printing'],
    ['تعدد العملات', 'Multi-Currency'],
    ['تعدد الوحدات', 'Multi-Unit System'],
    ['نقاط البيع', 'Point of Sale (POS)'],
    ['نقطة بيع', 'Point of Sale (POS)'],
    ['السعر الإفرادي', 'Unit Price'],
    ['طريقة الدفع', 'Payment Method'],
    ['اسم العميل', 'Customer Name'],
    ['رصيد سابق', 'Previous Balance'],
    ['المبلغ المدفوع', 'Paid Amount'],
    ['المبلغ المتبقي', 'Remaining Balance'],
    ['جميع الحقوق محفوظة', 'All rights reserved'],
    ['وحدات', 'Units'],
    ['وحدة', 'Unit'],
    ['صندوق', 'Box'],
    ['طرد', 'Parcel'],
    ['كغ', 'kg'],
    ['نقدي', 'Cash'],
    ['آجل', 'Credit'],
    ['الإجمالي', 'Total'],
    ['الكمية', 'Quantity'],
    ['السعر', 'Price'],
    ['الوزن', 'Weight'],
    ['الخصم', 'Discount'],
    ['الحسم', 'Discount'],
    ['الضريبة', 'Tax'],
    ['البيان', 'Description'],
    ['الوصف', 'Description'],
    ['التاريخ', 'Date'],
    ['العملة', 'Currency'],
    ['حفظ', 'Save'],
    ['طباعة', 'Print'],
    ['تنزيل', 'Download'],
    ['تحميل', 'Download'],
    ['إلغاء', 'Cancel'],
    ['إغلاق', 'Close'],
    ['بحث', 'Search'],
    ['ذكي', 'Smart'],
    ['مجاناً', 'Free'],
    ['مجانا', 'Free']
  ];

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
    'صندوق': 'Cashbox',
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

  // 1. إزالة بطاقة Desktop ERP المزعجة
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

  // 2. تحويل 30 يوم إلى 20 فاتورة فقط
  function normalizeTrialText(text) {
    if (!text) return text;
    if ((text.indexOf('30') !== -1 || text.indexOf('٣٠') !== -1) && text.indexOf('يوم') !== -1) {
      return text
        .replace(trialRegexWithDuration, '20 فاتورة فقط')
        .replace(trialRegexGeneral, '20 فاتورة فقط');
    }
    return text;
  }

  // 3. ترجمة النص العربي إلى الإنجليزية بدقة
  function translateArabicToEnglish(arText) {
    if (!arText) return arText;
    var result = normalizeTrialText(arText);

    for (var i = 0; i < exactTranslations.length; i++) {
      var arPhrase = exactTranslations[i][0];
      var enPhrase = exactTranslations[i][1];
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

    return result;
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

    var attrElements = document.querySelectorAll('[placeholder], [title], [aria-label], [data-label]');
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

      var attrs = ['placeholder', 'title', 'aria-label', 'data-label'];
      for (var k = 0; k < attrs.length; k++) {
        var attr = attrs[k];
        if (el.hasAttribute(attr)) {
          if (typeof savedAttrs[attr] !== 'string') {
            savedAttrs[attr] = normalizeTrialText(el.getAttribute(attr) || '');
          }
          var arVal = savedAttrs[attr];
          var newVal = currentLang === 'en' ? translateArabicToEnglish(arVal) : arVal;
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
      var text = (a.textContent || '').trim();
      if (!text) continue;
      if (/^invoify/i.test(text) && text.length < 15) continue;

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

    var headerInner = header.querySelector('div.flex, div[class*="justify-between"], nav.flex') || header;
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
    var headerButtons = header.querySelectorAll('button, [role="button"]');
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
        '<svg class="lang-globe-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
        '<circle cx="12" cy="12" r="10"></circle>' +
        '<line x1="2" y1="12" x2="22" y2="12"></line>' +
        '<path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>' +
        '</svg>' +
        '<span class="lang-btn-text">' + (currentLang === 'en' ? 'عربي' : 'EN') + '</span>';

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
        '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">' +
        '<line x1="3" y1="6" x2="21" y2="6"></line>' +
        '<line x1="3" y1="12" x2="21" y2="12"></line>' +
        '<line x1="3" y1="18" x2="21" y2="18"></line>' +
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
        '<div class="invoify-drawer-header">' +
        '<div class="invoify-drawer-brand">' +
        '<span class="invoify-drawer-logo">V</span>' +
        '<div><strong class="invoify-drawer-title">Invoify</strong></div>' +
        '</div>' +
        '<button type="button" class="invoify-drawer-close" aria-label="Close">' +
        '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>' +
        '</button>' +
        '</div>' +
        '<div class="invoify-drawer-links"></div>';

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

  // 6. تحويل الجداول إلى بطاقات متجاوبة على الموبايل
  function enhanceTables() {
    var tables = document.querySelectorAll('table');
    for (var t = 0; t < tables.length; t++) {
      var table = tables[t];
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
      var interactive = target.closest ? target.closest('button, a, [role="tab"], [role="button"]') : null;
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
