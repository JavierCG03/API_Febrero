// ============================================
// Data/ApplicationDbContext.cs - ACTUALIZADO
// ============================================
using Microsoft.EntityFrameworkCore;
using CarSlineAPI.Models.Entities;

namespace CarSlineAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets existentes
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<TipoServicio> TiposServicio { get; set; }
        public DbSet<ServicioFrecuente> ServiciosFrecuentes { get; set; }
        public DbSet<OrdenGeneral> OrdenesGenerales { get; set; }
        public DbSet<HistorialServicio> HistorialServicios { get; set; }
        public DbSet<Refaccion> Refacciones { get; set; }
        public DbSet<CheckListServicio> CheckListServicios { get; set; }
        public DbSet<TrabajoPorOrden> TrabajosPorOrden { get; set; }
        public DbSet<EstadoTrabajo> EstadosTrabajos { get; set; }
        public DbSet<pausatrabajo> PausasTrabajos { get; set; }
        public DbSet<Evidenciaorden> Evidenciasorden { get; set; }
        public DbSet<Refacciontrabajo> Refaccionestrabajo { get; set; }
        public DbSet<ProximoServicio> ProximosServicios { get; set; }   
        public DbSet<Cita> Citas { get; set; }
        public DbSet<TrabajoPorCita> TrabajosPorCitas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // CONFIGURACIÓN DE TRABAJOS POR ORDEN
            // ============================================
            modelBuilder.Entity<TrabajoPorOrden>(entity =>
            {
                entity.ToTable("trabajopororden");

                entity.HasIndex(e => e.OrdenGeneralId)
                    .HasDatabaseName("IX_Trabajo_OrdenGeneral");

                entity.HasIndex(e => e.TecnicoAsignadoId)
                    .HasDatabaseName("IX_Trabajo_Tecnico");

                entity.HasIndex(e => e.EstadoTrabajo)
                    .HasDatabaseName("IX_Trabajo_Estado");

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("IX_Trabajo_Activo");

                entity.HasIndex(e => e.FechaHoraInicio)
                    .HasDatabaseName("IX_Trabajo_FechaInicio");

                entity.HasIndex(e => e.FechaHoraTermino)
                    .HasDatabaseName("IX_Trabajo_FechaTermino");

                // Relación con OrdenGeneral
                entity.HasOne(e => e.OrdenGeneral)
                    .WithMany(o => o.Trabajos)
                    .HasForeignKey(e => e.OrdenGeneralId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Usuario (Técnico)
                entity.HasOne(e => e.TecnicoAsignado)
                    .WithMany()
                    .HasForeignKey(e => e.TecnicoAsignadoId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Relación con EstadoTrabajo
                entity.HasOne(e => e.EstadoTrabajoNavegacion)
                    .WithMany()
                    .HasForeignKey(e => e.EstadoTrabajo)
                    .OnDelete(DeleteBehavior.Restrict);

                // Valor por defecto para FechaCreacion
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Valor por defecto para EstadoTrabajo
                entity.Property(e => e.EstadoTrabajo)
                    .HasDefaultValue(1);

                // Valor por defecto para Activo
                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);
            });

            // ============================================
            // CONFIGURACIÓN DE ESTADOS DE TRABAJO
            // ============================================
            modelBuilder.Entity<EstadoTrabajo>(entity =>
            {
                entity.ToTable("estadostrabajo");

                // Datos semilla (estados predefinidos)
                entity.HasData(
                    new EstadoTrabajo { Id = 1, NombreEstado = "Pendiente", Descripcion = "Trabajo en espera de asignación", Color = "#FFA500", Orden = 1 },
                    new EstadoTrabajo { Id = 2, NombreEstado = "En Proceso", Descripcion = "Técnico trabajando actualmente", Color = "#2196F3", Orden = 2 },
                    new EstadoTrabajo { Id = 3, NombreEstado = "Completado", Descripcion = "Trabajo terminado exitosamente", Color = "#4CAF50", Orden = 3 },
                    new EstadoTrabajo { Id = 4, NombreEstado = "Pausado", Descripcion = "Trabajo pausado temporalmente", Color = "#FF9800", Orden = 4 },
                    new EstadoTrabajo { Id = 5, NombreEstado = "Cancelado", Descripcion = "Trabajo cancelado", Color = "#F44336", Orden = 5 }
                );
            });

            // ============================================
            // ACTUALIZACIÓN DE ÓRDEN GENERAL
            // ============================================
            modelBuilder.Entity<OrdenGeneral>(entity =>
            {
                // Índices adicionales para los nuevos campos
                entity.HasIndex(e => e.ProgresoGeneral)
                    .HasDatabaseName("IX_OrdenGeneral_Progreso");

                // Valores por defecto
                entity.Property(e => e.TotalTrabajos)
                    .HasDefaultValue(0);

                entity.Property(e => e.TrabajosCompletados)
                    .HasDefaultValue(0);

                entity.Property(e => e.ProgresoGeneral)
                    .HasDefaultValue(0.00m)
                    .HasPrecision(5, 2);

                // Relación con trabajos (ya definida en TrabajoPorOrden)
                entity.HasMany(e => e.Trabajos)
                    .WithOne(t => t.OrdenGeneral)
                    .HasForeignKey(t => t.OrdenGeneralId);
            });

            // ============================================
            // CONFIGURACIONES EXISTENTES
            // ============================================

            // Índices/constraints existentes
            modelBuilder.Entity<Cliente>().HasIndex(c => c.TelefonoMovil).IsUnique(false);
            modelBuilder.Entity<Vehiculo>().HasIndex(v => v.VIN).IsUnique();
            modelBuilder.Entity<Rol>().HasIndex(r => r.NombreRol).IsUnique();

            // Relaciones existentes
            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.NombreUsuario)
                    .IsUnique()
                    .HasDatabaseName("idx_nombre_usuario");

                entity.HasIndex(e => e.RolId)
                    .HasDatabaseName("idx_rol");

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("idx_activo");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(e => e.RolId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreadoPor)
                    .WithMany()
                    .HasForeignKey(e => e.CreadoPorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasIndex(e => e.NombreRol)
                    .IsUnique()
                    .HasDatabaseName("idx_nombre_rol");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // En el método OnModelCreating, después de las configuraciones existentes:

            modelBuilder.Entity<CheckListServicio>(entity =>
            {
                entity.ToTable("checklistservicios");

                entity.HasIndex(e => e.TrabajoId)
                    .HasDatabaseName("IX_CheckList_Trabajo");

                entity.HasIndex(e => e.OrdenGeneralId)
                    .HasDatabaseName("IX_CheckList_OrdenGeneral");

                // Relación con TrabajoPorOrden
                entity.HasOne(e => e.TrabajoPorOrden)
                    .WithMany()
                    .HasForeignKey(e => e.TrabajoId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con OrdenGeneral
                entity.HasOne(e => e.OrdenGeneral)
                    .WithMany()
                    .HasForeignKey(e => e.OrdenGeneralId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}