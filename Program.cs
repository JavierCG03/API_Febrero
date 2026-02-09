using CarSlineAPI.Data;
using CarSlineAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURACIÓN PARA RED LOCAL
// ============================================
// Configurar para escuchar en todas las interfaces de red (0.0.0.0)
// Esto permite que dispositivos en la red local se conecten
builder.WebHost.UseUrls("http://0.0.0.0:5293");

// ============================================
// CONFIGURACIÓN DE SERVICIOS
// ============================================

// Agregar controladores
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CarSline API",
        Version = "v1",
        Description = "API para el Sistema de Gestión de Taller Automotriz"
    });
});

// Configurar la cadena de conexión a MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
}

// Configurar DbContext con MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPdfService, PdfService>();  

// Configurar CORS para permitir cualquier origen (SOLO PARA DESARROLLO)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ============================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// ============================================

// Usar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CarSline API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

// Usar CORS
app.UseCors("AllowAll");

// NO usar HTTPS redirect para desarrollo local
// app.UseHttpsRedirection();

// Usar autorización (aunque no tenemos autenticación JWT todavía)
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// ============================================
// VERIFICAR Y CREAR BASE DE DATOS
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Verificar conexión a la base de datos
        if (await context.Database.CanConnectAsync())
        {
            Console.WriteLine("✅ Conexión exitosa a la base de datos");

            // Aplicar migraciones pendientes (si las hay)
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Console.WriteLine("⏳ Aplicando migraciones pendientes...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Migraciones aplicadas correctamente");
            }
        }
        else
        {
            Console.WriteLine("❌ No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// ============================================
// OBTENER IP LOCAL DE LA LAPTOP
// ============================================
string GetLocalIPAddress()
{
    try
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "No se pudo obtener la IP";
    }
    catch
    {
        return "Error al obtener la IP";
    }
}

var localIP = GetLocalIPAddress();

// ============================================
// INICIAR LA APLICACIÓN
// ============================================
Console.WriteLine($"Entorno: {app.Environment.EnvironmentName}");
Console.WriteLine($"Escuchando en: http://0.0.0.0:5293");
Console.WriteLine();
Console.WriteLine("        📱 Coneccion Movil:");
Console.WriteLine($"IP de este ordenador : {localIP}");
Console.WriteLine($"URL para la app: http://{localIP}:5293/api");
Console.WriteLine();
Console.WriteLine("    🌐 Verificacion en navegador:");
Console.WriteLine($" Desde tu teléfono: http://{localIP}:5293");
Console.WriteLine("===========================================");

app.Run();