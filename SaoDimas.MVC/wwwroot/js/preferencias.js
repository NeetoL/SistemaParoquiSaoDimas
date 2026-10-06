// Carregado de forma síncrona no <head>: aplica as preferências salvas antes da
// primeira pintura, evitando que a sidebar "pisque" ao carregar a página.
try {
  if (localStorage.getItem("sd:sidebar") === "recolhida") {
    document.documentElement.dataset.sidebar = "recolhida";
  }
} catch {
  // localStorage indisponível (modo privado/bloqueado): usa o padrão expandido.
}

// Tema explícito salvo; na primeira visita respeita a preferência do dispositivo.
(() => {
  const raiz = document.documentElement;
  const dispositivo = matchMedia("(prefers-color-scheme: dark)");
  let escolha = null;
  try { escolha = localStorage.getItem("sd:tema"); } catch {}
  if (escolha !== "dark" && escolha !== "light") escolha = null;
  function aplicar(tema) {
    raiz.dataset.theme = tema;
    document.querySelectorAll("[data-alternar-tema]").forEach(botao => {
      botao.setAttribute("aria-pressed", String(tema === "dark"));
      botao.setAttribute("aria-label", tema === "dark" ? "Ativar modo claro" : "Ativar modo escuro");
      botao.title = tema === "dark" ? "Ativar modo claro" : "Ativar modo escuro";
    });
  }
  aplicar(escolha ?? (dispositivo.matches ? "dark" : "light"));
  document.addEventListener("DOMContentLoaded", () => aplicar(raiz.dataset.theme));
  document.addEventListener("click", evento => {
    if (!evento.target.closest("[data-alternar-tema]")) return;
    escolha = raiz.dataset.theme === "dark" ? "light" : "dark";
    try { localStorage.setItem("sd:tema", escolha); } catch {}
    aplicar(escolha);
  });
  dispositivo.addEventListener("change", () => { if (!escolha) aplicar(dispositivo.matches ? "dark" : "light"); });
  window.addEventListener("storage", evento => {
    if (evento.key !== "sd:tema") return;
    escolha = evento.newValue === "dark" || evento.newValue === "light" ? evento.newValue : null;
    aplicar(escolha ?? (dispositivo.matches ? "dark" : "light"));
  });
})();
