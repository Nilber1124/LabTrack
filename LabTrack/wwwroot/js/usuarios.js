(() => {
    "use strict";

    const modal = document.getElementById("modal-usuario");
    const campo = selector => modal.querySelector(selector);

    const abrir = (titulo, datos) => {
        const esNuevo = datos.id === "0";
        campo("#usuario-titulo").textContent = titulo;
        campo("#usuario-id").value = datos.id;
        campo("#Nombre").value = datos.nombre;
        campo("#Codigo").value = datos.codigo;
        campo("#Rol").value = datos.rol;
        campo("#Rol").disabled = !esNuevo;
        campo("#Contrasena").value = "";
        campo("#ConfirmarContrasena").value = "";
        campo("#usuario-ayuda-clave").textContent = esNuevo
            ? "Mínimo 8 caracteres."
            : "Déjala en blanco para conservar la actual.";
        modal.showModal();
    };

    document.querySelector(".js-nuevo-usuario")?.addEventListener("click", evento => {
        evento.preventDefault();
        abrir("Nuevo usuario", { id: "0", nombre: "", codigo: "", rol: "" });
    });

    // Sin JavaScript, estos enlaces abren la página del formulario.
    document.querySelectorAll(".js-editar-usuario").forEach(enlace => {
        enlace.addEventListener("click", evento => {
            evento.preventDefault();
            const { id, nombre, codigo, rol } = enlace.closest("tr").dataset;
            abrir("Editar usuario", { id, nombre, codigo, rol });
        });
    });

    const modalBaja = document.getElementById("modal-baja");
    const confirmarBaja = document.getElementById("confirmar-baja");
    let formularioBaja = null;

    document.querySelectorAll(".js-dar-baja").forEach(formulario => {
        formulario.addEventListener("submit", evento => {
            evento.preventDefault();
            const { nombre, codigo } = formulario.closest("tr").dataset;
            formularioBaja = formulario;
            document.getElementById("baja-titulo").textContent = `¿Dar de baja a ${nombre}?`;
            document.getElementById("baja-detalle").textContent =
                `${codigo} ya no podrá iniciar sesión. Su historial se conserva y podrás reactivarlo cuando quieras.`;
            modalBaja.showModal();
        });
    });

    confirmarBaja.addEventListener("click", () => {
        if (!formularioBaja) {
            return;
        }
        confirmarBaja.disabled = true;
        // submit() no dispara el evento, así que no vuelve a abrir el modal.
        formularioBaja.submit();
    });

    document.querySelectorAll("[data-cerrar]").forEach(boton => {
        boton.addEventListener("click", () => boton.closest("dialog").close());
    });
})();
