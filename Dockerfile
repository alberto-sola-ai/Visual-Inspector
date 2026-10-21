# Fase 1: compilar y publicar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /src
COPY *.csproj ./
# Primera descarga de paquetes solo con el .csproj: se guarda en caché y acelera las siguientes compilaciones
RUN dotnet restore
COPY . .
# Sin --no-restore: con las pantallas .razor ya copiadas, .NET 10 añade el paquete que trae el
# programa de Blazor (_framework/blazor.web.js). Con --no-restore faltaba y los botones no funcionaban.
RUN dotnet publish -c Release -o /app

# Fase 2: imagen final, solo con lo necesario para ejecutar (imagen estándar Debian:
# incluye los datos de idioma y zona horaria que necesitan los formatos españoles)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilacion /app .
# Carpetas de registros y de claves de sesión, con permiso de escritura para el usuario que ejecuta la aplicación
RUN mkdir -p /app/log /app/claves && chown app:app /app/log /app/claves
# Ámbito Interno WYS: HTTP en el puerto 8080 (el cifrado lo gestiona la red de la empresa)
ENV ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Madrid
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "VisualInspector.dll"]
