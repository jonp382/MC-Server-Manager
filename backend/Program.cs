using MinecraftServerManager.Hubs;
using MinecraftServerManager.Services;

var builder = WebApplication.CreateBuilder(args);

// container services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ServerRegistry>();
builder.Services.AddSingleton<PathTranslator>();
builder.Services.AddSingleton<ServerProcessService>();
builder.Services.AddTransient<MinecraftQueryService>();
builder.Services.AddHostedService<LogWatcherService>();

// in a container the registry writes servers.json to a mounted volume;
// outside, it stays at the content root as before
var registryPath = Environment.GetEnvironmentVariable("SERVER_REGISTRY_PATH");
if (!string.IsNullOrEmpty(registryPath))
{
    builder.Configuration["RegistryPath"] = registryPath;
}

// register signalR
builder.Services.AddSignalR();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://0.0.0.0:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// seed the registry so the log watcher has instances on first run
app.Services.GetRequiredService<ServerRegistry>();

// http requests and swagger for dev testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// https is only wired up for local dev; in a container nginx terminates
// plain http, so redirecting would bounce requests to a dead https port
if (!string.Equals(app.Environment.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

// end points
app.MapControllers();
app.MapHub<ConsoleHub>("/hubs/ConsoleHub");

app.Run();
