// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.DataCenter
{
    public class BitbucketOAuth2Client : AbstractBitbucketOAuth2Client
    {
        public BitbucketOAuth2Client(HttpClient httpClient, ISettings settings, ITrace trace)
            : base(httpClient, GetEndpoints(settings),
                GetClientId(settings), GetRedirectUri(settings), GetClientSecret(settings), trace)
        {
        }

        public override IEnumerable<string> Scopes => new string[] {
            DataCenterConstants.OAuthScopes.PublicRepos,
            DataCenterConstants.OAuthScopes.RepoRead,
            DataCenterConstants.OAuthScopes.RepoWrite
        };

        public override string GetRefreshTokenServiceName(InputArguments input)
        {
            Uri baseUri = input.GetRemoteUri(includeUser: false);

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }

        public override async Task<OAuth2TokenResult> GetTokenByAuthorizationCodeAsync(OAuth2AuthorizationCodeResult authorizationCodeResult, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.TokenEndpoint.GrantTypeParameter] = OAuth2Constants.TokenEndpoint.AuthorizationCodeGrantType,
                [OAuth2Constants.TokenEndpoint.AuthorizationCodeParameter] = authorizationCodeResult.Code,
                [OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier,
                [OAuth2Constants.ClientIdParameter] = ClientId,
                ["client_secret"] = ClientSecret
            };

            if (authorizationCodeResult.RedirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = authorizationCodeResult.RedirectUri.ToString();
            }

            if (authorizationCodeResult.CodeVerifier != null)
            {
                formData[OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier;
            }

            Trace.WriteLine($"FormData: ");
            Trace.WriteDictionary(formData);

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, Endpoints.TokenEndpoint, requestContent, false))
            using (HttpResponseMessage response = await HttpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                {
                    return result;
                }

                throw CreateExceptionFromResponse(json);
            }
        }

        public override async Task<OAuth2TokenResult> GetTokenByRefreshTokenAsync(string refreshToken, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.TokenEndpoint.GrantTypeParameter] = OAuth2Constants.TokenEndpoint.RefreshTokenGrantType,
                [OAuth2Constants.TokenEndpoint.RefreshTokenParameter] = refreshToken,
                [OAuth2Constants.ClientIdParameter] = ClientId,
                ["client_secret"] = ClientSecret
            };

            if (RedirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = RedirectUri.ToString();
            }

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, Endpoints.TokenEndpoint, requestContent, false))
            using (HttpResponseMessage response = await HttpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                {
                    return result;
                }

                throw CreateExceptionFromResponse(json);
            }
        }

        private static OAuth2ServerEndpoints GetEndpoints(ISettings settings)
        {
            return new OAuth2ServerEndpoints(
                new Uri(BitbucketHelper.GetBaseUri(settings) + "/rest/oauth2/latest/authorize"),
                new Uri(BitbucketHelper.GetBaseUri(settings) + "/rest/oauth2/latest/token")
                );
        }

        private static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.OAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.OAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            throw new ArgumentException("Bitbucket DC OAuth Client ID must be defined");
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.OAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.OAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return DataCenterConstants.OAuth2RedirectUri;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.OAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.OAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            throw new ArgumentException("Bitbucket DC OAuth Client Secret must be defined");
        }

        protected override bool TryCreateTokenEndpointResult(string json, out OAuth2TokenResult result)
        {
            // We override the token endpoint response parsing because the Bitbucket authority returns
            // the non-standard 'scopes' property for the list of scopes, rather than the (optional)
            // 'scope' (note the singular vs plural) property as outlined in the standard.
            if (TryDeserializeJson(json, out BitbucketTokenEndpointResponseJson jsonObj))
            {
                result = jsonObj.ToResult();
                return true;
            }

            result = null;
            return false;
        }

        private class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonProperty("scopes")]
            public override string Scope { get; set; }
        }
    }
}
