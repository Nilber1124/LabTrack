(() => {
    "use strict";
    const supportsDialog = typeof HTMLDialogElement !== "undefined";
    const approveDialog = document.getElementById("confirmar-aprobacion");
    const rejectDialog = document.getElementById("confirmar-rechazo");
    let approveForm = null;
    let rejectRow = null;
    let submitting = false;

    const fillSummary = (dialog, row) => {
        dialog.querySelector("[data-modal-estudiante]").textContent =
            row.dataset.estudiante + " · " + row.dataset.equipo + " (" + row.dataset.inventario + ")";
        dialog.querySelector("[data-modal-horario]").textContent =
            row.dataset.inicio + " — " + row.dataset.fin;
    };

    const markBusy = (form, row, label) => {
        submitting = true;
        form.setAttribute("aria-busy", "true");
        if (row) {
            row.setAttribute("aria-busy", "true");
            row.querySelectorAll("button").forEach(button => { button.disabled = true; });
            row.querySelectorAll("a").forEach(link => { link.setAttribute("aria-disabled", "true"); });
        }
        document.querySelectorAll("dialog[open] button").forEach(button => { button.disabled = true; });
        const dialog = form.closest("dialog") || approveDialog;
        if (dialog) {
            const progress = dialog.querySelector(".submit-progress");
            if (progress) progress.textContent = label;
        }
        const submit = form.querySelector('button[type="submit"]');
        if (submit) submit.textContent = label;
    };

    document.querySelectorAll(".js-aprobar").forEach(form => {
        form.addEventListener("submit", event => {
            if (submitting) { event.preventDefault(); return; }
            if (form.dataset.confirmada === "true") {
                markBusy(form, form.closest("tr"), "Aprobando…");
                return;
            }
            event.preventDefault();
            if (!supportsDialog || !approveDialog) {
                if (window.confirm("¿Aprobar esta reserva?")) {
                    form.dataset.confirmada = "true";
                    form.requestSubmit();
                }
                return;
            }
            approveForm = form;
            fillSummary(approveDialog, form.closest("tr"));
            approveDialog.showModal();
        });
    });

    document.getElementById("confirmar-aprobar")?.addEventListener("click", () => {
        if (!approveForm || submitting) return;
        approveForm.dataset.confirmada = "true";
        approveForm.requestSubmit();
    });

    document.querySelectorAll(".js-rechazar").forEach(link => {
        link.addEventListener("click", event => {
            if (submitting) { event.preventDefault(); return; }
            if (!supportsDialog || !rejectDialog) return;
            event.preventDefault();
            rejectRow = link.closest("tr");
            fillSummary(rejectDialog, rejectRow);
            document.getElementById("rechazo-id").value = rejectRow.dataset.solicitudId;
            const motivo = document.getElementById("rechazo-motivo");
            motivo.value = "";
            motivo.setCustomValidity("");
            document.getElementById("rechazo-error").textContent = "";
            rejectDialog.showModal();
            motivo.focus();
        });
    });

    const motivo = document.getElementById("rechazo-motivo");
    motivo?.addEventListener("input", () => {
        motivo.setCustomValidity("");
        document.getElementById("rechazo-error").textContent = "";
    });
    document.querySelector(".js-confirmar-rechazo")?.addEventListener("submit", event => {
        if (submitting) { event.preventDefault(); return; }
        motivo.value = motivo.value.trim();
        if (!motivo.value || motivo.value.length > 255) {
            event.preventDefault();
            const mensaje = "Escribe un motivo de entre 1 y 255 caracteres.";
            motivo.setCustomValidity(mensaje);
            document.getElementById("rechazo-error").textContent = mensaje;
            motivo.reportValidity();
            return;
        }
        markBusy(event.currentTarget, rejectRow, "Rechazando…");
    });

    document.querySelectorAll("dialog").forEach(dialog => {
        dialog.querySelectorAll("[data-close-dialog]").forEach(button => {
            button.addEventListener("click", () => { if (!submitting) dialog.close(); });
        });
        dialog.addEventListener("cancel", event => { if (submitting) event.preventDefault(); });
        dialog.addEventListener("close", () => {
            if (dialog.dataset.returnUrl) window.location.assign(dialog.dataset.returnUrl);
            approveForm = null;
            rejectRow = null;
        });
        if (dialog.hasAttribute("data-auto-open") && supportsDialog) dialog.showModal();
    });

    // Restaurar el estado si el navegador recupera la página desde su caché de navegación.
    window.addEventListener("pageshow", event => { if (event.persisted) window.location.reload(); });
})();
