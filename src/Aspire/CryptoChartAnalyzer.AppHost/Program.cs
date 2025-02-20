var builder = DistributedApplication.CreateBuilder(args);

// Add Redis
var redis = builder.AddRedis("redis");

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

// Add GUI microservice
builder
    .AddProject<Projects.GUI_Crypto>("gui-crypto")
    .WithReference(coinsBuilder)
    .WithReference(klineBuilder)
    .WithReference(externalBuilder)
    .WithReference(bridgeBuilder);

builder.Build().Run();
