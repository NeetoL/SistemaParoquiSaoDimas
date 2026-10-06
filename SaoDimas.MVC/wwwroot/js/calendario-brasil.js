import { Romcal } from "../lib/romcal/romcal.js";
import { Brazil_PtBr } from "../lib/romcal/brazil.js";

// Missal do Brasil: Epifania e Ascensão no domingo; Corpus Christi na quinta-feira.
// Pedro e Paulo: domingo entre 28/06 e 04/07; Assunção: domingo em/após 15/08.
// Todos os Santos: domingo em/após 01/11, exceto quando conflita com Finados.
// Referências nacionais e edição dos dados ficam documentadas na própria tela.
export function criarCalendarioBrasil(ano) {
  const calendario = structuredClone(Brazil_PtBr);
  function fixar(id, data) {
    const entradas = calendario.inputs[id];
    calendario.inputs[id] = [{ ...Object.assign({}, ...entradas), dateDef: { month: data.getUTCMonth() + 1, date: data.getUTCDate() }, dateExceptions: [] }];
  }
  function domingoEmOuDepois(mes, dia) {
    const data = new Date(Date.UTC(ano, mes - 1, dia));
    data.setUTCDate(data.getUTCDate() + (7 - data.getUTCDay()) % 7);
    return data;
  }
  fixar("peter_and_paul_apostles", domingoEmOuDepois(6, 28));
  fixar("assumption_of_the_blessed_virgin_mary", domingoEmOuDepois(8, 15));
  const todosSantos = domingoEmOuDepois(11, 1);
  fixar("all_saints", todosSantos.getUTCDate() === 2 ? new Date(Date.UTC(ano, 10, 1)) : todosSantos);
  return new Romcal({ localizedCalendar: calendario, epiphanyOnSunday: true, ascensionOnSunday: true, corpusChristiOnSunday: false });
}

export async function obterCalendarioBrasil(ano) {
  if (!Number.isInteger(ano) || ano < 2000 || ano > 2100) throw new RangeError("Ano fora do intervalo do calendário.");
  const romcal = criarCalendarioBrasil(ano);
  const calendario = await romcal.generateCalendar(ano);
  // No Brasil, a Imaculada Conceição permanece em 8/12 mesmo no domingo do Advento.
  // https://www.cnbb.org.br/imaculada-conceicao-2o-domingo-advento/
  if (new Date(Date.UTC(ano, 11, 8)).getUTCDay() === 0) {
    const oito = `${ano}-12-08`, nove = `${ano}-12-09`;
    const imaculada = calendario[nove].find(dia => dia.id === "immaculate_conception_of_the_blessed_virgin_mary");
    if (imaculada) {
      calendario[oito] = [{ id: imaculada.id, date: oito, name: imaculada.name, rank: imaculada.rank, rankName: imaculada.rankName, colors: imaculada.colors, colorNames: imaculada.colorNames, seasonNames: imaculada.seasonNames, isOptional: false }];
      const restantes = calendario[nove].filter(dia => dia !== imaculada);
      const juanDiego = await romcal.getOneLiturgicalDay("juan_diego_cuauhtlatoatzin", { year: ano, computeInWholeYear: false });
      calendario[nove] = [imaculada.weekday, ...restantes, ...(juanDiego ? [juanDiego] : [])].filter(Boolean);
    }
  }
  return calendario;
}
