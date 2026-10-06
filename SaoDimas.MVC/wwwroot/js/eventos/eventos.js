// Eventos são persistidos no JSON do servidor; o documento local serve apenas à importação do legado.
// - envia o legado na primeira requisição e protege as operações com o token antiforgery;
// - abre/fecha o modal do módulo e mostra erros como toast.
import { lerDocumento } from "./armazenamento.js";
import { mostrarToast } from "../toast.js";

const CAMPO = "__estadoEventos";
let operadorAtual = "";
let legadoPendente = true;

const raiz = document.querySelector("[data-eventos]");
const token = raiz?.querySelector("input[name='__RequestVerificationToken']")?.value;
const modal = document.getElementById("modal-eventos");

const doModulo = (elemento) => elemento instanceof Element && elemento.closest("[data-eventos]") !== null;

// Mantém a ordem das operações da interface; a persistência controla a concorrência no servidor.
raiz?.setAttribute("hx-sync", "[data-eventos]:queue all");

const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const lerValor = (valor) => {
  const texto = String(valor).replace(/R\$|\s/g, "");
  const ponto = texto.indexOf(".");
  const decimalPonto = !texto.includes(",") && ponto >= 0 && ponto === texto.lastIndexOf(".") && texto.length - ponto <= 3;
  return Number(decimalPonto ? texto : texto.replaceAll(".", "").replace(",", "."));
};
const semAcentos = (texto) => texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("pt-BR");

document.addEventListener("input", (evento) => {
  const elemento = evento.target;
  if (!(elemento instanceof HTMLInputElement)) return;
  if (elemento.matches("[data-busca-responsavel]")) {
    const central = elemento.closest("[data-central-prestacao]");
    let encontrados = 0;
    central.querySelectorAll("[data-conta-responsavel]").forEach((card) => {
      const visivel = semAcentos(card.dataset.contaResponsavel).includes(semAcentos(elemento.value));
      card.hidden = !visivel;
      if (visivel) encontrados++;
    });
    central.querySelector("[data-busca-vazia]").classList.toggle("hidden", encontrados > 0);
  }
  const prestacao = elemento.closest("[data-prestacao-preview]");
  if (prestacao) {
    const entregue = lerValor(prestacao.querySelector("[name='entregue']").value);
    const saldo = Number(prestacao.dataset.saldo) - entregue;
    prestacao.querySelector("[data-preview-saida]").textContent = !Number.isFinite(entregue) || entregue < 0 ? "Informe um valor válido." :
      `Após este lançamento: prestado ${moeda.format(Number(prestacao.dataset.prestado) + entregue)}, saldo ${moeda.format(saldo)} · ${saldo === 0 ? "Quitado" : saldo < 0 ? "Valor acima do saldo" : "Parcial"}.`;
  }
  const contagem = elemento.closest("[data-contagem-caixa]");
  if (contagem) {
    const contado = contagem.querySelector("[name='contado']");
    if (elemento.matches("[data-cedula]")) {
      const centavos = [...contagem.querySelectorAll("[data-cedula]")].reduce((total, campo) =>
        total + Math.round(Number(campo.dataset.cedula) * 100) * Math.max(0, Math.trunc(Number(campo.value) || 0)), 0);
      contado.value = (centavos / 100).toFixed(2).replace(".", ",");
    }
    const diferenca = lerValor(contado.value) - Number(contagem.dataset.teorico);
    contagem.querySelector("[data-diferenca-caixa]").textContent = `Diferença: ${moeda.format(diferenca)}`;
    contagem.querySelector("[name='justificativa']").required = diferenca !== 0;
  }
});

document.addEventListener("change", (evento) => {
  const seletor = evento.target;
  if (!(seletor instanceof HTMLSelectElement) || !seletor.matches("[data-modelo-edicao]")) return;
  const formulario = seletor.closest("form");
  formulario.setAttribute("hx-post", seletor.value);
  window.htmx.process(formulario);
});

document.body.addEventListener("htmx:configRequest", (evento) => {
  if (!doModulo(evento.detail.elt)) return;

  if (legadoPendente) evento.detail.parameters[CAMPO] = lerDocumento();
  const operador = evento.detail.parameters.operador ?? evento.detail.parameters.Operador;
  if (operador) operadorAtual = operador;
  if (!operador && /\/(Status|Remover)$/.test(evento.detail.elt.getAttribute("hx-post") || ""))
    evento.detail.parameters.operador = raiz.querySelector("[data-operador-acoes]")?.value || operadorAtual;
  if (token) evento.detail.headers["RequestVerificationToken"] = token;
});

document.body.addEventListener("htmx:afterSwap", () => {
  if (operadorAtual) raiz?.querySelectorAll("input[name='operador'],input[name='Operador']").forEach((campo) => {
    if (!campo.value) campo.value = operadorAtual;
  });
});

document.body.addEventListener("htmx:beforeOnLoad", (evento) => {
  if (evento.detail.xhr.getResponseHeader("X-Persistencia-Eventos") === "servidor") legadoPendente = false;
});

document.body.addEventListener("htmx:responseError", (evento) => {
  if (!doModulo(evento.detail.elt)) return;
  mostrarToast(evento.detail.xhr.responseText || "Não foi possível concluir a operação.", "error");
});

document.body.addEventListener("htmx:sendError", () =>
  mostrarToast("Sem conexão com o servidor. Tente novamente.", "error"));

// Formulários comuns também podem iniciar a importação do legado.
document.addEventListener("submit", (evento) => {
  const formulario = evento.target;
  if (!(formulario instanceof HTMLFormElement) || !formulario.matches("[data-eventos-documento]")) return;

  formulario.querySelector(`input[name='${CAMPO}']`).value = legadoPendente ? lerDocumento() : "";
}, true);

document.body.addEventListener("eventos:abrir-modal", () => {
  if (modal && !modal.open) modal.showModal();
});

document.body.addEventListener("eventos:fechar-modal", () => modal?.close());

// Quantidade de tickets de uma faixa (apenas exibição; a validação é sempre do servidor).
document.addEventListener("input", (evento) => {
  const area = evento.target instanceof Element ? evento.target.closest("[data-faixa-quantidade]") : null;
  if (!area) return;

  const inicial = Number(area.querySelector("[name='NumeroInicial']")?.value);
  const final = Number(area.querySelector("[name='NumeroFinal']")?.value);
  const saida = area.querySelector("[data-faixa-quantidade-saida]");
  if (!saida) return;

  saida.textContent = inicial > 0 && final >= inicial ? `${final - inicial + 1} tickets` : "—";
});
