// Entrega al navegador un archivo que ha preparado el servidor (por ejemplo, la exportación a Excel o a CSV).
// Blazor le pasa el contenido como un flujo; aquí se convierte en un archivo y se pulsa un enlace invisible para
// que el navegador lo descargue con su nombre. Va en un archivo propio porque la política de seguridad (CSP)
// solo permite scripts de la aplicación.
window.viDescargarArchivo = async function (nombre, flujo, tipo) {
    const datos = await flujo.arrayBuffer();
    const archivo = new Blob([datos], { type: tipo });
    const direccion = URL.createObjectURL(archivo);
    const enlace = document.createElement('a');
    enlace.href = direccion;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    // Se libera la memoria un poco después, cuando el navegador ya ha empezado la descarga
    setTimeout(function () { URL.revokeObjectURL(direccion); }, 10000);
};
