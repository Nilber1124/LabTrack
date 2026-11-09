(() => {
    "use strict";

    const TAMANO_MAXIMO = 2 * 1024 * 1024;
    const TIPOS_VALIDOS = ["image/jpeg", "image/png"];

    const modalEquipo = document.getElementById("modal-equipo");
    const contexto = modalEquipo ?? document;
    const foto = contexto.querySelector("#Foto");
    const vista = contexto.querySelector("#equipo-vista");
    const errorFoto = contexto.querySelector("#equipo-foto-error");

    const pintarVista = url => {
        vista.replaceChildren();
        if (url) {
            const imagen = document.createElement("img");
            imagen.src = url;
            imagen.alt = "";
            vista.append(imagen);
            return;
        }
        const texto = document.createElement("span");
        texto.textContent = "vista previa";
        vista.append(texto);
    };

    // Aviso inmediato; el servidor vuelve a validar el contenido del archivo.
    foto?.addEventListener("change", () => {
        const archivo = foto.files[0];
        const invalido = archivo && (archivo.size > TAMANO_MAXIMO || !TIPOS_VALIDOS.includes(archivo.type));
        errorFoto.hidden = !invalido;
        if (invalido) {
            foto.value = "";
        }
        pintarVista(archivo && !invalido ? URL.createObjectURL(archivo) : vista.dataset.fotoActual);
    });

    const abrirModal = (titulo, datos) => {
        modalEquipo.querySelector("#equipo-titulo").textContent = titulo;
        modalEquipo.querySelector("#equipo-id").value = datos.id;
        modalEquipo.querySelector("#Nombre").value = datos.nombre;
        modalEquipo.querySelector("#CodigoInventario").value = datos.codigo;
        modalEquipo.querySelector("#IdCategoria").value = datos.categoria;
        modalEquipo.querySelector("#Laboratorio").value = datos.laboratorio;
        foto.value = "";
        errorFoto.hidden = true;
        vista.dataset.fotoActual = datos.foto;
        pintarVista(datos.foto);
        modalEquipo.showModal();
    };

    document.querySelectorAll(".js-nuevo-equipo").forEach(enlace => {
        enlace.addEventListener("click", evento => {
            evento.preventDefault();
            abrirModal("Nuevo equipo", { id: "0", nombre: "", codigo: "", categoria: "", laboratorio: "", foto: "" });
        });
    });

    // Sin JavaScript, estos enlaces abren la página del formulario.
    document.querySelectorAll(".js-editar-equipo").forEach(enlace => {
        enlace.addEventListener("click", evento => {
            evento.preventDefault();
            const { id, nombre, codigo, categoria, laboratorio, foto: fotoActual } = enlace.closest("tr").dataset;
            abrirModal("Editar equipo", { id, nombre, codigo, categoria, laboratorio, foto: fotoActual });
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
            document.getElementById("baja-titulo").textContent = `¿Dar de baja ${nombre}?`;
            document.getElementById("baja-detalle").textContent =
                `Inv. ${codigo} dejará de aparecer en el catálogo y no se podrá reservar. El historial se conserva y podrás reactivarlo cuando quieras.`;
            modalBaja.showModal();
        });
    });

    confirmarBaja?.addEventListener("click", () => {
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
