(() => {
    "use strict";

    const modalAprobar = document.getElementById("modal-aprobar");
    const modalRechazar = document.getElementById("modal-rechazar");
    const motivo = document.getElementById("rechazo-motivo");
    const confirmarRechazo = document.getElementById("confirmar-rechazo");
    const contador = document.getElementById("rechazo-contador");
    const ayuda = document.getElementById("rechazo-ayuda");
    let formularioAprobar = null;
    let enviando = false;

    const rellenar = (modal, fila) => {
        modal.querySelector("[data-resumen]").textContent = `${fila.dataset.estudiante} · ${fila.dataset.equipo}`;
        modal.querySelector("[data-horario]").textContent = fila.dataset.horario;
    };

    const bloquear = () => {
        enviando = true;
        document.querySelectorAll("button").forEach(boton => { boton.disabled = true; });
    };

    document.querySelectorAll(".js-aprobar").forEach(formulario => {
        formulario.addEventListener("submit", evento => {
            evento.preventDefault();
            if (enviando) {
                return;
            }
            formularioAprobar = formulario;
            rellenar(modalAprobar, formulario.closest("tr"));
            modalAprobar.showModal();
        });
    });

    document.getElementById("confirmar-aprobar")?.addEventListener("click", evento => {
        if (!formularioAprobar || enviando) {
            return;
        }
        evento.currentTarget.textContent = "Aprobando…";
        bloquear();
        // submit() no dispara el evento, así que no vuelve a abrir el modal.
        formularioAprobar.submit();
    });

    const actualizarMotivo = () => {
        const escrito = motivo.value.trim().length > 0;
        confirmarRechazo.disabled = !escrito;
        contador.textContent = `${motivo.value.length} / 200`;
        ayuda.textContent = escrito
            ? "El estudiante verá este motivo en «Mis reservas»."
            : "Escribe un motivo para habilitar «Confirmar rechazo».";
    };
    motivo?.addEventListener("input", actualizarMotivo);

    // Sin JavaScript, el enlace abre la página de rechazo.
    document.querySelectorAll(".js-rechazar").forEach(enlace => {
        enlace.addEventListener("click", evento => {
            evento.preventDefault();
            if (enviando) {
                return;
            }
            const fila = enlace.closest("tr");
            rellenar(modalRechazar, fila);
            document.getElementById("rechazo-id").value = fila.dataset.id;
            motivo.value = "";
            actualizarMotivo();
            modalRechazar.showModal();
            motivo.focus();
        });
    });

    document.getElementById("form-rechazar")?.addEventListener("submit", () => {
        confirmarRechazo.textContent = "Rechazando…";
        bloquear();
    });

    document.querySelectorAll("[data-cerrar]").forEach(boton => {
        boton.addEventListener("click", () => boton.closest("dialog").close());
    });
})();
