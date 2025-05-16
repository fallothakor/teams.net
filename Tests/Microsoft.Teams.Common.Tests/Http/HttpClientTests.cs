﻿
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.Teams.Api.SignIn;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common.Http;

using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Common.Tests.Http;

public class HttpClientTests
{


    [Fact]
    public async Task HttpClient_ShouldReturnExpectedResponse_WhenMocked()
    {
        // Arrange
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("Mocked response")
               });

        var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(mockMessageHandler.Object));
        HttpRequest request = HttpRequest.Get("https://www.microsoft.com");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Mocked response", response.Body);
    }

    [Fact]
    public async Task HttpClient_ShouldReturnExpectedResponseWithHeaders()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("Mocked response"),
               });

        var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(mockMessageHandler.Object));
        HttpRequest request = HttpRequest.Get("https://www.microsoft.com");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Mocked response", response.Body);
    }

    [Fact]
    public async Task HttpClient_ShouldReturnExpectedResponse_ResponseObject()
    {
        // Arrange
        var urlResponse = new UrlResponse()
        {
            SignInLink = "valid signin dataa",
            TokenExchangeResource = new Api.TokenExchange.Resource()
            {
                Id = "id",
                ProviderId = "providerId",
                Uri = "uri",
            },
            TokenPostResource = new Api.Token.PostResource()
            {
                SasUrl = "valid sas url",
            }
        };
        var urlResponseJson = JsonSerializer.Serialize(urlResponse, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(urlResponseJson, Encoding.UTF8, "application/json"),
               });

        var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(mockMessageHandler.Object));
        HttpRequest request = HttpRequest.Get("https://www.microsoft.com");

        // Act
        var response = await httpClient.SendAsync<UrlResponse>(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(urlResponse.ToString(), response.Body.ToString());
    }

    //[Fact]
    //public void HttpClient_ShouldDisposeClient()
    //{
    //    // Arrange
    //    var mockMessageHandler = new Mock<HttpMessageHandler>();
    //    mockMessageHandler.Protected().Setup("Dispose");
    //    var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(mockMessageHandler.Object));
    //    // Act
    //    httpClient.Dispose();
    //    // Assert
    //    mockMessageHandler.Protected().Verify("Dispose", Times.Once());
    //}

    [Fact]
    public void HttpClient_ShouldDisposeClientWithNullHandler()
    {
        // Arrange
        var httpClient = new Common.Http.HttpClient();
        // Act
        httpClient.Dispose();
        // Assert
        Assert.True(true); // No exception should be thrown
    }

    public class MockHttpClient : Common.Http.HttpClient
    {
        public HttpRequestMessage ValidateCreateRequest(HttpRequest request)
        {
            var httpRequestMessage = CreateRequest(request);
            return httpRequestMessage;
        }
    }

    [Fact]
    public void HttpClient_ShouldSetRequestHeaders_CustomHeader()
    {
        // Arrange
        var customRequestHeader = new List<string> { "HeaderValue", "someOther value" };
        HttpRequest request = HttpRequest.Get("https://www.microsoft.com");
        request.AddHeader("Custom-Header", customRequestHeader);

        var mockHttpclient = new MockHttpClient();

        // Act
        var httpRequestMessage = mockHttpclient.ValidateCreateRequest(request);

        // Assert
        Assert.Equal("GET", httpRequestMessage.Method.Method);
        var httpRequestHeaders = httpRequestMessage.Headers.GetValues("Custom-Header").ToList();
        Assert.Equal("HeaderValue", httpRequestHeaders[0]);
        Assert.Equal("someOther value", httpRequestHeaders[1]);
    }

    [Fact]
    public void HttpClient_ShouldSetRequestHeaders_BodyAsDictionary()
    {
        // Arrange
        var customRequestHeader = new List<string> { "HeaderValue", "someOther value" };
        HttpRequest request = HttpRequest.Post("https://www.microsoft.com");
        request.AddHeader("Custom-Header", customRequestHeader);
        request.AddHeader("Content-Type", "application/json");
        request.Body = new Dictionary<string, string>()
        {
            { "grant_type", "client_credentials" },
            { "client_id", "ClientId" },
            { "client_secret", "ClientSecret" },
            { "scope", "scope" }
        };

        var mockHttpclient = new MockHttpClient();

        // Act
        var httpRequestMessage = mockHttpclient.ValidateCreateRequest(request);

        // Assert
        Assert.Equal("POST", httpRequestMessage.Method.Method);
        var httpRequestHeaders = httpRequestMessage.Headers.GetValues("Custom-Header").ToList();
        Assert.Equal("HeaderValue", httpRequestHeaders[0]);
        Assert.Equal("someOther value", httpRequestHeaders[1]);

        var contentTypeHeader = httpRequestMessage.Content?.Headers.GetValues("Content-Type").ToList();
        Assert.Equal(2, contentTypeHeader!.Count);
        Assert.Equal("application/x-www-form-urlencoded", httpRequestMessage.Content?.Headers.ContentType?.MediaType);
        Assert.Equal("application/x-www-form-urlencoded", contentTypeHeader[0]);
        Assert.Equal("application/json", contentTypeHeader[1]);

        // TODO : Check the content of the request body 
        //var requestBody = httpRequestMessage.Content?.ToString();
        //Assert.Contains("grant_type=client_credentials", requestBody);

    }

    [Fact]
    public void HttpClient_ShouldSetRequestHeaders_BodyAsString()
    {
        // Arrange
        var customRequestHeader = new List<string> { "HeaderValue", "someOther value" };
        HttpRequest request = HttpRequest.Post("https://www.microsoft.com");
        request.AddHeader("Custom-Header", customRequestHeader);
        request.AddHeader("Content-Type", "application/json");
        request.Body = "post data";

        var mockHttpclient = new MockHttpClient();

        // Act
        var httpRequestMessage = mockHttpclient.ValidateCreateRequest(request);

        // Assert
        Assert.Equal("POST", httpRequestMessage.Method.Method);
        var httpRequestHeaders = httpRequestMessage.Headers.GetValues("Custom-Header").ToList();
        Assert.Equal("HeaderValue", httpRequestHeaders[0]);
        Assert.Equal("someOther value", httpRequestHeaders[1]);

        var contentTypeHeader = httpRequestMessage.Content?.Headers.GetValues("Content-Type").ToList();
        Assert.Equal(2, contentTypeHeader!.Count);
        Assert.Equal("text/plain", httpRequestMessage.Content?.Headers.ContentType?.MediaType);
        Assert.Equal("text/plain; charset=utf-8", contentTypeHeader[0]);
        Assert.Equal("application/json", contentTypeHeader[1]);

        // TODO : Check the content of the request body 
    }

    [Fact]
    public async Task HttpClient_ShouldSetRequestHeaders_BodyAsJsonObject()
    {
        // Arrange
        var tokenData = new Api.Tabs.Request()
        {
            Context = new Api.Tabs.Context()
            {
                Theme = "default",
            },
            State = "state",
            TabContext = new Api.Tabs.EntityContext()
            {
                TabEntityId = "tabEntityId",
            }
        };

        var customRequestHeader = new List<string> { "HeaderValue", "someOther value" };
        HttpRequest request = HttpRequest.Post("https://www.microsoft.com");
        request.AddHeader("Custom-Header", customRequestHeader);
        request.AddHeader("Content-data", "valid");
        request.Body = tokenData;

        var mockHttpclient = new MockHttpClient();

        // Act
        var httpRequestMessage = mockHttpclient.ValidateCreateRequest(request);

        // Assert
        Assert.Equal("POST", httpRequestMessage.Method.Method);
        var httpRequestHeaders = httpRequestMessage.Headers.GetValues("Custom-Header").ToList();
        Assert.Equal("HeaderValue", httpRequestHeaders[0]);
        Assert.Equal("someOther value", httpRequestHeaders[1]);

        Assert.NotNull(httpRequestMessage.Content);
        var contentTypeHeader = httpRequestMessage.Content.Headers.GetValues("Content-Type").ToList();
        Assert.NotNull(contentTypeHeader);
        Assert.Single(contentTypeHeader!);
        Assert.Equal("application/json", httpRequestMessage.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/json; charset=utf-8", contentTypeHeader[0]);

        var deserializedContent = await httpRequestMessage.Content.ReadFromJsonAsync<Api.Tabs.Request>();
        Assert.Equal(tokenData.ToString(), deserializedContent!.ToString());
    }
}