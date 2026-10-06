const TIPOS = new Set(["success", "error", "warning", "info"]);
const DURACAO_PADRAO_MS = 5000;

/**
 * Exibe um toast no canto superior direito.
 * @param {string} mensagem Texto (inserido como texto, nunca como HTML).
 * @param {"success"|"error"|"warning"|"info"} [tipo]
 * @param {number} [duracao] Em ms; 0 mantém até o usuário fechar.
 */
export function mostrarToast(mensagem, tipo = "success", duracao = DURACAO_PADRAO_MS) {
  const container = document.getElementById("toasts");
  const template = document.getElementById("toast-template");
  if (!container || !template) return;

  if (!TIPOS.has(tipo)) tipo = "info";

  const toast = template.content.firstElementChild.cloneNode(true);
  toast.classList.add(`toast-${tipo}`);
  if (tipo === "error") toast.setAttribute("role", "alert");

  for (const icone of toast.querySelectorAll(".toast-icone [data-tipo]")) {
    if (icone.dataset.tipo !== tipo) icone.remove();
  }
  toast.querySelector(".toast-mensagem").textContent = mensagem;

  const fechar = () => {
    if ("saindo" in toast.dataset) return;
    toast.dataset.saindo = "";
    toast.addEventListener("transitionend", () => toast.remove(), { once: true });
    setTimeout(() => toast.remove(), 400); // garante remoção com movimento reduzido
  };

  toast.querySelector("[data-toast-fechar]").addEventListener("click", fechar);
  container.append(toast);

  if (duracao > 0) {
    let temporizador = setTimeout(fechar, duracao);
    toast.addEventListener("pointerenter", () => clearTimeout(temporizador));
    toast.addEventListener("pointerleave", () => (temporizador = setTimeout(fechar, duracao / 2)));
  }
}

/**
 * Permite disparar toasts por evento, inclusive via HTMX:
 * HX-Trigger: {"toast": {"mensagem": "Dizimista cadastrado com sucesso.", "tipo": "success"}}
 */
export function iniciarToasts() {
  // Mensagens renderizadas pelo servidor (TempData) após um redirect.
  for (const mensagem of document.querySelectorAll("[data-toast-servidor]")) {
    mostrarToast(mensagem.textContent.trim(), mensagem.dataset.toastServidor);
    mensagem.remove();
  }

  document.body.addEventListener("toast", (e) => {
    const { mensagem, tipo, duracao } = e.detail ?? {};
    if (mensagem) mostrarToast(String(mensagem), tipo, duracao);
  });
}
