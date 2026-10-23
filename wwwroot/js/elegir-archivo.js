// «Elegir archivo» del autocontrol con carpeta recordada: el botón del patrón abre el explorador
// de Windows en la última carpeta de patrones usada, y los de las muestras en la última carpeta
// de muestras (cada uno la suya). La primera vez se abre en «Mis documentos».
// Usa el selector de archivos moderno de Edge y Chrome, que solo existe con https o en el propio
// equipo (localhost). Si no existe, no se hace nada y sale el explorador de siempre.
// Va en un archivo propio porque la política de seguridad (CSP) solo permite scripts de la
// aplicación. Escucha en todo el documento para seguir funcionando cuando Blazor cambia la página.
(function () {
    // Qué archivos admite cada tipo de hueco
    var tipos = {
        patrones: [{ description: 'Patrón en PDF', accept: { 'application/pdf': ['.pdf'] } }],
        muestras: [{ description: 'Muestra escaneada', accept: { 'image/jpeg': ['.jpg', '.jpeg'], 'image/png': ['.png'], 'image/tiff': ['.tif', '.tiff'] } }]
    };

    document.addEventListener('click', function (evento) {
        var campo = evento.target.closest('input[type=file][data-carpeta-recordada]');
        if (!campo || campo.disabled || typeof window.showOpenFilePicker !== 'function') return;

        var carpeta = campo.getAttribute('data-carpeta-recordada');
        if (!tipos[carpeta]) return;

        // En lugar del explorador de siempre, se abre el que recuerda la carpeta de este tipo
        evento.preventDefault();
        window.showOpenFilePicker({
            id: 'vi-' + carpeta,        // el navegador recuerda una carpeta distinta para cada id
            startIn: 'documents',       // la primera vez: «Mis documentos»
            types: tipos[carpeta],
            multiple: false
        }).then(function (elegidos) {
            return elegidos[0].getFile();
        }).then(function (archivo) {
            // Se entrega el archivo al campo como si se hubiera elegido con el explorador de siempre,
            // para que la aplicación lo reciba y lo revise igual
            var transferencia = new DataTransfer();
            transferencia.items.add(archivo);
            campo.files = transferencia.files;
            campo.dispatchEvent(new Event('change', { bubbles: true }));
        }).catch(function (error) {
            // Cancelar el explorador no es un error. Con cualquier otro fallo, ese botón vuelve a
            // usar el explorador de siempre la próxima vez que se pulse
            if (error && error.name !== 'AbortError') campo.removeAttribute('data-carpeta-recordada');
        });
    }, true);
})();
