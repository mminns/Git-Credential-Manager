﻿using Atlassian.Bitbucket.Cloud;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketHostProviderTest
    {
        #region Tests

        private const string MOCK_ACCESS_TOKEN = "at-0987654321";
        private const string MOCK_REFRESH_TOKEN = "rt-1234567809";
        private const string BITBUCKET_DOT_ORG_HOST = "bitbucket.org";
        private const string DC_SERVER_HOST = "example.com";
        private Mock<IBitbucketAuthentication> bitbucketAuthentication = new Mock<IBitbucketAuthentication>(MockBehavior.Strict);
        private Mock<IBitbucketRestApi> bitbucketApi = new Mock<IBitbucketRestApi>(MockBehavior.Strict);

        [Theory]
        [InlineData("https", null, false)]
        // We report that we support unencrypted HTTP here so that we can fail and
        // show a helpful error message in the call to `GenerateCredentialAsync` instead.
        [InlineData("http", BITBUCKET_DOT_ORG_HOST, true)]
        [InlineData("ssh", BITBUCKET_DOT_ORG_HOST, false)]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, true)]
        [InlineData("https", "api.bitbucket.org", true)] // Currently does support sub domains.

        [InlineData("https", "bitbucket.ogg", false)] // No support of phony similar tld.
        [InlineData("https", "bitbucket.com", false)] // No support of wrong tld.
        [InlineData("https", DC_SERVER_HOST, false)] // No support of non bitbucket domains.

        [InlineData("http", "bitbucket.my-company-server.com", false)]  // Currently no support for named on-premise instances
        [InlineData("https", "my-company-server.com", false)]
        [InlineData("https", "bitbucket.my.company.server.com", false)]
        [InlineData("https", "api.bitbucket.my-company-server.com", false)]
        [InlineData("https", "BITBUCKET.My-Company-Server.Com", false)]
        public void BitbucketHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void BitbucketHostProvider_IsSupported_FailsForNullInput()
        {
            InputArguments input = null;
            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void BitbucketHostProvider_IsSupported_FailsForNullHttpResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = null;
            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(httpResponseMessage));
        }

        [Theory]
        [InlineData("X-AREQUESTID", "123456789", true)] // only the specific header is acceptable
        [InlineData("X-REQUESTID", "123456789", false)]
        [InlineData(null, null, false)]
        public void BitbucketHostProvider_IsSupported_HttpResponseMessage(string header, string value, bool expected)
        {
            var input = new HttpResponseMessage();
            if (header != null)
            {
                input.Headers.Add(header, value);
            }

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public void BitbucketHostProvider_GetCredentialAsync_Succeeds_ForValidStoredBasicAuthAccount(string protocol, string host, string username,string password)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, input, password);
            MockRemoteOAuthAccountIsInvalid(bitbucketApi);
            MockRemoteBasicAuthAccountIsValidNo2FA(bitbucketApi, input, password, username);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            //verify credentials were validated
            VerifyValidateBasicAuthCredentialsRan();

            // Stored credentials so don't ask for more
            VerifyInteractiveBasicAuthFlowNeverRan(password, input, credential);

            // Valid Basic Auth credentials so don't run Oauth
            VerifyInteractiveOAuthFlowNeverRan(input, credential);
        }

        public Mock<IBitbucketRestApi> GetBitbucketApi()
        {
            return bitbucketApi;
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        public void BitbucketHostProvider_GetCredentialAsync_Succeeds_ForValidStoredOAuthAccount(string protocol, string host, string username, string token)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, input, token);
            MockRemoteOAuthAccountIsValid(bitbucketApi, input, token, username, twoFAEnabled: false);
            
            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            //verify bitbucket.org credentials were validated
            VerifyValidateOAuthCredentialsRan();

            // Stored credentials so don't ask for more
            VerifyInteractiveBasicAuthFlowNeverRan(token, input, credential);

            // Valid Basic Auth credentials so don't run Oauth
            VerifyInteractiveOAuthFlowNeverRan(input, credential);
        }

        private void MockDCSSOEnabled()
        {
            bitbucketApi.Setup(ba => ba.GetAuthenticationMethodsAsync()).Returns(Task.FromResult(new List<AuthenticationMethod>(){AuthenticationMethod.BASIC_AUTH, AuthenticationMethod.SSO}));
            bitbucketApi.Setup(ba => ba.IsOAuthInstalledAsync()).Returns(Task.FromResult(true));
        }

        [Theory]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        // cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public void BitbucketHostProvider_GetCredentialAsync_Succeeds_ForFreshValidBasicAuthAccount(string protocol, string host, string username, string password)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockUserEntersValidBasicCredentials(bitbucketAuthentication, input, password);

            if (BITBUCKET_DOT_ORG_HOST.Equals(host))
            {
                MockRemoteOAuthAccountIsValid(bitbucketApi, input, password, username, twoFAEnabled: true);
            }

            MockRemoteBasicAuthAccountIsValidNo2FA(bitbucketApi, input, password, username);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            VerifyBasicAuthFlowRan(password, true, input, credential, null);

            VerifyOAuthFlowDidNotRun(password, true, input, credential);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_ACCESS_TOKEN)]
        public void BitbucketHostProvider_GetCredentialAsync_Succeeds_ForFreshValid2FAAcccount(string protocol, string host, string username, string password)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // user is prompted for basic auth credentials
            MockUserEntersValidBasicCredentials(bitbucketAuthentication, input, password);
            // basic auth credentials are valid but 2FA is ON
            MockRemoteBasicAuthAccountIsValidRequires2FA(bitbucketApi, input, password, username);
            MockRemoteOAuthAccountIsValid(bitbucketApi, input, password, username, twoFAEnabled: true);
            MockRemoteValidRefreshToken(input);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            VerifyOAuthFlowRan(password, false, true, input, credential, null);

            VerifyBasicAuthFlowNeverRan(password, input, false, null);
        }

        [Theory]
        // cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "basic")]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "oauth")]
        // Basic Auth works
        public void BitbucketHostProvider_GetCredentialAsync_ForcedAuthMode_IsRespected(string protocol, string host, string username, string password,
            string preconfiguredAuthModes)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            if (preconfiguredAuthModes != null)
            {
                context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, preconfiguredAuthModes);
            }

            MockUserEntersValidBasicCredentials(bitbucketAuthentication, input, password);
            MockRemoteBasicAuthAccountIsValidRequires2FA(bitbucketApi, input, password, username);
            bitbucketAuthentication.Setup(m => m.ShowOAuthRequiredPromptAsync()).ReturnsAsync(true);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            Assert.NotNull(credential);

            if (preconfiguredAuthModes.Contains("basic"))
            {
                VerifyInteractiveBasicAuthFlowRan(password, input, credential);
                VerifyInteractiveOAuthFlowNeverRan(input, credential);
            }

            if (preconfiguredAuthModes.Contains("oauth"))
            {
                VerifyInteractiveBasicAuthFlowNeverRan(password, input, credential);
                VerifyInteractiveOAuthFlowRan(password, input, credential);
            }
        }

        [Theory]
        // cloud
        //[InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "false")]
        //[InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "0")]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "true")]
        //[InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", "1")]
        //[InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password", null)]
        // DC
        //[InlineData("https", DC_SERVER_HOST, "jsquire", "password", "false")]
        //[InlineData("https", DC_SERVER_HOST, "jsquire", "password", "0")]
        //[InlineData("https", DC_SERVER_HOST, "jsquire", "password", "1")]
        //[InlineData("https", DC_SERVER_HOST, "jsquire", "password", "true")]
        //[InlineData("https", DC_SERVER_HOST, "jsquire", "password", null)]
        public void BitbucketHostProvider_GetCredentialAsync_AlwaysRefreshCredentials_IsRespected(string protocol, string host, string username, string password,
            string alwaysRefreshCredentials)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            if (alwaysRefreshCredentials != null)
            {
                context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials, alwaysRefreshCredentials);
            }

            MockStoredAccount(context, input, password);
            MockUserEntersValidBasicCredentials(bitbucketAuthentication, input, password);
            MockRemoteOAuthAccountIsValid(bitbucketApi, input, password, username, twoFAEnabled: true);
            MockRemoteBasicAuthAccountIsValidNo2FA(bitbucketApi, input, password, username);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            var credential = provider.GetCredentialAsync(input);

            var alwaysRefreshCredentialsBool = "1".Equals(alwaysRefreshCredentials)
                || "on".Equals(alwaysRefreshCredentials)
                || "true".Equals(alwaysRefreshCredentials) ? true : false;

            if (alwaysRefreshCredentialsBool)
            {
                VerifyBasicAuthFlowRan(password, true, input, credential, null);
            }
            else
            {
                VerifyBasicAuthFlowNeverRan(password, input, true, null);
            }

            VerifyOAuthFlowDidNotRun(password, true, input, credential);
        }

        [Theory]
        // DC - supports Basic, OAuth
        [InlineData("https", "example.com", "basic", AuthenticationModes.Basic)]
        [InlineData("https", "example.com", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https", "example.com", "NOT-A-REAL-VALUE", DataCenterConstants.ServerAuthenticationModes)]
        [InlineData("https", "example.com", "none", DataCenterConstants.ServerAuthenticationModes)]
        [InlineData("https", "example.com", null, DataCenterConstants.ServerAuthenticationModes)]
        // cloud - supports Basic, OAuth
        [InlineData("https", "bitbucket.org", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https", "bitbucket.org", "basic", AuthenticationModes.Basic)]
        [InlineData("https", "bitbucket.org", "NOT-A-REAL-VALUE", CloudConstants.DotOrgAuthenticationModes)]    
        [InlineData("https", "bitbucket.org", "none", CloudConstants.DotOrgAuthenticationModes)]
        [InlineData("https", "bitbucket.org", null, CloudConstants.DotOrgAuthenticationModes)]
        public async Task BitbucketHostProvider_GetSupportedAuthenticationModes(string protocol, string host, string bitbucketAuthModes, AuthenticationModes expectedModes)
        {
            var input = MockInput(protocol, host, null);

            var context = new TestCommandContext { };
            if (bitbucketAuthModes != null)
            {
                context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, bitbucketAuthModes);
            }

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(input);

            Assert.Equal(expectedModes, actualModes);
        }

        [Theory]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        [InlineData("http", DC_SERVER_HOST, "jsquire", "password")]
        // cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        [InlineData("http", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_ValidateTargetUriAsync(string protocol, string host, string username, string password)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            if (protocol.ToLower().Equals("http") && host.ToLower().Equals(BITBUCKET_DOT_ORG_HOST))
            {
                // only fail for http://bitbucket.org
                await Assert.ThrowsAsync<Exception>(async () => await provider.GetCredentialAsync(input));
            }
            else
            {
                MockUserEntersValidBasicCredentials(bitbucketAuthentication, input, password);
                MockRemoteBasicAuthAccountIsValidRequires2FA(bitbucketApi, input, password, username);
                MockRemoteValidRefreshToken(input);
                bitbucketAuthentication.Setup(m => m.ShowOAuthRequiredPromptAsync()).ReturnsAsync(true);
                bitbucketAuthentication.Setup(m => m.CreateOAuthCredentialsAsync(input)).ReturnsAsync(new OAuth2TokenResult(MOCK_ACCESS_TOKEN, "access_token"));
                var userInfo = new Mock<IUserInfo>(MockBehavior.Strict); 
                userInfo.Setup(ui => ui.IsTwoFactorAuthenticationEnabled).Returns(false);
                userInfo.Setup(ui => ui.UserName).Returns(username);
                bitbucketApi.Setup(x => x.GetUserInformationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));

                var credential = await provider.GetCredentialAsync(input);
            }
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire")]
        public async Task BitbucketHostProvider_StoreCredentialAsync(string protocol, string host, string username)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(input);

            Assert.Equal(1, context.CredentialStore.Count);
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_EraseCredentialAsync(string protocol, string host, string username, string password)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockStoredAccount(context, input, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(input, bitbucketApi).Object);

            Assert.Equal(1, context.CredentialStore.Count);

            await provider.EraseCredentialAsync(input);

            Assert.Equal(0, context.CredentialStore.Count);
        }

        #endregion

        #region Test helpers
        private static InputArguments MockInput(string protocol, string host, string username)
        {
            return new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });
        }

        private void VerifyBasicAuthFlowRan(string password, bool expected, InputArguments input, Task<ICredential> credential,
            string preconfiguredAuthModes)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Once);

            // check username/password for Bitbucket.org
            if ((preconfiguredAuthModes == null && BITBUCKET_DOT_ORG_HOST == remoteUri.Host)
                || (preconfiguredAuthModes != null && preconfiguredAuthModes.Contains("oauth")))
            {
                bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);
            }
        }

        private void VerifyInteractiveBasicAuthFlowRan(string password, InputArguments input, Task<ICredential> credential)
        {
            var remoteUri = input.GetRemoteUri();

            // verify users was prompted for username/password credentials
            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Once);

            // check username/password for Bitbucket.org
            if (BITBUCKET_DOT_ORG_HOST == remoteUri.Host)
            {
                bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);
            }
        }

        private void VerifyBasicAuthFlowNeverRan(string password, InputArguments input, bool storedAccount,
            string preconfiguredAuthModes)
        {
            var remoteUri = input.GetRemoteUri();

            if (!storedAccount &&
                (preconfiguredAuthModes == null || preconfiguredAuthModes.Contains("basic")) )
            {
                // never prompt the user for basic credentials
                bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Once);
            }
            else
            {
                // never prompt the user for basic credentials
                bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Never);
            }
        }

        private void VerifyInteractiveBasicAuthFlowNeverRan(string password, InputArguments input, Task<ICredential> credential)
        {
            var remoteUri = input.GetRemoteUri();

            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Never);
        }

        private void VerifyOAuthFlowRan(string password, bool storedAccount, bool expected, InputArguments input, Task<ICredential> credential,
            string preconfiguredAuthModes)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            if (storedAccount)
            {
                // use refresh token to get new access token and refresh token
                bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(input, MOCK_REFRESH_TOKEN), Times.Once);

                // check access token works
                bitbucketApi.Verify(m => m.GetUserInformationAsync(null, MOCK_ACCESS_TOKEN, true), Times.Once);
            }
            else
            {
                if (preconfiguredAuthModes == null || preconfiguredAuthModes.Contains("basic"))
                {
                    // prompt user for basic auth, if basic auth is not excluded
                    bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Once);

                    // check if entered Basic Auth credentials work, if basic auth is not excluded
                    bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);
                }

                // Basic Auth 403-ed so push user through OAuth flow
                bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Once);
            }
        }

        private void VerifyInteractiveOAuthFlowRan(string password, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            var remoteUri = input.GetRemoteUri();

            // Basic Auth 403-ed so push user through OAuth flow
            bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Once);

        }

        private void VerifyOAuthFlowDidNotRun(string password, bool expected, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            // never prompt user through OAuth flow
            bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Never);

            // Never try to refresh Access Token
            bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(input, It.IsAny<string>()), Times.Never);

            // never check access token works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, MOCK_ACCESS_TOKEN, true), Times.Never);
        }

        private void VerifyInteractiveOAuthFlowNeverRan(InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            var remoteUri = input.GetRemoteUri();

            // never prompt user through OAuth flow
            bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Never);

            // Never try to refresh Access Token
            bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(input, It.IsAny<string>()), Times.Never);

            // never check access token works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, MOCK_ACCESS_TOKEN, true), Times.Never);
        }

        private void VerifyValidateBasicAuthCredentialsNeverRan()
        {
            // never check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never);
        }

        private void VerifyValidateBasicAuthCredentialsRan()
        {
            // check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(It.IsAny<string>(), It.IsAny<string>(), false), Times.Once);
        }

        private void VerifyValidateOAuthCredentialsNeverRan()
        {
            // never check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, It.IsAny<string>(), false), Times.Never);
        }

        private void VerifyValidateOAuthCredentialsRan()
        {
            // check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, It.IsAny<string>(), true), Times.Once);
        }

        private void MockStoredOAuthAccount(TestCommandContext context, InputArguments input)
        {
            // refresh token
            context.CredentialStore.Add("https://bitbucket.org/refresh_token", new TestCredential(input.Host, input.UserName, MOCK_REFRESH_TOKEN));
            // auth token
            context.CredentialStore.Add("https://bitbucket.org", new TestCredential(input.Host, input.UserName, MOCK_ACCESS_TOKEN));
        }

        private void MockRemoteValidRefreshToken(InputArguments input)
        {
            bitbucketAuthentication.Setup(m => m.RefreshOAuthCredentialsAsync(input, MOCK_REFRESH_TOKEN)).ReturnsAsync(new OAuth2TokenResult(MOCK_ACCESS_TOKEN, "access_token"));
        }

        private static void MockInvalidRemoteBasicAccount(Mock<IBitbucketRestApi> bitbucketApi, Mock<IBitbucketAuthentication> bitbucketAuthentication)
        {
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(It.IsAny<Uri>(), It.IsAny<String>(), It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.Basic, null));

            bitbucketApi.Setup(x => x.GetUserInformationAsync(It.IsAny<String>(), It.IsAny<String>(), false))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.Unauthorized));

        }
        private static void MockUserEntersValidBasicCredentials(Mock<IBitbucketAuthentication> bitbucketAuthentication, InputArguments input, string password)
        {
            var remoteUri = input.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.Basic, new TestCredential(input.Host, input.UserName, password)));
        }

        private static void MockUserDoesNotEntersValidBasicCredentials(Mock<IBitbucketAuthentication> bitbucketAuthentication)
        {
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(It.IsAny<Uri>(), It.IsAny<String>(), It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.Basic, null));
        }

        private static void MockRemoteBasicAuthAccountIsValid(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password, string username, bool twoFAEnabled)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict); 
            userInfo.Setup(ui => ui.IsTwoFactorAuthenticationEnabled).Returns(twoFAEnabled);
            userInfo.Setup(ui => ui.UserName).Returns(username);

            // Basic
            bitbucketApi.Setup(x => x.GetUserInformationAsync(input.UserName, password, false))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));

        }

        private static void MockRemoteBasicAuthAccountIsValidRequires2FA(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password, string username)
        {
            MockRemoteBasicAuthAccountIsValid(bitbucketApi, input, password, username, true);
        }

        private static void MockRemoteBasicAuthAccountIsValidNo2FA(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password, string username)
        {
            MockRemoteBasicAuthAccountIsValid(bitbucketApi, input, password, username, false);
        }

        private static void MockRemoteBasicAuthAccountIsInvalid(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict);

            // Basic
            bitbucketApi.Setup(x => x.GetUserInformationAsync(input.UserName, password, false))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.Forbidden, userInfo.Object));

        }

        private static void MockRemoteOAuthAccountIsValid(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password, string username, bool twoFAEnabled)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict); 
            userInfo.Setup(ui => ui.IsTwoFactorAuthenticationEnabled).Returns(twoFAEnabled);
            userInfo.Setup(ui => ui.UserName).Returns(username);
            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, password, true))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));
        }

        private static void MockRemoteOAuthAccountIsInvalid(Mock<IBitbucketRestApi> bitbucketApi)
        {
            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, It.IsAny<string>(), true)).ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.BadRequest));
        }

        private static void MockStoredAccount(TestCommandContext context, InputArguments input, string password)
        {
            var remoteUri = input.GetRemoteUri();
            var remoteUrl = remoteUri.AbsoluteUri.Substring(0, remoteUri.AbsoluteUri.Length - 1);
            context.CredentialStore.Add(remoteUrl, new TestCredential(input.Host, input.UserName, password));
        }

        private static void MockValidStoredOAuthUser(TestCommandContext context, Mock<IBitbucketRestApi> bitbucketApi)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict); 
            userInfo.Setup(ui => ui.IsTwoFactorAuthenticationEnabled).Returns(false);
            bitbucketApi.Setup(x => x.GetUserInformationAsync("jsquire", "password1", false))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));
            context.CredentialStore.Add("https://bitbucket.org", new TestCredential("https://bitbucket.org", "jsquire", "password1"));
        }

        private static Mock<IRegistry<IBitbucketRestApi>> MockRestApiRegistry(InputArguments input, Mock<IBitbucketRestApi> bitbucketApi)
        {
            var restApiRegistry = new Mock<IRegistry<IBitbucketRestApi>>(MockBehavior.Strict);
            
            restApiRegistry.Setup(rar => rar.Get(input)).Returns(bitbucketApi.Object);
            
            return restApiRegistry;
        }

        #endregion
    }
}
