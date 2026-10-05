using MinecraftServerManager.Hubs;
using MinecraftServerManager.Services;

var builder = WebApplication.CreateBuilder(args);

// container services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ServerRegistry>();
builder.Services.AddSingleton<ServerProcessService>();
builder.Services.AddTransient<MinecraftQueryService>();
builder.Services.AddHostedService<LogWatcherService>();

// register signalR
builder.Services.AddSignalR();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

// end points
app.MapControllers();
app.MapHub<ConsoleHub>("/hubs/ConsoleHub");

app.Run();
