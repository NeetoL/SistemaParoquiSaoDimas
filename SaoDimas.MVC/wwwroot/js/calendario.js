
const raiz = document.querySelector("[data-calendario]");
if (raiz) iniciar().catch(() => {
  raiz.setAttribute("aria-busy", "false");
  raiz.querySelector("[data-calendario-aviso]").textContent = "Não foi possível carregar o calendário. Recarregue a página para tentar novamente.";
});

async function iniciar() {
  const { obterCalendarioBrasil } = await import("./calendario-brasil.js");
  const $ = seletor => raiz.querySelector(seletor);
  const grade = $("[data-calendario-grade]");
  const detalhes = $("[data-calendario-detalhes]");
  const resultados = $("[data-calendario-resultados]");
  const busca = $("#calendario-busca");
  const mes = $("#calendario-mes");
  const ano = $("#calendario-ano");
  const aviso = $("[data-calendario-aviso]");
  const hoje = new Intl.DateTimeFormat("sv-SE", { timeZone: "America/Sao_Paulo", year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date());
  let selecionada = hoje;
  let exibido = dataLocal(hoje);
  let calendario = {};
  let revisao = 0;
  const cache = new Map();
  const formatoDia = new Intl.DateTimeFormat("pt-BR", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
  const normalizar = texto => texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
  for (let m = 0; m < 12; m++) {
    const option = document.createElement("option");
    option.value = m;
    option.textContent = new Intl.DateTimeFormat("pt-BR", { month: "long" }).format(new Date(2026, m, 1));
    mes.append(option);
  }
  function elemento(tag, classe, texto) {
    const el = document.createElement(tag);
    if (classe) el.className = classe;
    if (texto !== undefined) el.textContent = texto;
    return el;
  }
  function chave(data) { return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2,"0")}-${String(data.getDate()).padStart(2,"0")}`; }
  function dataLocal(valor) { const [a,m,d] = valor.split("-").map(Number); return new Date(a,m-1,d,12); }
  function cor(dia) { const cores = ["WHITE","RED","PURPLE","GREEN","ROSE","BLACK"]; return cores.includes(dia.colors?.[0]) ? dia.colors[0].toLowerCase() : "white"; }
  function resumo(dia) { return dia.name || "Celebração do dia"; }
  function mostrarDia(valor) {
    selecionada = valor;
    detalhes.replaceChildren();
    detalhes.append(elemento("span","sobrelinha",valor === hoje ? "HOJE NA IGREJA" : "MEMÓRIA E CELEBRAÇÃO"),elemento("h2","calendario-data",formatoDia.format(dataLocal(valor))));
    const celebracoes = calendario[valor] || [];
    for (const [indice,dia] of celebracoes.entries()) {
      const card = elemento("article","celebracao");
      const linha = elemento("div","celebracao-categoria");
      linha.append(elemento("i",`liturgia-cor cor-${cor(dia)}`),elemento("span",null,dia.rankName || dia.rank));
      card.append(linha,elemento("h3",null,resumo(dia)));
      if (indice === 0) card.append(elemento("p","celebracao-tempo",`${(dia.seasonNames || []).join(" · ")} · ${(dia.colorNames || []).join(" / ")}`));
      if (dia.isOptional) card.append(elemento("p","celebracao-tempo","Memória facultativa: pode ser celebrada conforme as normas litúrgicas."));
      detalhes.append(card);
    }
    if (!celebracoes.length) detalhes.append(elemento("p","text-muted","Consulte as memórias deste dia no Martirológio."));
    $("[data-martirologio]").href = `https://liturgia.pt/martirologio/elogio.php?data=${valor}`;
    grade.querySelectorAll("button").forEach(botao => botao.setAttribute("aria-pressed",String(botao.dataset.data === valor)));
  }
  function desenhar() {
    grade.replaceChildren();
    const primeiro = new Date(exibido.getFullYear(),exibido.getMonth(),1,12);
    const dias = new Date(exibido.getFullYear(),exibido.getMonth()+1,0).getDate();
    for (let i=0; i<primeiro.getDay(); i++) { const vazio=elemento("div","calendario-celula-vazia"); vazio.setAttribute("aria-hidden","true"); grade.append(vazio); }
    for (let d=1;d<=dias;d++) {
      const valor = chave(new Date(exibido.getFullYear(),exibido.getMonth(),d,12));
      const lista = calendario[valor] || [];
      const principal = lista[0]?.rank === "WEEKDAY" && lista.length > 1 ? lista[1] : lista[0];
      const botao = elemento("button","calendario-celula");
      botao.type="button"; botao.dataset.data=valor;
      botao.setAttribute("aria-pressed",String(valor===selecionada));
      botao.setAttribute("aria-label",`${formatoDia.format(dataLocal(valor))}${valor===hoje?", hoje":""}: ${lista.map(resumo).join("; ")}`);
      if (valor===hoje) botao.setAttribute("aria-current","date");
      const numero=elemento("span","calendario-numero",String(d));
      botao.append(numero);
      if (principal) {
        botao.append(elemento("i",`liturgia-cor cor-${cor(principal)}`));
        botao.append(elemento("span","calendario-nome",resumo(principal)));
        if (principal.isOptional || lista.length>1) botao.append(elemento("small","calendario-mais",`${principal.isOptional ? "Facultativa" : ""}${principal.isOptional && lista.length>1 ? " · " : ""}${lista.length>1 ? `+${lista.length-1}` : ""}`));
      }
      grade.append(botao);
    }
    mostrarDia(selecionada);
    pesquisar();
  }
  function pesquisar() {
    const termo=normalizar(busca.value.trim());
    resultados.replaceChildren(); resultados.hidden=!termo; grade.hidden=!!termo;
    $(".calendario-semana").hidden=!!termo;
    $("[data-calendario-limpar]").hidden=!termo;
    if (!termo) { aviso.textContent=`${new Intl.DateTimeFormat("pt-BR",{month:"long",year:"numeric"}).format(exibido)} · Selecione um dia para ver os detalhes.`; return; }
    const encontrados=Object.entries(calendario).flatMap(([data,lista])=>lista.filter(dia=>normalizar(resumo(dia)).includes(termo)).map(dia=>({data,dia})));
    aviso.textContent=`${encontrados.length} resultado${encontrados.length===1?"":"s"} em ${exibido.getFullYear()}.`;
    for (const {data,dia} of encontrados) {
      const botao=elemento("button","calendario-resultado");botao.type="button";botao.dataset.data=data;
      botao.append(elemento("span","resultado-data",dataLocal(data).toLocaleDateString("pt-BR",{day:"2-digit",month:"2-digit"})),elemento("span",null,resumo(dia)),elemento("small",null,dia.rankName));
      resultados.append(botao);
    }
    if (!encontrados.length) resultados.append(elemento("p","calendario-nota","Nenhuma celebração encontrada neste ano. Outros santos e beatos podem ser consultados no Martirológio do dia."));
  }
  async function carregar() {
    const versao=++revisao; const a=exibido.getFullYear();
    mes.value=exibido.getMonth(); ano.value=a;
    raiz.setAttribute("aria-busy","true"); aviso.textContent="Preparando o calendário…";
    if (!cache.has(a)) cache.set(a,obterCalendarioBrasil(a));
    const dados=await cache.get(a);
    if (versao!==revisao) return;
    calendario=dados;
    raiz.setAttribute("aria-busy","false");
    $("[data-calendario-anterior]").disabled=a===2000 && exibido.getMonth()===0;
    $("[data-calendario-proximo]").disabled=a===2100 && exibido.getMonth()===11;
    desenhar();
  }
  async function mover(delta) {
    const data=new Date(exibido.getFullYear(),exibido.getMonth()+delta,1,12);
    if (data.getFullYear()<2000 || data.getFullYear()>2100) return;
    exibido=data;selecionada=chave(data);await carregar();
  }
  function acao(operacao) { return () => operacao().catch(()=>{raiz.setAttribute("aria-busy","false");aviso.textContent="Não foi possível preparar este período. Recarregue a página para tentar novamente.";}); }
  $("[data-calendario-anterior]").addEventListener("click",acao(()=>mover(-1)));
  $("[data-calendario-proximo]").addEventListener("click",acao(()=>mover(1)));
  $("[data-calendario-hoje]").addEventListener("click",acao(async()=>{exibido=dataLocal(hoje);selecionada=hoje;busca.value="";await carregar();}));
  mes.addEventListener("change",acao(async()=>{exibido=new Date(exibido.getFullYear(),Number(mes.value),1,12);selecionada=chave(exibido);await carregar();}));
  ano.addEventListener("change",acao(async()=>{const valor=Number(ano.value);if(!Number.isInteger(valor)||valor<2000||valor>2100){ano.value=exibido.getFullYear();return;}exibido=new Date(valor,exibido.getMonth(),1,12);selecionada=chave(exibido);await carregar();}));
  busca.addEventListener("input",pesquisar);
  $("[data-calendario-limpar]").addEventListener("click",()=>{busca.value="";pesquisar();busca.focus();});
  raiz.addEventListener("click",event=>{const botao=event.target.closest("button[data-data]");if(botao){mostrarDia(botao.dataset.data);if(botao.closest("[data-calendario-resultados]")) detalhes.scrollIntoView({block:"nearest"});}});
  grade.addEventListener("keydown",async event=>{
    const alvo=event.target.closest("button[data-data]");if(!alvo)return;
    const incrementos={ArrowLeft:-1,ArrowRight:1,ArrowUp:-7,ArrowDown:7};
    if (!(event.key in incrementos)) return;
    event.preventDefault();const data=dataLocal(alvo.dataset.data);data.setDate(data.getDate()+incrementos[event.key]);
    if(data.getFullYear()<2000||data.getFullYear()>2100)return;
    selecionada=chave(data);
    if(data.getMonth()!==exibido.getMonth()||data.getFullYear()!==exibido.getFullYear()){exibido=data;await carregar();}else mostrarDia(selecionada);
    grade.querySelector(`[data-data="${selecionada}"]`)?.focus();
  });
  await carregar();
}
