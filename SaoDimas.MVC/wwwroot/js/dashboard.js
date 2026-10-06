const painel = document.querySelector("[data-santos-painel]");
if (painel) {
  const lista = painel.querySelector("[data-santos-lista]");
  let calendario;
  let selecionada = painel.dataset.hoje;
  function exibir(data) {
    selecionada = data;
    const dia = new Date(data + "T12:00:00");
    painel.querySelector("[data-santos-data]").textContent = new Intl.DateTimeFormat("pt-BR", {weekday:"long",day:"numeric",month:"long"}).format(dia);
    painel.querySelector("[data-santos-martirologio]").href = "https://liturgia.pt/martirologio/elogio.php?data=" + data;
    painel.querySelectorAll(".dashboard-semana a").forEach(a => a.setAttribute("aria-current",new URL(a.href).searchParams.get("de")===data?"date":"false"));
    if (!calendario) return;
    lista.replaceChildren();
    const celebracoes = calendario[data] ?? [];
    for (const celebracao of celebracoes) {
      const artigo = document.createElement("article");
      artigo.className = "dashboard-santo";
      const categoria = document.createElement("small");
      categoria.textContent = celebracao.rankName || celebracao.rank;
      const nome = document.createElement("strong");
      nome.textContent = celebracao.name;
      artigo.append(categoria,nome);
      lista.append(artigo);
    }
    if (!celebracoes.length) {
      const mensagem = document.createElement("p");
      mensagem.className = "dashboard-nota";
      mensagem.textContent = "Sem celebração registrada nesta data. Consulte os santos no Martirológio.";
      lista.append(mensagem);
    }
    lista.setAttribute("aria-busy","false");
  }
  painel.querySelectorAll(".dashboard-semana a").forEach(a=>a.addEventListener("click",e=>{
    if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
    e.preventDefault();
    exibir(new URL(a.href).searchParams.get("de"));
  }));
  import("./calendario-brasil.js").then(({obterCalendarioBrasil})=>obterCalendarioBrasil(Number(painel.dataset.hoje.slice(0,4)))).then(async dados=>{
    calendario = dados;
    const ultimo = painel.querySelector(".dashboard-semana a:last-child");
    const outroAno = Number(new URL(ultimo.href).searchParams.get("de").slice(0,4));
    if(outroAno !== Number(painel.dataset.hoje.slice(0,4))) {
      const {obterCalendarioBrasil} = await import("./calendario-brasil.js");
      calendario = {...calendario,...await obterCalendarioBrasil(outroAno)};
    }
    exibir(selecionada);
  }).catch(()=>{
    lista.textContent="Não foi possível carregar as celebrações. Abra o calendário da fé ou consulte o Martirológio.";
    lista.setAttribute("aria-busy","false");
  });
}
const financeiro = document.querySelector("[data-financeiro-painel]");
if(financeiro) {
  financeiro.querySelectorAll("button[data-grafico-modo]").forEach(botao=>botao.addEventListener("click",()=>{
    financeiro.dataset.graficoModo=botao.dataset.graficoModo;
    financeiro.querySelectorAll("button[data-grafico-modo]").forEach(b=>b.setAttribute("aria-pressed",String(b===botao)));
  }));
  financeiro.querySelectorAll(".dashboard-grafico-mes").forEach(botao=>botao.addEventListener("click",()=>{
    financeiro.querySelectorAll(".dashboard-grafico-mes").forEach(b=>b.setAttribute("aria-pressed",String(b===botao)));
    for(const campo of ["periodo","entradas","saidas","pagamentos"]) {
      const el=financeiro.querySelector("[data-financeiro-"+campo+"]");
      if(el) el.textContent=botao.dataset[campo];
    }
  }));
}
