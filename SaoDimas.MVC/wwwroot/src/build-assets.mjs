// Gera os assets estáticos derivados de node_modules em wwwroot/ (não versionados).
//
// - icons/lucide.svg : sprite somente com os ícones Lucide usados nas views.
// - fonts/           : Rubik variável (subset latin), servida localmente.
// - lib/htmx/        : HTMX, disponível para uso futuro.
// - img/             : brasão otimizado e favicon, a partir da logo oficial (wwwroot/src/marca).

import { copyFile, mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const wwwroot = join(raiz, "wwwroot");
const pastaDeIcones = join(raiz, "node_modules", "lucide-static", "icons");

async function* views(diretorio) {
  for (const entrada of await readdir(diretorio, { withFileTypes: true })) {
    const caminho = join(diretorio, entrada.name);
    if (entrada.isDirectory()) yield* views(caminho);
    else if (entrada.name.endsWith(".cshtml")) yield caminho;
  }
}

// Ícones usados: lucide.svg#nome nas views e textos entre aspas nas views que sejam nomes de ícones Lucide
// (ex.: Item("Dizimistas", "users", ...) no _Sidebar). Referência a ícone inexistente interrompe o build.
async function iconesUsados() {
  const disponiveis = new Set((await readdir(pastaDeIcones)).map((arquivo) => arquivo.replace(/\.svg$/, "")));
  const nomes = new Set(["hand-coins","wallet","calendar-clock","book-open","church","heart-handshake","list-checks"]);

  for await (const arquivo of views(join(raiz, "Views"))) {
    const conteudo = await readFile(arquivo, "utf8");

    for (const [, nome] of conteudo.matchAll(/lucide\.svg#([a-z0-9-]+)"/g)) {
      if (!disponiveis.has(nome)) throw new Error(`Ícone Lucide inexistente: "${nome}" em ${relative(raiz, arquivo)} (ver https://lucide.dev/icons)`);
      nomes.add(nome);
    }
    for (const [, texto] of conteudo.matchAll(/"([a-z][a-z0-9-]*)"/g)) {
      if (disponiveis.has(texto)) nomes.add(texto);
    }
  }

  return [...nomes].sort();
}

async function gerarSpriteDeIcones() {
  const nomes = await iconesUsados();

  const simbolos = [];
  for (const nome of nomes) {
    const svg = await readFile(join(pastaDeIcones, `${nome}.svg`), "utf8");
    const conteudo = svg.slice(svg.indexOf(">", svg.indexOf("<svg")) + 1, svg.lastIndexOf("</svg>")).trim();
    simbolos.push(`<symbol id="${nome}" viewBox="0 0 24 24">${conteudo.replace(/\s*\n\s*/g, "")}</symbol>`);
  }

  await mkdir(join(wwwroot, "icons"), { recursive: true });
  await writeFile(
    join(wwwroot, "icons", "lucide.svg"),
    `<!-- Lucide (ISC) https://lucide.dev --><svg xmlns="http://www.w3.org/2000/svg">${simbolos.join("")}</svg>\n`
  );
  console.log(`icons/lucide.svg: ${simbolos.length} ícones`);
}

async function copiar(origem, destino) {
  await mkdir(dirname(destino), { recursive: true });
  await copyFile(origem, destino);
  console.log(relative(raiz, destino));
}

async function gerarImagensDaMarca() {
  const logoOficial = join(wwwroot, "src", "marca", "logo-paroquia-sao-dimas.png");
  const destino = join(wwwroot, "img");
  await mkdir(destino, { recursive: true });

  // Brasão completo, sem margens transparentes. Altura 2x a maior exibição (144px) para telas de alta densidade.
  await sharp(logoOficial)
    .trim()
    .resize({ height: 288 })
    .webp({ quality: 90, alphaQuality: 100, effort: 6 })
    .toFile(join(destino, "brasao.webp"));

  // Favicon: somente o símbolo (cruz processional + escudo), sem a faixa, que fica ilegível em 16–32px.
  // Recorte em coordenadas da imagem original (1254×1254).
  await sharp(logoOficial)
    .extract({ left: 305, top: 15, width: 655, height: 800 })
    .resize(64, 64, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 })
    .toFile(join(destino, "favicon.png"));

  // Logo para documentos impressos (Carta de Dízimo): PNG transparente, cores originais, ~580 dpi a 26 mm.
  await sharp(logoOficial)
    .trim()
    .resize({ height: 600 })
    .png({ compressionLevel: 9 })
    .toFile(join(destino, "brasao-impressao.png"));

  console.log("img/brasao.webp, img/favicon.png, img/brasao-impressao.png");
}

await gerarSpriteDeIcones();
await gerarImagensDaMarca();
// Recursos originais do envelope: copiar sem transformar os bytes ou a aparência.
await copiar(join(wwwroot, "src", "marca", "dizimo-expressao-fe.jpeg"), join(wwwroot, "img", "dizimo-expressao-fe.jpeg"));
await copiar(join(wwwroot, "src", "marca", "pix-qrcode.png"), join(wwwroot, "img", "pix-qrcode.png"));
await copiar(
  join(raiz, "node_modules", "@fontsource-variable", "rubik", "files", "rubik-latin-wght-normal.woff2"),
  join(wwwroot, "fonts", "rubik-latin-wght-normal.woff2")
);
await copiar(join(raiz, "node_modules", "htmx.org", "dist", "htmx.min.js"), join(wwwroot, "lib", "htmx", "htmx.min.js"));


const bibliotecaRomcal = await readFile(join(raiz, "node_modules", "romcal", "rites", "roman1969", "dist", "esm", "romcal.js"), "utf8");
await mkdir(join(wwwroot, "lib", "romcal"), { recursive: true });
await writeFile(join(wwwroot, "lib", "romcal", "romcal.js"), bibliotecaRomcal.replace(/(["'])i18next\1/g, '"./i18next.js"'));
await copiar(join(raiz, "node_modules", "i18next", "dist", "esm", "i18next.js"), join(wwwroot, "lib", "romcal", "i18next.js"));
await copiar(join(raiz, "node_modules", "i18next", "LICENSE"), join(wwwroot, "lib", "romcal", "i18next-LICENSE.txt"));
await copiar(join(raiz, "node_modules", "@romcal", "calendar.brazil", "esm", "pt-br.js"), join(wwwroot, "lib", "romcal", "brazil.js"));
await copiar(join(raiz, "node_modules", "romcal", "LICENSE"), join(wwwroot, "lib", "romcal", "LICENSE.txt"));
