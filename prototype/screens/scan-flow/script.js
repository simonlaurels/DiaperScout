const views = document.querySelectorAll('.view');
const show = id => views.forEach(view => view.classList.toggle('active', view.id === id));
document.querySelectorAll('[data-go]').forEach(button => button.addEventListener('click', () => show(button.dataset.go)));
document.querySelector('#start-scan').addEventListener('click', () => {
  const scanner = document.querySelector('#scanner');
  const label = document.querySelector('#scan-label');
  scanner.classList.add('scanning'); label.textContent = 'Looking closely…';
  setTimeout(() => { scanner.classList.remove('scanning'); label.textContent = 'Product identified'; show('found-view'); }, 1450);
});
document.querySelector('#add-observation').addEventListener('click', () => show('observation-view'));
document.querySelector('#observation-form').addEventListener('submit', event => { event.preventDefault(); show('success-view'); });
document.querySelector('#scan-another').addEventListener('click', () => show('scan-view'));
