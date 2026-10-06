const CHAVE_PREFERENCIA = "sd:sidebar";

const raiz = document.documentElement;
const desktop = matchMedia("(min-width: 64rem)");

export function iniciarSidebar() {
  const sidebar = document.getElementById("sidebar");
  const overlay = document.getElementById("sidebar-overlay");
  const principal = document.getElementById("app-principal");
  const botaoAbrir = document.querySelector("[data-sidebar-abrir]");
  const botaoRecolher = document.querySelector("[data-sidebar-recolher]");

  if (!sidebar || !overlay || !principal || !botaoAbrir || !botaoRecolher) return;

  // ---------------------------------------------------------- Desktop: recolher/expandir

  const estaRecolhida = () => raiz.dataset.sidebar === "recolhida";

  const atualizarBotaoRecolher = () => {
    const recolhida = estaRecolhida();
    botaoRecolher.setAttribute("aria-expanded", String(!recolhida));
    botaoRecolher.setAttribute("aria-label", recolhida ? "Expandir menu lateral" : "Recolher menu lateral");
  };

  botaoRecolher.addEventListener("click", () => {
    if (estaRecolhida()) delete raiz.dataset.sidebar;
    else raiz.dataset.sidebar = "recolhida";

    try {
      localStorage.setItem(CHAVE_PREFERENCIA, estaRecolhida() ? "recolhida" : "expandida");
    } catch {
      // Sem persistência; o estado vale apenas para esta página.
    }

    atualizarBotaoRecolher();
    esconderTooltip();
  });

  atualizarBotaoRecolher();

  // Os grupos começam fechados. O atributo name oferece exclusão nativa;
  // este listener também mantém o comportamento em navegadores sem esse suporte.
  const grupos = [...sidebar.querySelectorAll(".sidebar-grupo")];
  for (const grupo of grupos) {
    grupo.addEventListener("toggle", () => {
      if (!grupo.open) return;
      for (const outro of grupos) {
        if (outro !== grupo) outro.open = false;
      }
    });
    grupo.querySelector("summary").addEventListener("click", () => {
      if (!desktop.matches || !estaRecolhida()) return;
      delete raiz.dataset.sidebar;
      try { localStorage.setItem(CHAVE_PREFERENCIA, "expandida"); } catch {}
      atualizarBotaoRecolher();
      esconderTooltip();
    });
  }

  // ---------------------------------------------------------- Desktop recolhida: tooltips

  const tooltip = document.createElement("div");
  tooltip.className = "sidebar-tooltip";
  tooltip.setAttribute("aria-hidden", "true"); // o nome acessível já vem do rótulo (sr-only)
  tooltip.hidden = true;
  document.body.append(tooltip);

  function mostrarTooltip(alvo) {
    if (!desktop.matches || !estaRecolhida()) return;

    const area = alvo.getBoundingClientRect();
    tooltip.textContent = alvo.dataset.tooltip;
    tooltip.style.top = `${area.top + area.height / 2}px`;
    tooltip.style.left = `${sidebar.getBoundingClientRect().right + 8}px`;
    tooltip.hidden = false;
  }

  function esconderTooltip() {
    tooltip.hidden = true;
  }

  for (const evento of ["pointerover", "focusin"]) {
    sidebar.addEventListener(evento, (e) => {
      const alvo = e.target.closest("[data-tooltip]");
      if (alvo) mostrarTooltip(alvo);
      else esconderTooltip();
    });
  }
  for (const evento of ["pointerleave", "focusout"]) {
    sidebar.addEventListener(evento, esconderTooltip);
  }
  sidebar.querySelector(".sidebar-nav")?.addEventListener("scroll", esconderTooltip, { passive: true });

  // ---------------------------------------------------------- Mobile/tablet: drawer

  const drawerAberto = () => "aberta" in sidebar.dataset;

  // Conteúdo de fundo fica inacessível ao teclado/leitores de tela enquanto o drawer está aberto.
  const fundo = [principal, ...document.querySelectorAll(".pular-para-conteudo")];
  const definirFundoInerte = (inerte) => fundo.forEach((elemento) => (elemento.inert = inerte));

  function abrirDrawer() {
    sidebar.dataset.aberta = "";
    overlay.hidden = false;
    definirFundoInerte(true);
    raiz.classList.add("overflow-hidden");
    botaoAbrir.setAttribute("aria-expanded", "true");
    sidebar.querySelector("a[href], button:not([disabled])")?.focus();
  }

  function fecharDrawer({ devolverFoco = true } = {}) {
    if (!drawerAberto()) return;

    delete sidebar.dataset.aberta;
    overlay.hidden = true;
    definirFundoInerte(false);
    raiz.classList.remove("overflow-hidden");
    botaoAbrir.setAttribute("aria-expanded", "false");
    if (devolverFoco) botaoAbrir.focus();
  }

  botaoAbrir.addEventListener("click", abrirDrawer);

  for (const fechar of document.querySelectorAll("[data-sidebar-fechar]")) {
    fechar.addEventListener("click", () => fecharDrawer());
  }

  sidebar.addEventListener("click", (e) => {
    if (e.target.closest("a[href]")) fecharDrawer({ devolverFoco: false });
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && drawerAberto()) fecharDrawer();
  });

  desktop.addEventListener("change", () => {
    fecharDrawer({ devolverFoco: false });
    esconderTooltip();
  });
}
