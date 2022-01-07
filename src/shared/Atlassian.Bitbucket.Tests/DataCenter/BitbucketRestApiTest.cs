using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager.Tests;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class BitbucketRestApiTest
    {
        [Fact]
        public async Task BitbucketRestApi_GetUserInformationAsync_ReturnsUserInfo_ForSuccessfulRequest_DoesNothing() 
        {
            var twoFactorAuthenticationEnabled = false;

            var context = new TestCommandContext();

            var httpHandler = new TestHttpMessageHandler();
            context.HttpClientFactory.MessageHandler = httpHandler;
            
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);
            var result = await api.GetUserInformationAsync("never used", "never used", false);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Response.UserName);
            Assert.Equal(twoFactorAuthenticationEnabled, result.Response.IsTwoFactorAuthenticationEnabled);
            Assert.Null(((UserInfo)result.Response).AccountId);
            Assert.Equal(Guid.Empty, ((UserInfo)result.Response).Uuid);

            httpHandler.AssertNoRequests();
        }
    
        [Fact]
        public async Task BitbucketRestApi_IsOAuthInstalledAsync_ReturnsTrue_WhenSupported()
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/oauth2/1.0/client");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var isInstalled = await api.IsOAuthInstalledAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.True(isInstalled);
        }

        [Fact]
        public async Task BitbucketRestApi_IsOAuthInstalledAsync_ReturnsFalse_WhenNotSupported()
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/oauth2/1.0/client");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var isInstalled = await api.IsOAuthInstalledAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.False(isInstalled);
        }

        [Fact]
        public async Task BitbucketRestApi_GetAuthenticationMethodsAsync_ReturnsOnlyBasicAuth_WhenNoIdp()
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/authconfig/1.0/login-options");
            
            var loginOptionResponseJson = $"{{ \"results\":[ {{ \"type\":\"LOGIN_FORM\"}}]}}";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(loginOptionResponseJson)
            };

            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var authMethods = await api.GetAuthenticationMethodsAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.NotNull(authMethods);
            Assert.Contains(AuthenticationMethod.BASIC_AUTH, authMethods);
            Assert.DoesNotContain(AuthenticationMethod.SSO, authMethods);
        }

        [Fact]
        public async Task BitbucketRestApi_GetAuthenticationMethodsAsync_ReturnsOnlySso_WhenNoLoginForm()
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/authconfig/1.0/login-options");
            
            var loginOptionResponseJson = $"{{ \"results\":[{{\"type\":\"IDP\"}}]}}";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(loginOptionResponseJson)
            };

            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var authMethods = await api.GetAuthenticationMethodsAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.NotNull(authMethods);
            Assert.DoesNotContain(AuthenticationMethod.BASIC_AUTH, authMethods);
            Assert.Contains(AuthenticationMethod.SSO, authMethods);
        }

        [Fact]
        public async Task BitbucketRestApi_GetAuthenticationMethodsAsync_ReturnsBasciAndSso_WhenSupported()
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/authconfig/1.0/login-options");
            
            var loginOptionResponseJson = $"{{ \"results\":[{{\"type\":\"IDP\"}}, {{ \"type\":\"LOGIN_FORM\"}},  {{ \"type\":\"UNDEFINED\"}}]}}";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(loginOptionResponseJson)
            };

            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var authMethods = await api.GetAuthenticationMethodsAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.NotNull(authMethods);
            Assert.Equal(2, authMethods.Count);
            Assert.Contains(AuthenticationMethod.BASIC_AUTH, authMethods);
            Assert.Contains(AuthenticationMethod.SSO, authMethods);
        }
    }
}