using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SocxoBlurbCommentGenerator
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        Kernel _Kernel;
        private readonly string _conn;
        private string error { get; set; } = string.Empty;

        public Function1(ILogger<Function1> logger, Kernel kernal)
        {
            _logger = logger;
            _Kernel = kernal;
            _conn = Environment.GetEnvironmentVariable("SqlConnectionString");
        }

        [Function("CommentGenerator")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, CancellationToken cancellationToken)
        {
            // Read and parse the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RequestModel create = JsonConvert.DeserializeObject<RequestModel>(requestBody);
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            req.Headers.TryGetValue("X-API-KEY", out var extractedApiKey);
            create.clientId = extractedApiKey;
            var checkAUthorize = FetchClientId(extractedApiKey);
            if (!checkAUthorize)
            {
                return new OkObjectResult(error);
            }
            else
            {
                var _requestOperations = new RequestOperations(_Kernel);
                var response = await _requestOperations.Handle(create, cancellationToken);
                return new OkObjectResult(response);
            }


        }
        public Task<List<ClientIds>> FetchClientids()
        {
            var dbContext = new AppDbContext(Environment.GetEnvironmentVariable("SqlConnectionString"));
            var result = dbContext.ClientIds.ToListAsync();
            return result;
        }
        private bool FetchClientId(string clientId)
        {
            try
            {
                // Get the dictionary of API keys from configuration
                var apiKeys = FetchClientids().Result;
                if (apiKeys.Where(c => c.clientId.ToString().Equals(clientId)).Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                error = ex.Message;
                // Log exception if needed
                return false; // Return a default value in case of failure
            }
        }
    }
}
