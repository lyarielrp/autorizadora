using autorizadora_producer.services;
using DotNetEnv;
using autorizadora_producer.services.interfaces;
using Microsoft.Extensions.Options;

using autorizadora_producer.Entity;

Env.Load();
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SocketServerConfig>(config => {
    config.Ip = Env.GetString("socket_authorize_ip");
    config.Port = Env.GetInt("socket_authorize_port");
});
//builder.Services.AddSingleton<ISocketServer>(new SocketServer());
//builder.Services.AddSingleton<ISocketServer,SocketServer>();

builder.Services.AddSingleton<IRabbitMQ_Producer, RabbitMQ_Producer>();
// builder.Services.AddSingleton(new RabbitMQ(Env.GetString("RABBITMQ_HOST"), Env.GetInt("RABBITMQ_PORT"), Env.GetString("RABBITMQ_VIRTUALHOST"), Env.GetString("RABBITMQ_USERNAME"), Env.GetString("RABBITMQ_PASSWORD")));
builder.Services.AddHostedService<SocketBackgroundService>( );
builder.Services.AddSingleton(provider =>
{
    var rabbitmq = provider.GetRequiredService<IRabbitMQ_Producer>();
    var config = provider.GetRequiredService<IOptions<SocketServerConfig>>().Value;
    Console.WriteLine(config.Ip);
    var logger = provider.GetRequiredService<ILogger<SocketServer>>();
    
    return new SocketServer(config.Port, config.Ip,rabbitmq, logger);
});


builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
});

var app = builder.Build();

await app.RunAsync();