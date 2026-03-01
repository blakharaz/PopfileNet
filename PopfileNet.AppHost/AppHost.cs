var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

builder.AddProject<Projects.PopfileNet_Backend>("popfilenet-backend")
    .WithReference(postgres);

builder.AddProject<Projects.PopfileNet_Ui>("popfilenet-ui");

builder.Build().Run();
