var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var backend = builder.AddProject<Projects.PopfileNet_Backend>("popfilenet-backend")
    .WithReference(postgres)
    .WithHttpEndpoint(port: 5000)
    .WithEnvironment("DevMode__Enabled", "true");

builder.AddProject<Projects.PopfileNet_Ui>("popfilenet-ui")
    .WithReference(backend)
    .WithHttpEndpoint(port: 5010)
    .WithEnvironment("DevMode__Enabled", "true");

builder.Build().Run();
