
using apiforchat.Models;
using apiforchat.Repositories;
using apiforchat.services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB settings and services
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});


builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var mongoClient = sp.GetRequiredService<IMongoClient>();
    return mongoClient.GetDatabase(settings.DatabaseName);
});


builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddSingleton<IMessageRepository, MessageRepository>();
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseCors("AllowAll");


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API V1");
        c.RoutePrefix = string.Empty;
    });
}


app.UseWebSockets();


app.UseMiddleware<MiddlewareforChat>();


app.UseRouting();
app.MapControllers();

app.Run();
