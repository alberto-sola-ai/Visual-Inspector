// Modo diurno / nocturno de la aplicación. Como el interruptor de la luz: cada persona elige el
// suyo y el navegador se acuerda la próxima vez. Solo se guarda la palabra "noche" o "dia" en el
// propio navegador (localStorage); no se envía nada al servidor.
// Va en un archivo propio porque la política de seguridad (CSP) solo permite scripts de la
// aplicación, y se carga en la cabecera de la página para que no haya un parpadeo en blanco antes
// de oscurecerse.
(function () {
    var CLAVE = 'vi-modo-visual';

    // Lee lo que eligió la persona; si el navegador no deja guardar datos, siempre modo diurno
    function leerModo() {
        try {
            return localStorage.getItem(CLAVE) === 'noche' ? 'noche' : 'dia';
        } catch (error) {
            return 'dia';
        }
    }

    // Pone el modo en la página: Bootstrap oscurece todo cuando ve data-bs-theme="dark"
    function aplicarModo(modo) {
        document.documentElement.setAttribute('data-bs-theme', modo === 'noche' ? 'dark' : 'light');
    }

    // Se aplica nada más cargar este archivo, antes de pintar la pantalla
    aplicarModo(leerModo());

    // Al pulsar la opción del menú se cambia de modo y se recuerda la elección.
    // Escucha en todo el documento para seguir funcionando cuando Blazor cambia la página
    document.addEventListener('click', function (evento) {
        if (!evento.target.closest('[data-modo-visual]')) return;

        var nuevo = leerModo() === 'noche' ? 'dia' : 'noche';
        try {
            localStorage.setItem(CLAVE, nuevo);
        } catch (error) {
            // Sin almacenamiento el cambio vale solo para esta pantalla
        }
        aplicarModo(nuevo);
    });
})();
