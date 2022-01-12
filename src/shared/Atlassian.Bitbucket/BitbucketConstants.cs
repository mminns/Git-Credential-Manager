using System;

namespace Atlassian.Bitbucket
{
    public static class BitbucketConstants
    {
        public const string Id = "bitbucket";

        public const string Name = "Bitbucket";

        public const string DefaultAuthenticationHelper = "Atlassian.Bitbucket.UI";

        public static class EnvironmentVariables
        {
            public const string AuthenticationHelper = "GCM_BITBUCKET_HELPER";
            public const string OAuthRedirectUri = "GCM_BITBUCKET_OAUTH_REDIRECTURI";
            public const string AuthenticationModes = "GCM_BITBUCKET_AUTHMODES";
            public const string AlwaysRefreshCredentials = "GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS";
            public const String ValidateStoredCredentials = "GCM_GITBUCKET_VALIDATE_STORED_CREDENTIALS";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationHelper = "bitbucketHelper";
                public const string OAuthRedirectUri = "bitbucketOauthRedirectUri";
                public const string AuthenticationModes = "bitbucketAuthModes";
                public const string AlwaysRefreshCredentials = "bitbucketAlwaysRefreshCredentials";
                public const string ValidateStoredCredentials = "bitbucketValidateStoredCredentials";
            }
        }
    }
}
