const campo = document.getElementById("numeros");
const botoes = [...document.querySelectorAll("[data-rifa-numero]")];
const nota = document.querySelector("[data-rifa-selecao]");
function selecionados() { return new Set((campo?.value ?? "").split(/[,;\s]+/).filter(Boolean).map(Number)); }
function atualizar() {
 const numeros = selecionados();
 for (const botao of botoes) botao.setAttribute("aria-pressed", String(botao.dataset.rifaEstado === "livre" && numeros.has(Number(botao.dataset.rifaNumero))));
 if (nota) nota.textContent = numeros.size ? `${numeros.size} número(s) selecionado(s). Preencha o comprador para reservar.` : campo ? "Escolha os números livres para preencher a reserva." : "Vendas fechadas. Clique nos números ocupados para consultar o comprador.";
}
campo?.addEventListener("input", atualizar);
for (const botao of botoes) botao.addEventListener("click", () => {
 const numero = Number(botao.dataset.rifaNumero);
 if (botao.dataset.rifaEstado !== "livre") { const linha = document.getElementById(`rifa-numero-${numero}`); if (linha) { linha.closest("details").open = true; linha.scrollIntoView({block:"center",behavior:matchMedia("(prefers-reduced-motion: reduce)").matches?"instant":"smooth"}); } return; }
 if (!campo) return;
 const numeros = selecionados();
 if (numeros.has(numero)) numeros.delete(numero); else { if (numeros.size >= 100) { nota.textContent = "Selecione até 100 números por reserva."; return; } numeros.add(numero); }
 campo.value = [...numeros].sort((a,b)=>a-b).join(", "); atualizar();
});
atualizar();
