using System;
using System.Collections.Generic;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{
    public abstract class AbstractBitbucketOAuth2Client : OAuth2Client
    {
        public AbstractBitbucketOAuth2Client(HttpClient httpClient, OAuth2ServerEndpoints endpoints, string clientId, Uri redirectUri, string clientSecret, ITrace trace) : base(httpClient, endpoints, clientId, redirectUri, clientSecret, trace)
        {
        }

        public abstract string GetRefreshTokenServiceName(InputArguments input);

        public abstract IEnumerable<string> Scopes { get; }
    }
}
