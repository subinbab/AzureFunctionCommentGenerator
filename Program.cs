using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

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

    if (false)
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
                deploymentName: "gpt-35-turbo-16k",
                endpoint: "https://chataisolution.openai.azure.com/",
                apiKey: "3d6e995cb1824cd6ba1019584bb851b6")
            .Build();

    }
});
builder.Build().Run();
