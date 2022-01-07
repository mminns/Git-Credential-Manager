using System;
using System.Collections.Generic;
using System.Net.Http;
using GitCredentialManager;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class OAuth2ClientRegistryTest
    {
        private Mock<ICommandContext> context = new Mock<ICommandContext>(MockBehavior.Loose);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Strict);
        private Mock<IHttpClientFactory> httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        private Mock<ITrace> trace = new Mock<ITrace>(MockBehavior.Strict);

        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsCloudOAuth2Client_ForBitbucketOrg()
        {
            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://bitbucket.org"));
            context.Setup(c => c.Settings).Returns(settings.Object);   
            var clientId = "never used";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientId,
                out clientId)).Returns(false);
            var clientSecret = "never used";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out clientSecret)).Returns(false);
            var redirectUrl = "never used";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out redirectUrl)).Returns(false); 
            context.Setup(c => c.HttpClientFactory).Returns(httpClientFactory.Object);
            httpClientFactory.Setup(f => f.CreateClient()).Returns(new HttpClient());

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "bitbucket.org",
            });

            // When
            var registry = new OAuth2ClientRegistry(context.Object);
            var api = registry.Get(input);
        
            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.Cloud.BitbucketOAuth2Client>(api);

        }


        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsDataCenterOAuth2Client_ForBitbucketDC()
        {
            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://example.com"));
            context.Setup(c => c.Settings).Returns(settings.Object);

            var clientId = "";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientId,
                out clientId)).Returns(true);
            var clientSecret = "";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out clientSecret)).Returns(true);
            var redirectUrl = "never used";
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out redirectUrl)).Returns(false);


            context.Setup(c => c.HttpClientFactory).Returns(httpClientFactory.Object);
            httpClientFactory.Setup(f => f.CreateClient()).Returns(new HttpClient());

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "example.com",
            });

            // When
            var registry = new OAuth2ClientRegistry(context.Object);
            var api = registry.Get(input);
        
            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.DataCenter.BitbucketOAuth2Client>(api);

        }

    }
}