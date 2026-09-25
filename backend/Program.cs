using MinecraftServerManager.Hubs;
using MinecraftServerManager.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// container services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<LogWatcherService>();
builder.Services.AddSingleton<MinecraftProcessService>();

// register signalR 
builder.Services.AddSignalR();
builder.Services.AddSingleton<IMinecraftProcessService, MinecraftProcessService>();

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

// signalR
var processService = app.Services.GetRequiredService<IMinecraftProcessService>();
var hubContext = app.Services.GetRequiredService<IHubContext<ConsoleHub>>();

processService.OutputReceived += async (line) =>
{
    await hubContext.Clients.All.SendAsync("ReceiveLogLine", line);
};

app.Run();