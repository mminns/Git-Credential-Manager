using System;

namespace Atlassian.Bitbucket.DataCenter
{
    public static class DataCenterConstants
    {
        public static class OAuthScopes
        {
            public const string PublicRepos = "PUBLIC_REPOS";
            public const string RepoWrite = "REPO_WRITE";
            public const string RepoRead = "REPO_READ";
        }

        public static readonly Uri OAuth2RedirectUri = new Uri("http://localhost:34106/");

        /// <summary>
        /// Supported authentication modes for Bitbucket Server/DC
        /// </summary>
        public const AuthenticationModes ServerAuthenticationModes = AuthenticationModes.Basic  | AuthenticationModes.OAuth;

        /// <summary>
        /// Bitbucket Server/DC does not have a REST API we can use to trade an OAuth access_token for the owning username.
        /// However one is needed to construct the Bsic Auth request made by Git HTTP requests, therefore use a hardcoded
        /// placeholder for the usernme.
        /// </summary>
        public const string OauthUserName = "OAUTH_USERNAME";

        public static class EnvironmentVariables
        {
            public const string OAuthClientId = "GCM_BITBUCKET_DATACENTER_CLIENTID";
            public const string OAuthClientSecret = "GCM_BITBUCKET_DATACENTER_CLIENTSECRET";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string OAuthClientId = "bitbucketDataCenterOauthClientId";
                public const string OAuthClientSecret = "bitbucketDataCenterOauthClientSecret";
            }
        }
    }
}
