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
    }
}
