using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Endpoints;
using RhinoImageStudio.Backend.Infrastructure;
using RhinoImageStudio.Backend.Options;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);

var port = Defaults.DefaultPort;
var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg.Split('=')[1], out var parsedPort))
    port = parsedPort;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, port);
});

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RhinoImageStudio",
    Defaults.DatabaseName);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddRhinoImageStudioServices(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins($"http://localhost:{port}", $"http://127.0.0.1:{port}")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Rhino Image Studio API", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

var api = app.MapGroup("/api");
api.MapProjectEndpoints();
api.MapCaptureEndpoints();
api.MapGenerationEndpoints();
api.MapJobEndpoints();
api.MapConfigEndpoints(port);
api.MapEventEndpoints();
api.MapRhinoBridgeEndpoints();
app.MapImageEndpoints();

app.MapFallbackToFile("index.html");

Console.WriteLine($"Rhino Image Studio Backend starting on http://localhost:{port}");
app.Run();
