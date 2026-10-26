// Panel de ayuda flotante: se arrastra desde su cabecera y recuerda dónde se dejó mientras la
// pestaña siga abierta. Va en un archivo propio porque la política de seguridad (CSP) solo
// permite scripts de la aplicación. Escucha en todo el documento para seguir funcionando cuando
// Blazor cambia la página o vuelve a dibujar el panel.
(function () {
    var CLAVE = 'vi-ayuda-posicion';
    var arrastre = null;

    // Lee y guarda la posición en la pestaña; si el navegador no deja, el panel funciona igual
    function leerPosicion() {
        try { return JSON.parse(sessionStorage.getItem(CLAVE) || 'null'); } catch (e) { return null; }
    }
    function guardarPosicion(panel) {
        try { sessionStorage.setItem(CLAVE, JSON.stringify({ x: panel.offsetLeft, y: panel.offsetTop })); } catch (e) { }
    }

    // Mantiene el panel dentro de la ventana, con la cabecera siempre a la vista para poder cogerlo
    function colocar(panel, x, y) {
        var maxX = Math.max(0, window.innerWidth - 120);
        var maxY = Math.max(0, window.innerHeight - 40);
        panel.style.left = Math.min(Math.max(0, x), maxX) + 'px';
        panel.style.top = Math.min(Math.max(0, y), maxY) + 'px';
        panel.style.right = 'auto';
        panel.style.bottom = 'auto';
    }

    // Cuando el panel aparece en la página, se coloca donde se dejó la última vez
    function recolocar() {
        var panel = document.querySelector('.vi-ayuda-flotante:not([data-colocado])');
        if (!panel) return;
        panel.setAttribute('data-colocado', '1');
        var guardada = leerPosicion();
        if (guardada) colocar(panel, guardada.x, guardada.y);
    }
    new MutationObserver(recolocar).observe(document.documentElement, { childList: true, subtree: true });
    recolocar();

    document.addEventListener('pointerdown', function (evento) {
        var cabecera = evento.target.closest('.vi-ayuda-flotante__cabecera');
        // Pulsar un botón o un enlace de la cabecera no inicia el arrastre
        if (!cabecera || evento.target.closest('a, button')) return;
        var panel = cabecera.closest('.vi-ayuda-flotante');
        arrastre = { panel: panel, dx: evento.clientX - panel.offsetLeft, dy: evento.clientY - panel.offsetTop };
        cabecera.setPointerCapture(evento.pointerId);
        evento.preventDefault();
    });

    document.addEventListener('pointermove', function (evento) {
        if (!arrastre) return;
        colocar(arrastre.panel, evento.clientX - arrastre.dx, evento.clientY - arrastre.dy);
    });

    function soltar() {
        if (!arrastre) return;
        guardarPosicion(arrastre.panel);
        arrastre = null;
    }
    document.addEventListener('pointerup', soltar);
    document.addEventListener('pointercancel', soltar);
})();
