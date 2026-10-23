// Menús desplegables de la cabecera (Administración y el de la persona conectada). El navegador
// ya los abre y cierra al pulsar su botón; este script además los cierra al pulsar fuera, al
// elegir una opción, al abrir otro menú y con la tecla Escape, como cualquier menú desplegable.
// Va en un archivo propio porque la política de seguridad (CSP) solo permite scripts de la
// aplicación. Escucha en todo el documento para seguir funcionando cuando Blazor cambia la página.
(function () {
    // Cierra todos los menús abiertos menos el indicado
    function cerrar(salvo) {
        document.querySelectorAll('details.vi-desplegable[open]').forEach(function (menu) {
            if (menu !== salvo) menu.removeAttribute('open');
        });
    }

    document.addEventListener('click', function (evento) {
        var menu = evento.target.closest('details.vi-desplegable');
        // Pulsar fuera de los menús los cierra; abrir uno cierra los demás
        cerrar(menu);
        // Elegir una opción (enlace o botón) cierra su menú
        if (menu && evento.target.closest('.vi-desplegable__opciones a, .vi-desplegable__opciones button')) {
            menu.removeAttribute('open');
        }
    });

    // Escape cierra el menú y deja el foco en su botón, para seguir con el teclado
    document.addEventListener('keydown', function (evento) {
        if (evento.key !== 'Escape') return;
        var abierto = document.querySelector('details.vi-desplegable[open]');
        if (abierto) {
            abierto.removeAttribute('open');
            abierto.querySelector('summary').focus();
        }
    });
})();
