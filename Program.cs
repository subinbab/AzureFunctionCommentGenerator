using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using OpenAI.RealtimeConversation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//
//#pragma warning disable SKEXP0010.ConfigureFunctionsApplicationInsights();
#pragma warning disable SKEXP0010
builder.Services.AddSingleton<Kernel>(sp =>
{

    if (Environment.GetEnvironmentVariable("env") == "env")
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: "phi3:mini",
                endpoint: new Uri("http://localhost:11434"),
                apiKey: null,
                httpClient: new HttpClient())
            .Build();

    }
    else
    {
        return Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: Environment.GetEnvironmentVariable("DEPLOYMENT_MODEL"),
                endpoint: Environment.GetEnvironmentVariable("AZURE_OPEN_AI_ENDPOINT"),
                apiKey: Environment.GetEnvironmentVariable("AZURE_OPEN_AI_KEY"))
            .Build();

    }
});
builder.Build().Run();
