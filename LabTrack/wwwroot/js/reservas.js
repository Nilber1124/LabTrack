(() => {
    "use strict";

    const inicio = document.getElementById("HoraInicio");
    const fin = document.getElementById("HoraFin");
    const error = document.getElementById("error-horas");
    const boton = document.getElementById("boton-enviar");
    const formulario = document.getElementById("form-reserva");
    const duracion = document.getElementById("duracion-reserva");

    // Aviso inmediato; la validación que cuenta es la del servidor.
    const validar = () => {
        const invalido = fin.value <= inicio.value;
        error.hidden = !invalido;
        boton.disabled = invalido;
        fin.setAttribute("aria-invalid", String(invalido));
        const horas = parseInt(fin.value, 10) - parseInt(inicio.value, 10);
        duracion.textContent = invalido ? "" : `Duración: ${horas} ${horas === 1 ? "hora" : "horas"}`;
    };

    const seleccion = document.getElementById("ocupacion-seleccion");
    const pintarSeleccion = () => {
        const desde = parseInt(inicio.value, 10);
        const hasta = parseInt(fin.value, 10);
        seleccion.hidden = hasta <= desde;
        seleccion.style.left = `${(desde - 7) / 14 * 100}%`;
        seleccion.style.width = `${(hasta - desde) / 14 * 100}%`;
    };
    inicio.addEventListener("change", pintarSeleccion);
    fin.addEventListener("change", pintarSeleccion);
    pintarSeleccion();

    // La ocupación depende del día: se vuelve a pedir al cambiar la fecha.
    document.getElementById("Fecha").addEventListener("change", evento => {
        if (!evento.target.value) {
            return;
        }
        const url = new URL(window.location.origin + window.location.pathname);
        url.searchParams.set("idEquipo", document.getElementById("IdEquipo").value);
        url.searchParams.set("fecha", evento.target.value);
        window.location.assign(url);
    });

    document.getElementById("usar-sugerencia")?.addEventListener("click", evento => {
        inicio.value = evento.currentTarget.dataset.inicio;
        fin.value = evento.currentTarget.dataset.fin;
        validar();
        pintarSeleccion();
    });

    inicio.addEventListener("change", validar);
    fin.addEventListener("change", validar);
    validar();

    formulario.addEventListener("submit", () => {
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Comprobando disponibilidad…";
    });
})();
