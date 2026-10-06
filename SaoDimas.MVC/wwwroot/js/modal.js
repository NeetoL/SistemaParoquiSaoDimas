/**
 * Modais baseados em <dialog class="modal"> (focus trap, ESC e backdrop nativos).
 *
 *   <button type="button" data-modal-abrir="modal-exemplo">Abrir</button>
 *   <dialog id="modal-exemplo" class="modal" aria-labelledby="modal-exemplo-titulo"> ...
 *     <button type="button" data-modal-fechar>Cancelar</button>
 *   </dialog>
 */
export function iniciarModais() {
  document.addEventListener("click", (e) => {
    const abrir = e.target.closest("[data-modal-abrir]");
    if (abrir) {
      document.getElementById(abrir.dataset.modalAbrir)?.showModal();
      return;
    }

    const fechar = e.target.closest("[data-modal-fechar]");
    if (fechar) {
      fechar.closest("dialog")?.close();
      return;
    }

    // Clique no backdrop (fora do conteúdo do dialog) fecha o modal.
    if (e.target instanceof HTMLDialogElement && e.target.classList.contains("modal")) {
      const area = e.target.getBoundingClientRect();
      const dentro = e.clientX >= area.left && e.clientX <= area.right && e.clientY >= area.top && e.clientY <= area.bottom;
      if (!dentro) e.target.close();
    }
  });
}
