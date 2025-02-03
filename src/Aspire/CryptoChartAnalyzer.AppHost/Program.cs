var builder = DistributedApplication.CreateBuilder(args);

// Add service defaults
var defaultsBuilder = builder.AddProject<Projects.CryptoChartAnalyzer_ServiceDefaults>(
    "servicedefaults"
);

// Add GUI microservice
var guiBuilder = builder
    .AddProject<Projects.GUI_Crypto>("gui-crypto")
    .WithReference(defaultsBuilder);

// Add Service microservices
var bridgeBuilder = builder
    .AddProject<Projects.SVC_Bridge>("svc-bridge")
    .WithReference(defaultsBuilder);

var klineBuilder = builder
    .AddProject<Projects.SVC_Kline>("svc-kline")
    .WithReference(defaultsBuilder);

var externalBuilder = builder
    .AddProject<Projects.SVC_External>("svc-external")
    .WithReference(defaultsBuilder);

var coinsBuilder = builder
    .AddProject<Projects.SVC_Coins>("svc-coins")
    .WithReference(defaultsBuilder);

// Configure dependencies between services
bridgeBuilder
    .WithReference(klineBuilder)
    .WithReference(externalBuilder)
    .WithReference(coinsBuilder);

guiBuilder.WithReference(bridgeBuilder);

builder.Build().Run();
