// Reloj de la barra superior: fecha, hora (hora peninsular, Europe/Madrid) y turno de trabajo,
// actualizados cada segundo. Por ejemplo: "jueves 23/10/2026 · 14:32:05 · Turno de tarde".
// Va en un archivo propio porque la política de seguridad (CSP) solo permite scripts de la
// aplicación. Busca el reloj en cada vuelta para seguir funcionando cuando Blazor cambia la página.
(function () {
    // Formato español con la hora de Madrid, aunque el equipo tenga otra zona horaria
    var formato = new Intl.DateTimeFormat('es-ES', {
        timeZone: 'Europe/Madrid', weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23'
    });

    // Turnos de la fábrica: mañana de 6 a 14 h, tarde de 14 a 22 h y noche de 22 a 6 h
    function turno(hora) {
        if (hora >= 6 && hora < 14) return 'Turno de mañana';
        if (hora >= 14 && hora < 22) return 'Turno de tarde';
        return 'Turno de noche';
    }

    // Escribe la fecha, la hora y el turno en todos los relojes de la página
    function actualizar() {
        var partes = {};
        formato.formatToParts(new Date()).forEach(function (p) { partes[p.type] = p.value; });
        var fecha = partes.weekday + ' ' + partes.day + '/' + partes.month + '/' + partes.year;
        var hora = partes.hour + ':' + partes.minute + ':' + partes.second;
        var textoTurno = turno(parseInt(partes.hour, 10));

        document.querySelectorAll('.vi-reloj').forEach(function (reloj) {
            reloj.querySelector('.vi-reloj__fecha').textContent = fecha;
            reloj.querySelector('.vi-reloj__hora').textContent = hora;
            reloj.querySelector('.vi-reloj__turno').textContent = textoTurno;
            reloj.hidden = false;
        });
    }

    actualizar();
    setInterval(actualizar, 1000);
})();
