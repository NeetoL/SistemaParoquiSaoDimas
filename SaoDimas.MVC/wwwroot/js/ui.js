// Comportamento de apresentação; não modifica validações ou dados enviados.
export function iniciarInterface() {
  let sequencia = 0;
  function descreverCampos(conteudo = document) {
    for (const campo of conteudo.querySelectorAll(".form-control")) {
      const grupo = campo.closest(".form-field");
      const descricoes = [...(grupo?.querySelectorAll(".form-hint,.form-error,.field-validation-error") ?? [])].filter(item => item.textContent.trim());
      const ids = new Set((campo.getAttribute("aria-describedby") ?? "").split(" ").filter(Boolean));
      for (const descricao of descricoes) {
        if (!descricao.id) descricao.id = "ui-campo-descricao-" + (++sequencia);
        ids.add(descricao.id);
      }
      if (ids.size) campo.setAttribute("aria-describedby", [...ids].join(" "));
      if (campo.classList.contains("input-validation-error")) campo.setAttribute("aria-invalid", "true");
    }
    for (const tabela of conteudo.querySelectorAll(".table")) {
      for (const cabecalho of tabela.querySelectorAll("thead th")) cabecalho.setAttribute("scope", "col");
    }
  }
  descreverCampos();
  document.addEventListener("htmx:afterSwap", evento => descreverCampos(evento.detail.target));
  document.addEventListener("submit", evento => {
    const formulario = evento.target;
    if (formulario.closest("[hx-post],[hx-get]") || formulario.method.toLowerCase() !== "post") return;
    if (evento.defaultPrevented) return;
    evento.submitter?.setAttribute("aria-busy", "true");
  });
  document.addEventListener("htmx:beforeRequest", evento => {
    const origem = evento.detail.elt;
    if (origem.matches(".btn")) origem.setAttribute("aria-busy", "true");
    else if (origem.matches("form")) origem.querySelector('button[type="submit"],button:not([type])')?.setAttribute("aria-busy", "true");
  });
  document.addEventListener("htmx:afterRequest", evento => {
    const origem = evento.detail.elt;
    origem.removeAttribute("aria-busy");
    origem.querySelectorAll('[aria-busy="true"]').forEach(item => item.removeAttribute("aria-busy"));
  });
  window.addEventListener("pageshow", () => document.querySelectorAll('.btn[aria-busy="true"]').forEach(item => item.removeAttribute("aria-busy")));
}

