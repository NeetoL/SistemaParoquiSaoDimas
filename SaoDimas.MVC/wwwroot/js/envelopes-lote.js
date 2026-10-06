// Seleção de dizimistas na listagem para gerar Envelopes de Dízimo em lote (um envelope por folha A4).
const barra = document.querySelector("[data-envelopes-lote]");

if (barra) {
  const todos = document.querySelector("[data-envelopes-todos]");
  const itens = [...document.querySelectorAll("[data-envelopes-item]")];
  const gerar = barra.querySelector("[data-envelopes-gerar]");
  const contagem = barra.querySelector("[data-envelopes-contagem]");

  const atualizar = () => {
    const selecionados = itens.filter((item) => item.checked).length;

    gerar.disabled = selecionados === 0;
    contagem.textContent = selecionados === 0
      ? "Selecione dizimistas para gerar envelopes em lote."
      : `${selecionados} dizimista(s) selecionado(s): um envelope por folha A4.`;

    if (todos) {
      todos.checked = selecionados > 0 && selecionados === itens.length;
      todos.indeterminate = selecionados > 0 && selecionados < itens.length;
    }
  };

  todos?.addEventListener("change", () => {
    for (const item of itens) item.checked = todos.checked;
    atualizar();
  });

  for (const item of itens) item.addEventListener("change", atualizar);
  atualizar();
}
