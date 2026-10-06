import { test } from "node:test";
import assert from "node:assert/strict";
import { obterCalendarioBrasil } from "../wwwroot/js/calendario-brasil.js";

const cache = new Map();
function calendario(ano) { if (!cache.has(ano)) cache.set(ano, obterCalendarioBrasil(ano)); return cache.get(ano); }
function tem(dias, data, id) { assert.ok(dias[data].some(dia => dia.id === id), `${id} em ${data}`); }
test("Datas móveis e próprias do Brasil em 2026", async () => {
  const dias = await calendario(2026);
  tem(dias, "2026-01-04", "epiphany_of_the_lord");
  tem(dias, "2026-04-05", "easter_sunday");
  tem(dias, "2026-05-17", "ascension_of_the_lord");
  tem(dias, "2026-06-04", "most_holy_body_and_blood_of_christ");
  tem(dias, "2026-06-28", "peter_and_paul_apostles");
  tem(dias, "2026-08-16", "assumption_of_the_blessed_virgin_mary");
  tem(dias, "2026-10-12", "our_lady_of_aparecida");
  tem(dias, "2026-11-01", "all_saints");
  assert.match(dias["2026-10-01"][0].name, /Teresa|Teresinha/);
});
test("Finados no domingo conserva Todos os Santos em 1/11", async () => {
  const dias = await calendario(2025);
  tem(dias, "2025-11-01", "all_saints");
  tem(dias, "2025-11-02", "commemoration_of_all_the_faithful_departed");
});
test("Pedro e Paulo atravessa o mês e Assunção já no domingo", async () => {
  const dias = await calendario(2027);
  tem(dias, "2027-07-04", "peter_and_paul_apostles");
  tem(dias, "2027-08-15", "assumption_of_the_blessed_virgin_mary");
  tem(dias, "2027-11-07", "all_saints");
});
test("Ano bissexto e Imaculada Conceição no domingo do Advento brasileiro", async () => {
  const dias = await calendario(2024);
  assert.equal(Object.keys(dias).length, 366);
  assert.ok(dias["2024-02-29"].length);
  tem(dias, "2024-12-08", "immaculate_conception_of_the_blessed_virgin_mary");
  assert.ok(!dias["2024-12-09"].some(dia => dia.id === "immaculate_conception_of_the_blessed_virgin_mary"));
  assert.ok(dias["2024-12-09"].length);
});
test("Mostra várias memórias facultativas na mesma data", async () => {
  const dias = await calendario(2026);
  tem(dias, "2026-01-20", "fabian_i_pope");
  tem(dias, "2026-01-20", "sebastian_of_milan_martyr");
});
test("Rejeita anos inválidos", async () => {
  for (const ano of [1999, 2101, NaN, 2026.5]) await assert.rejects(obterCalendarioBrasil(ano), RangeError);
});
