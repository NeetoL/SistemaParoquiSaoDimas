import { iniciarInterface } from './ui.js';
iniciarInterface();
const botao = document.querySelector('[data-senha-visibilidade]');
const senha = document.querySelector('#Senha');
botao?.addEventListener('click', () => {
  const mostrar = senha.type === 'password';
  senha.type = mostrar ? 'text' : 'password';
  botao.setAttribute('aria-pressed', String(mostrar));
  botao.setAttribute('aria-label', mostrar ? 'Ocultar senha' : 'Mostrar senha');
});
