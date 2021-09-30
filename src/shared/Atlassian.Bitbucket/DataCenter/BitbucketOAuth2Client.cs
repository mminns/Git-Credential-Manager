// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.Authentication.OAuth.Json;
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

        private static OAuth2ServerEndpoints GetEndpoints(ISettings settings)
        {
            return new OAuth2ServerEndpoints(
                new Uri(GetBaseUri(settings) + "/rest/oauth2/latest/authorize"),
                new Uri(GetBaseUri(settings) + "/rest/oauth2/latest/token")
                );
        }

        private static string GetBaseUri(ISettings settings)
        {
            // TODO SHPLII-74 HACKY
            var pathParts = settings.RemoteUri.PathAndQuery.Split('/');
            var pathPart = settings.RemoteUri.PathAndQuery.StartsWith("/") ? pathParts[1] : pathParts[0];
            var path = !string.IsNullOrWhiteSpace(pathPart) ? "/" + pathPart : null;
            return $"{settings.RemoteUri.Scheme}://{settings.RemoteUri.Host}:{settings.RemoteUri.Port}{path}";
        }

        private static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return BitbucketConstants.XOAuth2ClientId;
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return BitbucketConstants.OAuth2RedirectUri;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientId))
            {
                return clientId;
            }

            return BitbucketConstants.XOAuth2ClientSecret;
        }

        public override IEnumerable<string> Scopes => new string[] {
            BitbucketConstants.XOAuthScopes.PublicRepos,
            BitbucketConstants.XOAuthScopes.RepoRead,
            BitbucketConstants.XOAuthScopes.RepoWrite
        };

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

        public override string GetRefreshTokenServiceName(InputArguments input)
        {
            Uri baseUri = input.GetRemoteUri(includeUser: false);

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }

        private class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonProperty("scopes")]
            public override string Scope { get; set; }
        }

        public override async Task<OAuth2TokenResult> GetTokenByAuthorizationCodeAsync(OAuth2AuthorizationCodeResult authorizationCodeResult, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.TokenEndpoint.GrantTypeParameter] = OAuth2Constants.TokenEndpoint.AuthorizationCodeGrantType,
                [OAuth2Constants.TokenEndpoint.AuthorizationCodeParameter] = authorizationCodeResult.Code,
                [OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier,
                [OAuth2Constants.ClientIdParameter] = _clientId,
                ["client_secret"] = _clientSecret
            };

            if (authorizationCodeResult.RedirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = authorizationCodeResult.RedirectUri.ToString();
            }

            if (authorizationCodeResult.CodeVerifier != null)
            {
                formData[OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier;
            }

            _trace.WriteLine($"FormData: ");
            _trace.WriteDictionary(formData);

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint, requestContent, false))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
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
                [OAuth2Constants.ClientIdParameter] = _clientId,
                ["client_secret"] = _clientSecret
            };

            if (_redirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = _redirectUri.ToString();
            }

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint, requestContent, false))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                {
                    return result;
                }

                throw CreateExceptionFromResponse(json);
            }
        }
    }
}
