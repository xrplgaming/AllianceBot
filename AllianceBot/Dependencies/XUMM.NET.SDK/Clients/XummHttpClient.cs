﻿using AllianceBot;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XUMM.NET.SDK.Models;

namespace XUMM.NET.SDK.EMBRS
{
    public class XummHttpClient : IXummHttpClient
    {
        private readonly ApiConfig _config;
        private readonly JsonSerializerOptions _serializerOptions;

        public XummHttpClient()
        {
            _config = new ApiConfig();
            _config.ApiKey = Settings.ApiKey;
            _config.ApiSecret = Settings.ApiSecret;
            _config.RestClientAddress = Settings.RestClientAddress;

            _serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            return await SendAsync<T>(HttpMethod.Get, endpoint, true, default);
        }

        public async Task<T> GetPublicAsync<T>(string endpoint)
        {
            return await SendAsync<T>(HttpMethod.Get, endpoint, false, default);
        }

        public async Task<T> PostAsync<T>(string endpoint, object content)
        {
            return await PostAsync<T>(endpoint, JsonSerializer.Serialize(content, _serializerOptions));
        }

        public async Task<T> PostAsync<T>(string endpoint, string json)
        {
            return await SendAsync<T>(HttpMethod.Post, endpoint, true, json);
        }

        public async Task<T> DeleteAsync<T>(string endpoint)
        {
            return await SendAsync<T>(HttpMethod.Delete, endpoint, true, default);
        }

        public HttpClient GetHttpClient(bool setCredentials)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();

            if (setCredentials)
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
                httpClient.DefaultRequestHeaders.Add("X-API-Secret", _config.ApiSecret);
            }

            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "XUMM-Net");
            return httpClient;
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string endpoint, bool setCredentials, string? json)
        {
            try
            {
                using var client = GetHttpClient(setCredentials);
                using var requestMessage = new HttpRequestMessage(method, GetRequestUrl(endpoint));

                if (json != null)
                {
                    requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                using var response = await client.SendAsync(requestMessage);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw GetHttpRequestException(response, responseText);
                }

                var result = JsonSerializer.Deserialize<T>(responseText);
                if (result == null)
                {
                    throw new Exception($"Unexpected response for {endpoint} response");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, $"Unexpected response from XUMM API [{method}:{endpoint}]");
                throw;
            }
        }

        private HttpRequestException GetHttpRequestException(HttpResponseMessage response, string responseText)
        {
            HttpRequestException? exception = null;

            try
            {
                var fatalApiError = JsonSerializer.Deserialize<XummFatalApiError>(responseText);
                if (fatalApiError != null)
                {
                    if (!string.IsNullOrWhiteSpace(fatalApiError.Message))
                    {
                        exception = new HttpRequestException(fatalApiError.Message, null, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, $"No {nameof(XummFatalApiError)} available in unsuccessful response body of request: {response.RequestMessage?.RequestUri}");
            }

            try
            {
                if (exception == null)
                {
                    var apiError = JsonSerializer.Deserialize<XummApiError>(responseText);
                    if (apiError != null)
                    {
                        var message = $"Error code {apiError.Error.Code}, see XUMM Dev Console, reference: '{apiError.Error.Reference}'.";

                        exception = new HttpRequestException(message, null, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, $"No {nameof(XummApiError)} available in unsuccessful response body of request: {response.RequestMessage?.RequestUri}");
            }

            exception ??= new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);

            return exception;
        }

        private string GetRequestUrl(string endpoint)
        {
            var result = _config.RestClientAddress;
            if (!result.EndsWith('/'))
            {
                result += "/";
            }

            result += $"platform/{endpoint}";

            return result;
        }
    }
}