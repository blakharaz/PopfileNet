var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var backend = builder.AddProject<Projects.PopfileNet_Backend>("popfilenet-backend")
    .WithReference(postgres);

builder.AddProject<Projects.PopfileNet_Ui>("popfilenet-ui")
    .WithReference(backend);

builder.Build().Run();
