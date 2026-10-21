using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Seguridad;

namespace VisualInspector.Datos;

/// <summary>
/// La puerta de entrada a la base de datos MariaDB: aquí se dice qué tablas hay
/// y se apunta solo quién y cuándo crea o cambia cada dato.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> opciones, IUsuarioActual usuarioActual)
    : IdentityDbContext<UsuarioAplicacion, IdentityRole, string>(opciones)
{
    // Juego de caracteres y ordenación en español: guarda bien acentos, eñes y el símbolo €,
    // y ordena la ñ después de la n
    public const string JuegoCaracteres_wys = "utf8mb4";
    public const string Ordenacion_wys = "utf8mb4_uca1400_spanish_ai_ci";

    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<OrdenFabricacion> OrdenesFabricacion => Set<OrdenFabricacion>();
    public DbSet<Autocontrol> Autocontroles => Set<Autocontrol>();

    /// <summary>
    /// Dibuja el plano de las tablas: nombres, longitudes y qué valores no se pueden repetir.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasCharSet(JuegoCaracteres_wys).UseCollation(Ordenacion_wys);

        builder.Entity<UsuarioAplicacion>(usuario =>
        {
            usuario.Property(u => u.NombreCompleto).HasMaxLength(150);
            usuario.Property(u => u.CreadoPor).HasMaxLength(256);
            usuario.Property(u => u.ModificadoPor).HasMaxLength(256);
        });

        builder.Entity<Parametro>(parametro =>
        {
            parametro.ToTable("Parametros");
            parametro.HasIndex(p => p.Clave).IsUnique();
            parametro.Property(p => p.Clave).HasMaxLength(100);
            parametro.Property(p => p.Valor).HasMaxLength(500);
            parametro.Property(p => p.Descripcion).HasMaxLength(500);
            parametro.Property(p => p.MotivoNoEditable).HasMaxLength(250);
            // El tipo se guarda como texto ("SiNo") para que se entienda al mirar la tabla
            parametro.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
            parametro.Property(p => p.CreadoPor).HasMaxLength(256);
            parametro.Property(p => p.ModificadoPor).HasMaxLength(256);
        });

        builder.Entity<OrdenFabricacion>(orden =>
        {
            orden.ToTable("OrdenesFabricacion");
            orden.HasIndex(o => o.Codigo).IsUnique();
            orden.Property(o => o.Codigo).HasMaxLength(10);
            orden.Property(o => o.Descripcion).HasMaxLength(200);
            orden.Property(o => o.CreadoPor).HasMaxLength(256);
            orden.Property(o => o.ModificadoPor).HasMaxLength(256);
        });

        builder.Entity<Autocontrol>(autocontrol =>
        {
            autocontrol.ToTable("Autocontroles");
            // Una orden no puede tener dos controles de la misma hora (dos "Inicial" o dos "+4 h")
            autocontrol.HasIndex(a => new { a.OrdenFabricacionId, a.HorasDesdeInicio }).IsUnique();
            autocontrol.HasIndex(a => a.FechaControl);
            // Restrict: no se puede borrar una orden que tenga autocontroles (son registro de fabricación)
            autocontrol.HasOne(a => a.OrdenFabricacion)
                .WithMany(o => o.Autocontroles)
                .HasForeignKey(a => a.OrdenFabricacionId)
                .OnDelete(DeleteBehavior.Restrict);
            autocontrol.Property(a => a.Estado).HasConversion<string>().HasMaxLength(30);
            autocontrol.Property(a => a.Observaciones).HasMaxLength(500);
            autocontrol.Property(a => a.CreadoPor).HasMaxLength(256);
            autocontrol.Property(a => a.ModificadoPor).HasMaxLength(256);
        });
    }

    /// <summary>
    /// Guarda los cambios, pero antes firma cada dato nuevo o cambiado con el nombre
    /// de quien lo hace y la hora, como quien firma un parte de trabajo.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var nombreUsuario = await usuarioActual.ObtenerNombreAsync();
        // Hora de Madrid: el contenedor y el servidor trabajan con TZ=Europe/Madrid
        var ahora = DateTime.Now;

        foreach (var entrada in ChangeTracker.Entries<ITrazable>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreadoPor = nombreUsuario;
                entrada.Entity.FechaCreacion = ahora;
            }
            else if (entrada.State == EntityState.Modified && !SoloCambiosTecnicos(entrada))
            {
                entrada.Entity.ModificadoPor = nombreUsuario;
                entrada.Entity.FechaModificacion = ahora;
                // Los datos de creación nunca se tocan al modificar
                entrada.Property(e => e.CreadoPor).IsModified = false;
                entrada.Property(e => e.FechaCreacion).IsModified = false;
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Datos que el sistema de usuarios cambia por su cuenta (intentos fallidos, bloqueos, sellos):
    // no son una modificación hecha por una persona, así que no cambian el "Modificado por"
    private static readonly HashSet<string> CamposTecnicos_wys =
    [
        nameof(UsuarioAplicacion.AccessFailedCount), nameof(UsuarioAplicacion.LockoutEnd),
        nameof(UsuarioAplicacion.ConcurrencyStamp), nameof(UsuarioAplicacion.SecurityStamp),
        nameof(UsuarioAplicacion.UltimoAcceso)
    ];

    /// <summary>
    /// Mira si en un dato modificado solo han cambiado campos técnicos. Se compara el valor
    /// original con el actual, porque el sistema de usuarios marca todos los campos como
    /// "modificados" aunque solo cambie uno.
    /// </summary>
    private static bool SoloCambiosTecnicos(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entrada) =>
        entrada.Properties
            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
            .All(p => CamposTecnicos_wys.Contains(p.Metadata.Name));
}
