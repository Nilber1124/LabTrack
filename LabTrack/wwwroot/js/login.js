(() => {
    "use strict";

    const clave = document.getElementById("Contrasena");
    const mostrar = document.getElementById("mostrar-clave");
    mostrar?.addEventListener("click", () => {
        const oculta = clave.type === "password";
        clave.type = oculta ? "text" : "password";
        mostrar.textContent = oculta ? "Ocultar" : "Mostrar";
        mostrar.setAttribute("aria-pressed", String(oculta));
    });

    const alerta = document.getElementById("alerta-bloqueo");
    const contador = document.getElementById("contador-bloqueo");
    if (alerta && contador) {
        let restantes = Number(alerta.dataset.segundos);
        const pintar = () => {
            const minutos = String(Math.floor(restantes / 60)).padStart(2, "0");
            const segundos = String(restantes % 60).padStart(2, "0");
            contador.textContent = `${minutos}:${segundos}`;
        };
        pintar();
        const intervalo = setInterval(() => {
            restantes -= 1;
            if (restantes <= 0) {
                clearInterval(intervalo);
                window.location.assign(window.location.pathname);
                return;
            }
            pintar();
        }, 1000);
    }

    // Evita envíos dobles; el servidor sigue validando cada intento.
    const formulario = document.getElementById("form-login");
    const boton = document.getElementById("boton-ingresar");
    formulario?.addEventListener("submit", () => {
        if (window.jQuery && !window.jQuery(formulario).valid()) {
            return;
        }
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Validando credenciales…";
    });
})();
