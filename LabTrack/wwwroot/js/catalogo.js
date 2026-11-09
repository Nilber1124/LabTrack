(() => {
    "use strict";

    // En móvil, «Cargar más» sustituye al paginador; sin JavaScript sigue funcionando el paginador.
    const boton = document.getElementById("cargar-mas");
    if (!boton) {
        return;
    }

    document.body.classList.add("con-cargar-mas");
    const tarjetas = document.querySelector(".tarjetas");
    const resumen = document.querySelector(".paginacion > span");
    const total = Number(boton.dataset.total);
    const totalPaginas = Number(boton.dataset.totalPaginas);
    let pagina = Number(boton.dataset.pagina);

    const actualizar = () => {
        resumen.textContent = `Mostrando 1–${tarjetas.children.length} de ${total} equipos`;
        boton.hidden = pagina >= totalPaginas;
    };

    boton.addEventListener("click", async () => {
        boton.disabled = true;
        boton.textContent = "Cargando equipos…";

        try {
            const url = new URL(window.location.href);
            url.searchParams.set("pagina", String(pagina + 1));
            const respuesta = await fetch(url);
            if (!respuesta.ok) {
                throw new Error(String(respuesta.status));
            }

            const documento = new DOMParser().parseFromString(await respuesta.text(), "text/html");
            tarjetas.append(...documento.querySelectorAll(".tarjetas > .tarjeta"));
            pagina += 1;
            boton.textContent = "Cargar más";
            actualizar();
        } catch {
            boton.textContent = "Reintentar";
        } finally {
            boton.disabled = false;
        }
    });

    actualizar();
})();
