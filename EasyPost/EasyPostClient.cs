﻿/*
 * Licensed under The MIT License (MIT)
 *
 * Copyright (c) 2014 EasyPost
 * Copyright (C) 2017 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;

namespace EasyPost
{
    public partial class EasyPostClient : IEasyPostClient
    {
        private readonly RestClient _restClient;
        private readonly ClientConfiguration _configuration;

        /// <summary>
        /// Returns the current client version
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Returns the API base URL for the configured client
        /// </summary>
        public string ApiBase => _configuration.ApiBase;

        /// <summary>
        /// Create a new EasyPost client
        /// </summary>
        /// <param name="apiKey">API key to use</param>
        public EasyPostClient(
            string apiKey)
            : this(new ClientConfiguration(apiKey))
        {
        }

        /// <summary>
        /// Create a new EasyPost client
        /// </summary>
        /// <param name="apiKey">API key to use</param>
        /// <param name="timeout">The timeout to use for client operations. 0 for the default.</param>
        public EasyPostClient(
            string apiKey,
            int timeout)
            : this(new ClientConfiguration(apiKey, timeout))
        {
        }

        /// <summary>
        /// Create a new EasyPost client
        /// </summary>
        /// <param name="clientConfiguration">Client configuration to use</param>
        public EasyPostClient(
            ClientConfiguration clientConfiguration)
        {
            if (clientConfiguration == null) {
                throw new ArgumentNullException(nameof(clientConfiguration));
            }
            _configuration = clientConfiguration;
            _restClient = ClientFactory.GetClient(clientConfiguration.ApiBase);

            var assembly = Assembly.GetExecutingAssembly();
            var info = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = info.FileVersion;
        }

        /// <summary>
        /// Internal function to execute a request
        /// </summary>
        /// <param name="request">EasyPost request to execute</param>
        private Task<RestResponse> Execute(
            EasyPostRequest request)
        {
            return _restClient.ExecuteAsync(PrepareRequest(request));
        }

        /// <summary>
        /// Internal function to execute a typed request
        /// </summary>
        /// <typeparam name="TResponse">Type of the JSON response we are expecting</typeparam>
        /// <param name="request">EasyPost request to execute</param>
        /// <returns>Response for the request</returns>
        private async Task<TResponse> Execute<TResponse>(
            EasyPostRequest request) where TResponse : new()
        {
            var response = await _restClient.ExecuteAsync<TResponse>(PrepareRequest(request)).ConfigureAwait(false);
            var statusCode = response.StatusCode;
            var data = response.Data;

            if (data == null || statusCode >= HttpStatusCode.BadRequest) {
                // Bail early if this is not an EasyPost object
                var result = data as EasyPostObject;
                RequestError requestError = null;
                if (result == null) {
                    // Return the RestSharp error message if we can
                    data = new TResponse();
                    result = data as EasyPostObject;
                    if (response.ErrorMessage == null || result == null) {
                        return default;
                    }
                    requestError = new RequestError {
                        Code = "RESPONSE.ERROR",
                        Message = response.ErrorMessage,
                        Errors = new List<Error>(),
                    };
                } else {
                    // Try to parse any generic EasyPost request errors first
                    var json = GoToRootElement(response.Content, new List<string> { "error" });
                    if (!string.IsNullOrEmpty(json)) {
                        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                        requestError = JsonSerializer.Deserialize<RequestError>(json, options);
                    }
                    if (requestError?.Code == null) {
                        // Can't make sense of the error so return a general one
                        requestError = new RequestError {
                            Code = "RESPONSE.PARSE_ERROR",
                            Message = "Unknown request error or unable to parse response",
                            Errors = new List<Error>(),
                        };
                    }
                }
                requestError.StatusCode = statusCode;
                requestError.Content = response.Content;
                result.RequestError = requestError;
            }

            return data;
        }

        /// <summary>
        /// Venture through the root element keys to find the root element of the JSON string.
        /// </summary>
        /// <param name="data">A string of JSON data</param>
        /// <param name="rootElementKeys">List, in order, of sub-keys path to follow to deserialization starting position.</param>
        /// <returns>The value of the JSON sub-element</returns>
        private static string GoToRootElement(
            string data,
            List<string> rootElementKeys)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(data);
            try {
                rootElementKeys.ForEach(key => { json = json.GetProperty(key); });
                return json.ToString();
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Internal function to prepare the request to be executed
        /// </summary>
        /// <param name="request">EasyPost request to be executed</param>
        /// <returns>RestSharp request to execute</returns>
        internal RestRequest PrepareRequest(
            EasyPostRequest request)
        {
            var restRequest = request.RestRequest;

            restRequest.Timeout = _configuration.Timeout;
            restRequest.AddHeader("user_agent", $"EasyPost/CSharpASync/{Version}");
            restRequest.AddHeader("authorization", $"Bearer {_configuration.ApiKey}");

            return restRequest;
        }
    }
}