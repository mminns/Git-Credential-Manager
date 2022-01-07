using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class BitbucketOAuth2ClientTest
    {
        private Mock<HttpClient> httpClient = new Mock<HttpClient>(MockBehavior.Strict);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Loose);
        private Mock<Trace> trace = new Mock<Trace>(MockBehavior.Loose);
        private Mock<IOAuth2WebBrowser> browser = new Mock<IOAuth2WebBrowser>(MockBehavior.Strict);
        private Mock<IOAuth2CodeGenerator> codeGenerator = new Mock<IOAuth2CodeGenerator>(MockBehavior.Strict);
        private IEnumerable<string> scopes = new List<string>();
        private CancellationToken ct = new CancellationToken();
        private Uri rootCallbackUri = new Uri("http://localhost:34106/");
        private string nonce = "12345";
        private string pkceCodeVerifier = "abcde";
        private string pkceCodeChallenge = "xyz987";
        private string authorization_code = "authorization_token";

        [Fact]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_ReturnsCode()
        {
            //MockRedirectUriOverride(""http://localhost:12345/"");
            var remoteUrl = MockRemoteUri("http://example.com");
            var clientId = MockClientIdOverride("dc-client-id");
            MockClientSecretOverride("dc-client-seccret");

            Uri finalCallbackUri = MockFinalCallbackUri(rootCallbackUri);

            MockGetAuthenticationCodeAsync(remoteUrl, rootCallbackUri, finalCallbackUri, clientId);

            MockCodeGenerator();

            var client = GetBitbucketOAuth2Client();

            var result = await client.GetAuthorizationCodeAsync(scopes, browser.Object, ct);

            VerifyAuthorizationCodeResult(result, rootCallbackUri);
        }

        [Fact]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_ReturnsCode_WhileRespectingRedirectUriOverride()
        {
            var rootCallbackUrl = MockRootCallbackUriOverride("http://localhost:12345/");
            var remoteUrl = MockRemoteUri("http://example.com");
            var clientId = MockClientIdOverride("dc-client-id");
            MockClientSecretOverride("dc-client-seccret");

            Uri finalCallbackUri = MockFinalCallbackUri(new Uri(rootCallbackUrl));

            MockGetAuthenticationCodeAsync(remoteUrl, new Uri(rootCallbackUrl), finalCallbackUri, clientId);

            MockCodeGenerator();

            var client = GetBitbucketOAuth2Client();

            var result = await client.GetAuthorizationCodeAsync(scopes, browser.Object, ct);

            VerifyAuthorizationCodeResult(result, new Uri(rootCallbackUrl));
        }

        private void VerifyAuthorizationCodeResult(OAuth2AuthorizationCodeResult result, Uri redirectUri)
        {
            Assert.NotNull(result);
            Assert.Equal(authorization_code, result.Code);
            Assert.Equal(redirectUri, result.RedirectUri);
            Assert.Equal(pkceCodeVerifier, result.CodeVerifier);
        }

        private BitbucketOAuth2Client GetBitbucketOAuth2Client()
        {
            var client = new BitbucketOAuth2Client(httpClient.Object, settings.Object, trace.Object);
            client.CodeGenerator = codeGenerator.Object;
            return client;
        }

        private void MockCodeGenerator()
        {
            codeGenerator.Setup(c => c.CreateNonce()).Returns(nonce);
            codeGenerator.Setup(c => c.CreatePkceCodeVerifier()).Returns(pkceCodeVerifier);
            codeGenerator.Setup(c => c.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, pkceCodeVerifier)).Returns(pkceCodeChallenge);
        }

        private void MockGetAuthenticationCodeAsync(string url, Uri redirectUri, Uri finalCallbackUri, string overrideClientId)
        {
            var authorizationUri = new UriBuilder(url + "/rest/oauth2/latest/authorize")
            {
                Query = "?response_type=code"
             + "&client_id=" + (overrideClientId ?? "clientId")
             + "&state=12345"
             + "&code_challenge_method=" + OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256
             + "&code_challenge=" + WebUtility.UrlEncode(pkceCodeChallenge).ToLower()
             + "&redirect_uri=" + WebUtility.UrlEncode(redirectUri.AbsoluteUri).ToLower()
            }.Uri;

            browser.Setup(b => b.GetAuthenticationCodeAsync(authorizationUri, redirectUri, ct)).Returns(Task.FromResult(finalCallbackUri));
        }

        private Uri MockFinalCallbackUri(Uri redirectUri)
        {
            var finalUri = new Uri(rootCallbackUri, "?state=" + nonce + "&code=" + authorization_code);
            // This is a simplification but consistent
            browser.Setup(b => b.UpdateRedirectUri(redirectUri)).Returns(redirectUri);
            return finalUri;
        }

        private string MockRemoteUri(string value)
        {
            settings.Setup(s => s.RemoteUri).Returns(new Uri(value));
            return value;
        }

        private string MockClientIdOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientId,
                out value)).Returns(true);
            return value;
        }

        private string MockClientSecretOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out value)).Returns(true);
            return value;
        }

        private string MockRootCallbackUriOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out value)).Returns(true);
            return value;
        }
    }
}