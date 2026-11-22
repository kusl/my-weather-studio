using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddDotnetProject("api", "../Weather.Api/Weather.Api.csproj");

builder.AddDotnetProject("web", "../Weather.Web/Weather.Web.csproj")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
