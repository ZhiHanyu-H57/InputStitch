(() => {
  const html = document.documentElement;
  const saved = localStorage.getItem('inputstitch-lang');
  const auto = (navigator.language || '').toLowerCase().startsWith('zh') ? 'zh' : 'en';
  const lang = saved || auto;
  html.dataset.lang = lang;
  html.lang = lang === 'zh' ? 'zh-CN' : 'en';

  const syncLanguageUi = () => {
    document.querySelectorAll('[data-lang-toggle]').forEach(btn => {
      btn.textContent = html.dataset.lang === 'zh' ? 'EN' : '中文';
      btn.setAttribute('aria-label', html.dataset.lang === 'zh' ? 'Switch to English' : '切换为中文');
    });
    document.querySelectorAll('[data-title-zh]').forEach(node => {
      document.title = html.dataset.lang === 'zh' ? node.dataset.titleZh : node.dataset.titleEn;
    });
  };
  syncLanguageUi();

  document.querySelectorAll('[data-lang-toggle]').forEach(btn => {
    btn.addEventListener('click', () => {
      const next = html.dataset.lang === 'zh' ? 'en' : 'zh';
      html.dataset.lang = next;
      html.lang = next === 'zh' ? 'zh-CN' : 'en';
      localStorage.setItem('inputstitch-lang', next);
      syncLanguageUi();
    });
  });

  const header = document.querySelector('.site-header');
  const syncHeader = () => header?.classList.toggle('scrolled', window.scrollY > 12);
  syncHeader();
  window.addEventListener('scroll', syncHeader, { passive: true });

  const menu = document.querySelector('[data-mobile-menu]');
  const nav = document.querySelector('.nav-links');
  menu?.addEventListener('click', () => {
    const open = nav?.classList.toggle('open');
    menu.setAttribute('aria-expanded', open ? 'true' : 'false');
  });
  nav?.querySelectorAll('a').forEach(a => a.addEventListener('click', () => nav.classList.remove('open')));

  const year = document.querySelector('[data-year]');
  if (year) year.textContent = new Date().getFullYear();

  const docsLinks = [...document.querySelectorAll('.docs-nav a[href^="#"]')];
  const docsSections = docsLinks.map(a => document.querySelector(a.getAttribute('href'))).filter(Boolean);
  if (docsSections.length && 'IntersectionObserver' in window) {
    const byId = new Map(docsLinks.map(a => [a.getAttribute('href').slice(1), a]));
    const observer = new IntersectionObserver(entries => {
      const visible = entries.filter(x => x.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
      if (!visible) return;
      docsLinks.forEach(a => a.classList.remove('active'));
      byId.get(visible.target.id)?.classList.add('active');
    }, { rootMargin: '-20% 0px -65% 0px', threshold: [0, .1, .5] });
    docsSections.forEach(s => observer.observe(s));
  }
})();
