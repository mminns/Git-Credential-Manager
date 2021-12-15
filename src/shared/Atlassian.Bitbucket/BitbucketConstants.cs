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
            public const string DevOAuthClientId = "GCM_DEV_BITBUCKET_CLIENTID";
            public const string DevOAuthClientSecret = "GCM_DEV_BITBUCKET_CLIENTSECRET";
            public const string DevOAuthRedirectUri = "GCM_DEV_BITBUCKET_REDIRECTURI";
            public const string AuthenticationModes = "GCM_BITBUCKET_AUTHMODES";
            public const string AlwaysRefreshCredentials = "GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationHelper = "bitbucketHelper";
                public const string DevOAuthClientId = "bitbucketDevClientId";
                public const string DevOAuthClientSecret = "bitbucketDevClientSecret";
                public const string DevOAuthRedirectUri = "bitbucketDevRedirectUri";
                public const string AuthenticationModes = "bitbucketAuthModes";
                public const string AlwaysRefreshCredentials = "bitbucketAlwaysRefreshCredentials";
            }
        }
    }
}
