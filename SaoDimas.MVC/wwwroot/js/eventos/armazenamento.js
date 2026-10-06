// Leitura do documento legado para importação inicial. Novas operações ficam no JSON do servidor.
// A cópia antiga é preservada para recuperação; ela não recebe novas gravações.
const CHAVE = "saodimas.eventos.v1";
export function lerDocumento() {
  try { return localStorage.getItem(CHAVE) ?? ""; }
  catch { return ""; }
}
