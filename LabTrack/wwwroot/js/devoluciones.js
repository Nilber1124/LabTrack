(() => {
    "use strict";

    const modal = document.getElementById("modal-devolucion");
    const titulo = document.getElementById("devolucion-titulo");
    const resumen = document.getElementById("devolucion-resumen");
    const tardia = document.getElementById("devolucion-tardia");
    const fin = document.getElementById("devolucion-fin");
    const aTiempo = document.getElementById("devolucion-a-tiempo");
    const confirmar = document.getElementById("confirmar-devolucion");
    let formularioActual = null;

    document.querySelectorAll(".js-devolver").forEach(formulario => {
        formulario.addEventListener("submit", evento => {
            evento.preventDefault();
            const fila = formulario.closest("tr");
            const vencida = fila.dataset.vencida === "true";

            // Solo es un aviso: el servidor decide la penalización con su propia hora.
            formularioActual = formulario;
            resumen.textContent = `${fila.dataset.estudiante} · ${fila.dataset.equipo}`;
            titulo.textContent = vencida ? "¿Confirmar devolución tardía?" : "¿Confirmar devolución?";
            fin.textContent = `Fin programado ${fila.dataset.fin}.`;
            tardia.hidden = !vencida;
            aTiempo.hidden = vencida;
            confirmar.textContent = vencida ? "Confirmar y penalizar" : "Confirmar devolución";
            modal.showModal();
        });
    });

    confirmar?.addEventListener("click", () => {
        if (!formularioActual) {
            return;
        }
        confirmar.disabled = true;
        formularioActual.submit();
    });

    modal?.querySelector("[data-cerrar]").addEventListener("click", () => modal.close());
})();
