// Botón "ojo" de los campos de contraseña: al pulsarlo la contraseña se ve, y al volver a
// pulsarlo se oculta. Al enviar el formulario se oculta siempre, para que el navegador nunca
// la trate como texto normal.
// Va en un archivo propio porque la política de seguridad (CSP) solo permite scripts de la
// aplicación. Escucha en todo el documento para seguir funcionando cuando Blazor cambia la página.
(function () {
    // Pone el campo visible u oculto y actualiza el botón (icono y texto para lectores de pantalla)
    function cambiar(boton, campo, esVisible) {
        campo.type = esVisible ? 'text' : 'password';
        boton.classList.toggle('vi-ojo--visible', esVisible);
        boton.setAttribute('aria-pressed', esVisible ? 'true' : 'false');
        boton.setAttribute('aria-label', esVisible ? 'Ocultar contraseña' : 'Mostrar contraseña');
        boton.title = esVisible ? 'Ocultar contraseña' : 'Mostrar contraseña';
    }

    // Pulsar el ojo: alterna entre ver y ocultar
    document.addEventListener('click', function (evento) {
        var boton = evento.target.closest('[data-mostrar-password]');
        if (!boton) return;
        var campo = document.getElementById(boton.getAttribute('data-mostrar-password'));
        if (!campo) return;
        cambiar(boton, campo, campo.type === 'password');
        campo.focus();
    });

    // Enviar el formulario: todas sus contraseñas vuelven a ocultarse
    document.addEventListener('submit', function (evento) {
        evento.target.querySelectorAll('[data-mostrar-password]').forEach(function (boton) {
            var campo = document.getElementById(boton.getAttribute('data-mostrar-password'));
            if (campo) cambiar(boton, campo, false);
        });
    }, true);
})();
