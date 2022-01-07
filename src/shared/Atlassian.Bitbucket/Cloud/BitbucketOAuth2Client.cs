// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.Cloud
{
    public class BitbucketOAuth2Client : AbstractBitbucketOAuth2Client
    {
        public BitbucketOAuth2Client(HttpClient httpClient, ISettings settings, ITrace trace)
            : base(httpClient, GetEndpoints(),
                GetClientId(settings), GetRedirectUri(settings), GetClientSecret(settings), trace)
        {
        }

        private static OAuth2ServerEndpoints GetEndpoints()
        {
            return new OAuth2ServerEndpoints(
                CloudConstants.OAuth2AuthorizationEndpoint,
                CloudConstants.OAuth2TokenEndpoint
            );
        }

        private static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                CloudConstants.EnvironmentVariables.OAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return CloudConstants.OAuth2ClientId;
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

            return CloudConstants.OAuth2RedirectUri;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                CloudConstants.EnvironmentVariables.OAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthClientSecret,
                out string clientId))
            {
                return clientId;
            }

            return CloudConstants.OAuth2ClientSecret;
        }

        public override IEnumerable<string> Scopes => new string[] {
            CloudConstants.OAuthScopes.RepositoryWrite,
            CloudConstants.OAuthScopes.Account,
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

        private class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonProperty("scopes")]
            public override string Scope { get; set; }
        }

        public override string GetRefreshTokenServiceName(InputArguments input)
        {
            Uri baseUri = input.GetRemoteUri(includeUser: false);

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }
    }
}
