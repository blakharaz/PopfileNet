using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

builder.AddDockerComposeEnvironment("compose")
    .ConfigureComposeFile(file => file.AddVolume(new Volume { Name = "classifier-models" }));

var backend = builder.AddProject<Projects.PopfileNet_Backend>("popfilenet-backend")
    .WithReference(postgres)
    .WithEnvironment("DevMode", "true")
    .WithEnvironment("Classifier__ModelsRoot", "/data/classifier-models")
    .PublishAsDockerComposeService((_, service) =>
        service.AddVolume(new Volume
        {
            Name = "classifier-models",
            Type = "volume",
            Source = "classifier-models",
            Target = "/data/classifier-models"
        }));

builder.AddProject<Projects.PopfileNet_Ui>("popfilenet-ui")
    .WithReference(backend);

builder.Build().Run();
