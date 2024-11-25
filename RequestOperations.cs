using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Azure.Core;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;

namespace SocxoBlurbCommentGenerator
{
    public class RequestOperations
    {
        private readonly string _connectionString;
        int limit;
        int domainRequestLimit;
        int validationTimeForClient;
        int validationTimeForBlurb;
        int NoOfCommentsGenerated;
        Kernel _kernal { get; set; }
        public RequestOperations(Kernel kernal)
        {
            _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            limit = Convert.ToInt32(Environment.GetEnvironmentVariable("BlurbCommentLimit"));
            domainRequestLimit = Convert.ToInt32(Environment.GetEnvironmentVariable("Domianlimit"));
            validationTimeForClient = Convert.ToInt32(Environment.GetEnvironmentVariable("LimitValidationTimeForClient"));
            validationTimeForBlurb = Convert.ToInt32(Environment.GetEnvironmentVariable("LimitValidationTimeForBlurb"));
            NoOfCommentsGenerated = Convert.ToInt32(Environment.GetEnvironmentVariable("NoOfCommentsGenerated"));
            _kernal = kernal;
        }
        public async Task<Response> Handle(RequestModel request, CancellationToken cancellationToken)
        {
            try
            {
                int remainingClientRequest = 0;
                int remainingBlurbRequest = 0;
                var client = FetchClientId(request.clientId);
                var existRequest = GetByClientId(request.clientId, cancellationToken);
                var existBlurb = GetByLink(request.link, cancellationToken);
                string BlurbLimitResetInTime = "";
                string ClientLimitResetInTime = "";
                if (existRequest != null && existBlurb != null)
                {

                    var checkRequestTimeExceeds = CheckIfRequestTimeExceeds(existRequest);
                    var checkBlurbTimeExceeds = CheckIfBlurbTimeExceeds(existBlurb);
                    if (checkRequestTimeExceeds && checkBlurbTimeExceeds)
                    {
                        ResetBlurbLimit(existBlurb, cancellationToken);
                        ResetRequestLimit(existRequest, cancellationToken);
                        existBlurb.DateCreated = DateTime.Now;
                        existRequest.DateCreated = DateTime.Now;
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                    }
                    if (checkRequestTimeExceeds)
                    {
                        ResetRequestLimit(existRequest, cancellationToken);
                        existRequest.DateCreated = DateTime.Now;
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                    }
                    if (checkBlurbTimeExceeds)
                    {
                        ResetBlurbLimit(existBlurb, cancellationToken);
                        existBlurb.DateCreated = DateTime.Now;
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                    }
                    if (!checkBlurbTimeExceeds)
                    {
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                        if (existBlurb.RemainingLimit <= 0)
                        {
                            remainingBlurbRequest = existBlurb.RemainingLimit;
                            remainingClientRequest = existRequest.RemainingLimit;
                            return new Response(null, request.link, false, "error occured exceeds the time limit", remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                        }
                        else
                        {
                            existBlurb.RemainingLimit = existBlurb.RemainingLimit - 1;
                            remainingBlurbRequest = existBlurb.RemainingLimit;
                            existRequest.Blurb.Where(blurb => blurb.Link.Equals(existBlurb.Link)).FirstOrDefault().RemainingLimit = remainingBlurbRequest;
                            BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                            ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                            Update(existRequest);
                        }
                    }
                    if (!checkRequestTimeExceeds)
                    {
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);

                        if (existRequest.RemainingLimit <= 0)
                        {

                            remainingClientRequest = existRequest.RemainingLimit;
                            return new Response(null, request.link, false, "error occured exceeds the time limit", remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                        }
                        else
                        {
                            existRequest.RemainingLimit = existRequest.RemainingLimit - 1;
                            remainingClientRequest = existRequest.RemainingLimit;
                            BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                            ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                            Update(existRequest);
                        }

                    }
                    List<GeneratedComments> comments;
                    string error;
                    try
                    {
                        //remainingBlurbRequest = existBlurb.RemainingLimit;
                        //remainingClientRequest = existRequest.RemainingLimit;
                        comments = await GenerateComment(request.description, request.title,request.wordLimit);
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                        error = "No errors";
                    }
                    catch (Exception ex)
                    {
                        comments = null;
                        error = ex.Message;
                    }

                    return new Response(comments, request.link, true, error, remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                }
                else if (existRequest != null)
                {
                    var checkRequestTimeExceeds = CheckIfRequestTimeExceeds(existRequest);
                    if (checkRequestTimeExceeds)
                    {
                        remainingClientRequest = limit;
                        remainingBlurbRequest = domainRequestLimit;
                        ResetRequestLimit(existRequest, cancellationToken);
                        existRequest.DateCreated = DateTime.Now;
                        existBlurb = new Blurbs();
                        existBlurb.DateCreated = DateTime.Now;
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                        //_blurbRepository.Create(new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now });
                        Add((new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now }));
                        Update(new Requests { RequestedTime = DateTime.Now, Blurb = existRequest.Blurb, Domian = "Socxo", Limit = domainRequestLimit, RemainingLimit = domainRequestLimit, ClientId = request.clientId, DateCreated = DateTime.Now });
                        List<GeneratedComments> comments;
                        string error;
                        try
                        {
                            comments = await GenerateComment(request.description, request.title, request.wordLimit);
                            error = "No errors";
                        }
                        catch (Exception ex)
                        {
                            comments = null;
                            error = ex.Message;
                        }
                        return new Response(comments, request.link, true, error, remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                    }
                    else
                    {
                        existRequest.RemainingLimit = existRequest.RemainingLimit - 1;
                        remainingClientRequest = existRequest.RemainingLimit;
                        remainingBlurbRequest = limit;
                        existBlurb = new Blurbs();
                        existBlurb.DateCreated = DateTime.Now;
                        BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                        ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                        if (existRequest.RemainingLimit <= 0)
                        {
                            return new Response(null, request.link, false, "error occured exceeds the time limit", remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                        }
                        else
                        {
                            existRequest.RemainingLimit = existRequest.RemainingLimit - 1;
                            //_blurbRepository.Create(new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now });
                            //await _unitOfWork.Save(cancellationToken);
                            existRequest.Blurb.Add((new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now }));
                            Update(new Requests { RequestedTime = DateTime.Now, Blurb = existRequest.Blurb, Domian = "Socxo", Limit = domainRequestLimit, RemainingLimit = domainRequestLimit, ClientId = request.clientId, DateCreated = DateTime.Now });
                            existBlurb.DateCreated = DateTime.Now;
                            BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                            List<GeneratedComments> comments;
                            string error;
                            try
                            {
                                comments = await GenerateComment(request.description, request.title, request.wordLimit);
                                error = "No errors";
                            }
                            catch (Exception ex)
                            {
                                comments = null;
                                error = ex.Message;
                            }
                            return new Response(comments, request.link, true, error, remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                        }
                    }

                }
                else
                {
                    remainingClientRequest = limit;
                    remainingBlurbRequest = domainRequestLimit;
                    //_blurbRepository.Create(new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now });
                    //await _unitOfWork.Save(cancellationToken);
                    //var blurbByLink = await _blurbRepository.GetByLink(request.link, cancellationToken);
                    Add(new Requests { RequestedTime = DateTime.Now, Blurb = new List<Blurbs>() { (new Blurbs { Title = request.title, Link = request.link, Description = request.description, Limit = limit, RemainingLimit = limit, DateCreated = DateTime.Now }) }, Domian = "Socxo", Limit = domainRequestLimit, RemainingLimit = domainRequestLimit, ClientId = request.clientId, DateCreated = DateTime.Now });
                    existBlurb = new Blurbs();
                    existRequest = new Requests();
                    existBlurb.DateCreated = DateTime.Now;
                    existRequest.DateCreated = DateTime.Now;
                    BlurbLimitResetInTime = BlurbLimitResetIn(existBlurb);
                    ClientLimitResetInTime = ClientLimitResetIn(existRequest);
                    List<GeneratedComments> comments;
                    string error;
                    try
                    {
                        comments = await GenerateComment(request.description, request.title,request.wordLimit);
                        error = "No errors";
                    }
                    catch (Exception ex)
                    {
                        comments = null;
                        error = ex.Message;
                    }
                    return new Response(comments, request.link, true, error, remainingBlurbRequest, remainingClientRequest, ResourceDetails.name, client.ToString(), BlurbLimitResetInTime, ClientLimitResetInTime);
                }
            }
            catch (Exception ex)
            {
                return new Response(null, request.link, false, ex.StackTrace.ToString(), 0, 0, ResourceDetails.name, "", "", "");

            }
        }

        private void Add(Blurbs blurbs)
        {
            var dbContext = new AppDbContext(_connectionString);
            dbContext.Blurbs.Add(blurbs);
            dbContext.SaveChanges();
        }
        private void Add(Requests request)
        {
            var dbContext = new AppDbContext(_connectionString);
            dbContext.Requests.Add(request);
            dbContext.SaveChanges();
        }

        private void Update(Requests existRequest)
        {
            var dbContext = new AppDbContext(_connectionString);
            dbContext.Requests.Update(existRequest);
            dbContext.SaveChanges();
        }
        private void Update(Blurbs existRequest)
        {
            var dbContext = new AppDbContext(_connectionString);
            dbContext.Blurbs.Update(existRequest);
            dbContext.SaveChanges();
        }

        private Blurbs GetByLink(string link, CancellationToken cancellationToken)
        {
            var dbContext = new AppDbContext(_connectionString);
            var result = dbContext.Blurbs.ToList().Where(c => c.Link.Equals(link)).FirstOrDefault();
            return result;
        }

        private Requests GetByClientId(string clientId, CancellationToken cancellationToken)
        {
            var dbContext = new AppDbContext(_connectionString);
            return dbContext.Requests.Include(c => c.Blurb).ToList().Where(c => c.ClientId.Equals(clientId)).FirstOrDefault();
        }


            public Task<List<ClientIds>> FetchClientids()
        {
            var dbContext = new AppDbContext(_connectionString);
            var result = dbContext.ClientIds.ToListAsync();
            return result;
        }
        private Clients FetchClientId(string clientId)
        {
            try
            {
                // Get the dictionary of API keys from configuration
                var apiKeys = FetchClientids().Result;
                var client = apiKeys.Where(c => c.clientId.ToString().Equals(clientId)).FirstOrDefault();

                // Check if the client key exists, and try to parse it into the Clients enum
                if (!string.IsNullOrEmpty(client.clientName) && Enum.TryParse(client.clientName, out Clients clientEnum))
                {
                    return clientEnum;
                }
                return Clients.Socxo;
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return Clients.Socxo; // Return a default value in case of failure
            }
        }

        private bool ResetRequestLimit(Requests requests, CancellationToken cancellationToken)
        {
            try
            {
                requests.RemainingLimit = domainRequestLimit;
                requests.DateCreated = DateTime.Now;
                Update(requests);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private bool ResetBlurbLimit(Blurbs requests, CancellationToken cancellationToken)
        {
            try
            {
                requests.RemainingLimit = limit;
                requests.DateCreated = DateTime.Now;
                Update(requests);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private bool CheckIfRequestTimeExceeds(Requests request)
        {
            try
            {
                if (request != null)
                {
                    // Calculate the time difference between DateCreated and now
                    TimeSpan timeDifference = DateTime.Now - request.DateCreated;
                    if (timeDifference.TotalHours > validationTimeForClient)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception();
            }
        }
        private string BlurbLimitResetIn(Blurbs request)
        {
            try
            {
                if (request != null)
                {
                    // Calculate the time difference between DateCreated and now
                    TimeSpan timeDifference = DateTime.Now - request.DateCreated;
                    return request.DateCreated.AddHours(validationTimeForBlurb).ToString();
                }
                throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception();
            }
        }
        private string ClientLimitResetIn(Requests request)
        {
            try
            {
                if (request != null)
                {
                    // Calculate the time difference between DateCreated and now
                    TimeSpan timeDifference = DateTime.Now - request.DateCreated;
                    return request.DateCreated.AddHours(validationTimeForClient).ToString();
                }
                throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception();
            }
        }
        private bool CheckIfBlurbTimeExceeds(Blurbs request)
        {
            try
            {
                if (request != null)
                {
                    // Calculate the time difference between DateCreated and now
                    TimeSpan timeDifference = DateTime.Now - request.DateCreated;
                    if (timeDifference.TotalHours > validationTimeForBlurb)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception();
            }
        }
        private async Task<List<GeneratedComments>> GenerateComment(string description, string title , int wordLimit)
        {

            try
            {
                var listComments = new List<GeneratedComments>();

                // Ensure the chat completion service is correctly set up
                var chatCompletionService = _kernal.GetRequiredService<IChatCompletionService>();
                if (chatCompletionService == null)
                {
                    throw new InvalidOperationException("Chat completion service not found.");
                }

                // Create a new chat history
                ChatHistory history = new ChatHistory();

                // Generate 5 unique comments
                // Set up chat history for generating varied comments
                history.AddUserMessage($"You are a social media expert. Write a unique, engaging comments for the following post, with a total of approximately "+ wordLimit + " words:\r\nPost: " + description + "\r\nComment:");
                //"You are a social media expert. Write five unique, engaging comments for the following post, with a total of approximately 120 words. Only separate each comment with |||SPLIT||| without adding any other words, symbols, or formatting."
                List<string> generatedComments = new List<string>();
                for (int i = 0; i < NoOfCommentsGenerated; i++)
                {

                    // Get chat completion response
                    var response = await chatCompletionService.GetChatMessageContentsAsync(history);

                    string comment = "";
                    foreach (var res in response)
                    {
                        comment += res;
                    }

                    //foreach (var i in comment.Split("|||SPLIT|||"))
                    //{
                    listComments.Add(new GeneratedComments { id = Guid.NewGuid(), generatedComment = comment });
                    // }


                    // Add response to chat history for context
                    history.AddAssistantMessage("create a different comment");
                }
                return listComments;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
    public static class ResourceDetails
    {
        public static string name { get; set; } = string.Empty;
    }
}
