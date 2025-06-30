using Serilog;

using Networking.Common;

var builder = WebApplication.CreateBuilder(args);

File.Delete("Logs/webapi-log.txt");
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
       path: "Logs/webapi-log.txt", 
       rollingInterval: RollingInterval.Infinite,
       outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
       )
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

try {
    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient();
    builder.Services.AddTransient<WebApiClientService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection();

    // app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception ex) {
    Log.Fatal(ex, "Application start-up failed");
}
finally {
    Log.CloseAndFlush();
}
