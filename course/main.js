(() => {
  const modules = [...document.querySelectorAll('.module')];
  const dots = [...document.querySelectorAll('.nav-dot')];
  const progress = document.getElementById('progress');

  dots.forEach(dot => dot.addEventListener('click', () => {
    document.getElementById(dot.dataset.target)?.scrollIntoView({behavior:'smooth'});
  }));

  const setActive = id => dots.forEach(dot => dot.classList.toggle('active', dot.dataset.target === id));
  const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (entry.isIntersecting) setActive(entry.target.id);
    });
  }, {threshold:.35});
  modules.forEach(m => observer.observe(m));
  if (modules[0]) setActive(modules[0].id);

  const animObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => { if (entry.isIntersecting) entry.target.classList.add('visible'); });
  }, {threshold:.12});
  document.querySelectorAll('.animate-in').forEach(el => animObserver.observe(el));

  const updateProgress = () => {
    const max = document.documentElement.scrollHeight - innerHeight;
    const pct = max > 0 ? Math.min(100, Math.max(0, scrollY / max * 100)) : 0;
    if (progress) progress.style.width = `${pct}%`;
  };
  addEventListener('scroll', updateProgress, {passive:true}); updateProgress();

  document.querySelectorAll('.quiz').forEach(quiz => {
    const feedback = quiz.querySelector('.quiz-feedback');
    quiz.querySelectorAll('.quiz-option').forEach(option => option.addEventListener('click', () => {
      quiz.querySelectorAll('.quiz-option').forEach(o => o.classList.remove('correct','incorrect'));
      const ok = option.dataset.correct === 'true';
      option.classList.add(ok ? 'correct' : 'incorrect');
      if (feedback) {
        feedback.textContent = ok ? (quiz.dataset.success || 'Exact.') : (quiz.dataset.retry || 'Pas tout à fait. Relis le scénario et réessaie.');
        feedback.classList.add('show');
      }
    }));
  });

  document.querySelectorAll('.flow').forEach(flow => {
    const actors = [...flow.querySelectorAll('.flow-actor')];
    const steps = (flow.dataset.steps || '').split('|').filter(Boolean);
    const status = flow.querySelector('.flow-status');
    const next = flow.querySelector('.flow-next');
    let index = -1;
    next?.addEventListener('click', () => {
      index = (index + 1) % Math.max(1, actors.length);
      actors.forEach((a,i) => a.classList.toggle('active', i === index));
      if (status && steps.length) status.textContent = steps[index % steps.length];
    });
  });

  document.querySelectorAll('.chat').forEach(chat => {
    const messages = [...chat.querySelectorAll('.chat-message')];
    const start = chat.querySelector('.chat-start');
    const reset = chat.querySelector('.chat-reset');
    let timer = null;
    const clear = () => { if (timer) clearInterval(timer); timer = null; messages.forEach(m => m.classList.remove('visible')); };
    start?.addEventListener('click', () => {
      clear(); let i = 0;
      if (messages[0]) messages[0].classList.add('visible');
      timer = setInterval(() => { i += 1; if (i >= messages.length) { clearInterval(timer); timer = null; return; } messages[i].classList.add('visible'); }, 650);
    });
    reset?.addEventListener('click', clear);
  });

  document.querySelectorAll('[data-reveal]').forEach(button => button.addEventListener('click', () => {
    const target = document.getElementById(button.dataset.reveal);
    if (!target) return;
    target.hidden = !target.hidden;
    button.setAttribute('aria-expanded', String(!target.hidden));
  }));
})();
