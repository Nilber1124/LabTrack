(() => {
    "use strict";

    const modal = document.getElementById("modal-levantar");
    const titulo = document.getElementById("levantar-titulo");
    const detalle = document.getElementById("levantar-detalle");
    const confirmar = document.getElementById("confirmar-levantar");
    let formularioActual = null;

    document.querySelectorAll(".js-levantar").forEach(formulario => {
        formulario.addEventListener("submit", evento => {
            evento.preventDefault();
            const fila = formulario.closest("tr");
            formularioActual = formulario;
            titulo.textContent = `¿Levantar la penalización de ${fila.dataset.nombre}?`;
            detalle.textContent = fila.dataset.detalle;
            modal.showModal();
        });
    });

    confirmar?.addEventListener("click", () => {
        if (!formularioActual) {
            return;
        }
        confirmar.disabled = true;
        confirmar.textContent = "Levantando…";
        // submit() no dispara el evento, así que no vuelve a abrir el modal.
        formularioActual.submit();
    });

    modal?.querySelector("[data-cerrar]").addEventListener("click", () => modal.close());
})();
