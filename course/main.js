(() => {
  const links = [...document.querySelectorAll('.toc a')];
  const sections = links.map(link => document.querySelector(link.getAttribute('href'))).filter(Boolean);
  const progress = document.getElementById('progress');

  const setActive = id => links.forEach(link => link.classList.toggle('active', link.getAttribute('href') === `#${id}`));
  const observer = new IntersectionObserver(entries => {
    const visible = entries.filter(e => e.isIntersecting).sort((a,b) => b.intersectionRatio - a.intersectionRatio)[0];
    if (visible) setActive(visible.target.id);
  }, {rootMargin:'-15% 0px -65% 0px', threshold:[0,.2,.5,1]});
  sections.forEach(section => observer.observe(section));

  const updateProgress = () => {
    const max = document.documentElement.scrollHeight - innerHeight;
    const value = max > 0 ? Math.min(100, Math.max(0, scrollY / max * 100)) : 0;
    if (progress) progress.style.width = `${value}%`;
  };
  addEventListener('scroll', updateProgress, {passive:true});
  updateProgress();

  document.querySelectorAll('.quiz').forEach(quiz => {
    const feedback = quiz.querySelector('.quiz-feedback');
    quiz.querySelectorAll('button').forEach(button => button.addEventListener('click', () => {
      quiz.querySelectorAll('button').forEach(b => b.classList.remove('correct','incorrect'));
      const ok = button.dataset.correct === 'true';
      button.classList.add(ok ? 'correct' : 'incorrect');
      if (feedback) feedback.textContent = ok ? (quiz.dataset.success || 'Exact.') : (quiz.dataset.retry || 'Pas encore. Reprends le point juste au-dessus.');
    }));
  });
})();
