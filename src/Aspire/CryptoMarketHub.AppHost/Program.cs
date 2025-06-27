var builder = DistributedApplication.CreateBuilder(args);

// Add Redis
var redis = builder.AddRedis("redis");

// Add RabbitMQ with management UI enabled
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

// Add Service microservices
var coinsBuilder = builder.AddProject<Projects.SVC_Coins>("svc-coins");

var klineBuilder = builder.AddProject<Projects.SVC_Kline>("svc-kline");

var externalBuilder = builder
    .AddProject<Projects.SVC_External>("svc-external")
    .WithReference(redis)
    .WaitFor(redis);

var bridgeBuilder = builder
    .AddProject<Projects.SVC_Bridge>("svc-bridge")
    .WithReference(coinsBuilder)
    .WithReference(klineBuilder)
    .WithReference(externalBuilder);

builder
    .AddProject<Projects.SVC_Scheduler>("svc-scheduler")
    .WithReference(bridgeBuilder)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

// Add GUI microservice
builder
    .AddProject<Projects.GUI_Crypto>("gui-crypto")
    .WithReference(coinsBuilder)
    .WithReference(klineBuilder)
    .WithReference(externalBuilder)
    .WithReference(bridgeBuilder)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

await builder.Build().RunAsync();
