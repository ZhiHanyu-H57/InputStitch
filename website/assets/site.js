(() => {
  'use strict';
  const root = document.documentElement;
  const read = key => { try { return localStorage.getItem(key); } catch { return null; } };
  const write = (key, value) => { try { localStorage.setItem(key, value); } catch { /* preferences remain usable for this page */ } };
  const menu = document.querySelector('[data-menu-toggle]');
  const nav = document.querySelector('[data-navigation]');
  let release = null;
  const formatBytes = bytes => `${(bytes / 1048576).toFixed(2)} MiB`;
  function paintRelease(value) {
    release = value;
    document.querySelectorAll('[data-release-version]').forEach(node => { node.textContent = value.version; });
    document.querySelectorAll('[data-release-size]').forEach(node => {
      const bytes = value.assets?.[node.dataset.releaseSize]?.size;
      if (Number.isFinite(bytes)) node.textContent = formatBytes(bytes);
    });
    document.querySelectorAll('[data-release-date]').forEach(node => {
      const date = new Date(value.publishedAt);
      if (Number.isNaN(date.getTime())) return;
      node.dateTime = date.toISOString().slice(0, 10);
      node.textContent = new Intl.DateTimeFormat(root.lang, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
    });
  }
  function setLanguage(language) {
    const lang = language === 'en' ? 'en' : 'zh';
    root.dataset.lang = lang;
    root.lang = lang === 'zh' ? 'zh-CN' : 'en';
    document.title = document.body.dataset[lang === 'zh' ? 'titleZh' : 'titleEn'];
    const description = document.querySelector('meta[name="description"]');
    if (description) description.content = document.body.dataset[lang === 'zh' ? 'descriptionZh' : 'descriptionEn'];
    document.querySelectorAll('[data-lang-toggle]').forEach(button => {
      button.textContent = lang === 'zh' ? 'EN' : '中文';
      button.setAttribute('aria-label', lang === 'zh' ? 'Switch to English' : '切换为中文');
    });
    document.querySelectorAll('[data-aria-zh]').forEach(node => {
      node.setAttribute('aria-label', node.dataset[lang === 'zh' ? 'ariaZh' : 'ariaEn']);
    });
    document.querySelectorAll('[data-image-zh]').forEach(node => {
      node.src = node.dataset[lang === 'zh' ? 'imageZh' : 'imageEn'];
      node.alt = node.dataset[lang === 'zh' ? 'altZh' : 'altEn'];
      const link = node.closest('[data-image-open]');
      if (link) link.href = node.src;
    });
    if (release) paintRelease(release);
  }
  const savedLanguage = read('inputstitch-lang');
  setLanguage(['zh', 'en'].includes(savedLanguage) ? savedLanguage : (navigator.language || '').toLowerCase().startsWith('zh') ? 'zh' : 'en');
  document.querySelectorAll('[data-lang-toggle]').forEach(button => button.addEventListener('click', () => {
    const lang = root.dataset.lang === 'zh' ? 'en' : 'zh';
    write('inputstitch-lang', lang);
    setLanguage(lang);
  }));
  function closeMenu() { nav?.classList.remove('open'); menu?.setAttribute('aria-expanded', 'false'); }
  menu?.addEventListener('click', () => {
    const open = menu.getAttribute('aria-expanded') !== 'true';
    nav.classList.toggle('open', open);
    menu.setAttribute('aria-expanded', String(open));
  });
  nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', closeMenu));
  document.addEventListener('click', event => { if (!event.target.closest('.site-header')) closeMenu(); });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && menu?.getAttribute('aria-expanded') === 'true') { closeMenu(); menu.focus(); }
  });
  const desktop = matchMedia('(min-width:561px)');
  desktop.addEventListener?.('change', event => { if (event.matches) closeMenu(); });

  const dialog = document.querySelector('[data-image-dialog]');
  document.querySelectorAll('[data-image-open]').forEach(link => link.addEventListener('click', event => {
    if (!dialog?.showModal || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    const source = link.querySelector('img');
    const image = dialog.querySelector('img');
    image.src = source.currentSrc || source.src;
    image.alt = source.alt;
    dialog.showModal();
  }));
  dialog?.querySelector('[data-dialog-close]').addEventListener('click', () => dialog.close());
  dialog?.addEventListener('click', event => { if (event.target === dialog) dialog.close(); });

  const stage = document.querySelector('[data-reticle]');
  if (stage) {
    const paths = {
      corners: '<path d="M13 7h22M41 13v22M35 41H13M7 35V13"/>',
      cross: '<path d="M24 4v11m0 18v11M4 24h11m18 0h11"/>',
      dot: '<circle cx="24" cy="24" r="3" fill="currentColor" stroke="none"/>',
      crossdot: '<path d="M24 4v11m0 18v11M4 24h11m18 0h11"/><circle cx="24" cy="24" r="2" fill="currentColor" stroke="none"/>',
      ring: '<circle cx="24" cy="24" r="13"/><circle cx="24" cy="24" r="2" fill="currentColor" stroke="none"/>'
    };
    document.querySelectorAll('[data-reticle-style]').forEach(button => button.addEventListener('click', () => {
      const path = paths[button.dataset.reticleStyle];
      if (!path) return;
      document.querySelectorAll('[data-reticle-style]').forEach(other => other.setAttribute('aria-pressed', String(button === other)));
      stage.querySelector('svg').innerHTML = path;
    }));
    document.querySelectorAll('[data-reticle-color]').forEach(button => button.addEventListener('click', () => {
      stage.style.setProperty('--reticle-color', button.dataset.reticleColor);
      document.querySelectorAll('[data-reticle-color]').forEach(other => other.setAttribute('aria-pressed', String(button === other)));
    }));
    const opacity = document.querySelector('[data-reticle-opacity]');
    opacity?.addEventListener('input', () => {
      stage.style.opacity = Number(opacity.value) / 100;
      document.querySelector('[data-opacity-value]').textContent = `${opacity.value}%`;
    });
  }
  const docLinks = [...document.querySelectorAll('.docs-nav a[href^="#"]')];
  if (docLinks.length && 'IntersectionObserver' in window) {
    const observer = new IntersectionObserver(entries => {
      const first = entries.find(entry => entry.isIntersecting);
      if (!first) return;
      docLinks.forEach(link => {
        if (link.hash === `#${first.target.id}`) link.setAttribute('aria-current', 'true');
        else link.removeAttribute('aria-current');
      });
    }, { rootMargin: '-100px 0px -55% 0px' });
    docLinks.forEach(link => { const section = document.getElementById(link.hash.slice(1)); if (section) observer.observe(section); });
  }

  // A small local snapshot renders immediately; an official Stable lookup refreshes
  // version/date/size independently of this website's publication schedule.
  async function updateRelease() {
    let snapshot;
    try { const response = await fetch('/assets/release.json'); if (!response.ok) return; snapshot = await response.json(); paintRelease(snapshot); }
    catch { return; }
    const key = 'inputstitch-release-v2';
    const compare = (a, b) => a.split('.').reduce((result, part, i) => result || Number(part) - Number(b.split('.')[i]), 0);
    try {
      const cached = JSON.parse(read(key) || 'null');
      if (cached && /^\d+\.\d+\.\d+$/.test(cached.version) && compare(cached.version, snapshot.version) >= 0) {
        paintRelease(cached);
        if (Date.now() - cached.checkedAt < 21600000) return;
      }
    } catch { /* ignore an unreadable preference */ }
    const abort = new AbortController();
    const timeout = setTimeout(() => abort.abort(), 2500);
    try {
      const response = await fetch('https://api.github.com/repos/ZhiHanyu-H57/InputStitch/releases/latest', { signal: abort.signal, headers: { Accept: 'application/vnd.github+json' } });
      if (!response.ok) return;
      const data = await response.json();
      const version = (data.tag_name || '').replace(/^v/, '');
      if (data.draft || data.prerelease || !/^\d+\.\d+\.\d+$/.test(version) || compare(version, snapshot.version) < 0) return;
      const assets = {};
      for (const arch of ['x64', 'x86']) {
        const asset = data.assets?.find(item => item.name === `InputStitch-${version}-Windows-${arch}.exe`);
        if (!asset || !Number.isFinite(asset.size)) return;
        assets[arch] = { size: asset.size };
      }
      const value = { version, publishedAt: data.published_at, assets, checkedAt: Date.now() };
      paintRelease(value); write(key, JSON.stringify(value));
    } catch { /* the local snapshot and permanent download links remain available */ }
    finally { clearTimeout(timeout); }
  }
  updateRelease();
})();
