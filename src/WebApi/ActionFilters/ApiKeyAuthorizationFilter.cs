﻿using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WebApi.Models;
using WebApi.Models.Configuration;
using Common.Extensions;
using WebApi.Contracts.Errors;
using System.Net;

namespace WebApi.ActionFilters
{
    public class ApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        public const string API_HEADER = "Api-Key";

        private readonly string _apiKey;
        private readonly ILogger<ApiKeyAuthorizationFilter> _logger;

        public ApiKeyAuthorizationFilter(
            ILogger<ApiKeyAuthorizationFilter> logger,
            IOptions<DevKnowledgeBookConfiguration> devKnowledgeBookConfig)
        {
            _apiKey = devKnowledgeBookConfig.Value.ApiKey;
            _logger = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var request = context.GetRequestPath();
            bool hasApiKeyHeader = context.HttpContext.Request.Headers.TryGetValue(API_HEADER, out StringValues clientApiKey);

            if (!hasApiKeyHeader)
            {
                var errorMessage = $"{API_HEADER} header missing.";
                var errorResponse = new ErrorResponse(request, HttpStatusCode.Forbidden, ErrorTitle.ApiKeyHeaderMissing, errorMessage);

                _logger.LogWarning(errorMessage);
                context.Result = new DevKnowledgeBookUnauthorizedResult(errorResponse);
                return;
            }

            if (!clientApiKey.ToString().Equals(_apiKey)) //TODO: look into which StringComparison to use if any
            {
                var errorMessage = $"{API_HEADER} is invalid.";
                var errorResponse = new ErrorResponse(request, HttpStatusCode.Forbidden, ErrorTitle.ApiKeyDidNotMatch, errorMessage);

                _logger.LogWarning(errorMessage);
                context.Result = new DevKnowledgeBookUnauthorizedResult(errorResponse);
                return;
            }
        }
    }
}
