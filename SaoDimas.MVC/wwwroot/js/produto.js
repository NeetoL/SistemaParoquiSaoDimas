// Navegação de apresentação. Os destinos vêm dos links autorizados da sidebar.
const dialogo = document.querySelector('#acesso-rapido');
if (dialogo) {
  const busca = dialogo.querySelector('#acesso-busca');
  const lista = dialogo.querySelector('[data-acesso-lista]');
  const vazio = dialogo.querySelector('[data-acesso-vazio]');
  const destinos = [...document.querySelectorAll('.sidebar-nav a.sidebar-link')].map(link => ({
    nome: link.querySelector('.sidebar-label').textContent.trim(), url: link.href,
    icone: link.querySelector('svg')?.cloneNode(true)
  }));
  const normalizar = texto => texto.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  function renderizar() {
    lista.replaceChildren();
    for (const destino of destinos.filter(item => normalizar(item.nome).includes(normalizar(busca.value)))) {
      const link = document.createElement('a');
      link.href = destino.url;
      link.className = 'produto-destino';
      if (destino.icone) link.append(destino.icone.cloneNode(true));
      const nome = document.createElement('span');
      nome.textContent = destino.nome;
      link.append(nome);
      lista.append(link);
    }
    vazio.hidden = lista.children.length > 0;
  }
  function abrir() { if (dialogo.open) return; busca.value = ''; renderizar(); dialogo.showModal(); busca.focus(); }
  document.querySelector('[data-acesso-abrir]')?.addEventListener('click', abrir);
  dialogo.querySelector('[data-acesso-fechar]').addEventListener('click', () => dialogo.close());
  dialogo.addEventListener('keydown', evento => {
    if (evento.key === 'Escape') { evento.preventDefault(); dialogo.close(); }
  });
  busca.addEventListener('input', renderizar);
  busca.addEventListener('keydown', evento => {
    if (evento.key === 'ArrowDown') { evento.preventDefault(); lista.querySelector('a')?.focus(); }
    if (evento.key === 'Enter' && lista.children.length === 1) { evento.preventDefault(); lista.querySelector('a').click(); }
  });
  document.addEventListener('keydown', evento => {
    if ((evento.ctrlKey || evento.metaKey) && evento.key.toLowerCase() === 'k') { evento.preventDefault(); abrir(); }
  });
}
