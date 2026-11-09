(() => {
    "use strict";

    const inicio = document.getElementById("HoraInicio");
    const fin = document.getElementById("HoraFin");
    const error = document.getElementById("error-horas");
    const boton = document.getElementById("boton-enviar");
    const formulario = document.getElementById("form-reserva");

    // Aviso inmediato; la validación que cuenta es la del servidor.
    const validar = () => {
        const invalido = fin.value <= inicio.value;
        error.hidden = !invalido;
        boton.disabled = invalido;
        fin.setAttribute("aria-invalid", String(invalido));
    };

    inicio.addEventListener("change", validar);
    fin.addEventListener("change", validar);
    validar();

    formulario.addEventListener("submit", () => {
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Comprobando disponibilidad…";
    });
})();
