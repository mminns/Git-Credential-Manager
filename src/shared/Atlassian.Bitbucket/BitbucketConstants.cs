// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Atlassian.Bitbucket
{
    public static class BitbucketConstants
    {
        public const string BitbucketBaseUrlHost = "bitbucket.org";
        public static readonly Uri BitbucketApiUri = new Uri("https://api.bitbucket.org");
        public const string DefaultAuthenticationHelper = "Atlassian.Bitbucket.UI";

        // TODO: use the GCM client ID and secret once we have this approved.
        // Until then continue to use Sourcetree's values like GCM Windows.
        //public const string OAuth2ClientId = "b5AKdPfpgFdEGpKzPE";
        //public const string OAuth2ClientSecret = "7NUP5qUtSR3SxdFK4xAGaU6PMNvNdE59";
        //public static readonly Uri OAuth2RedirectUri = new Uri("http://localhost:46337/");
        public const string OAuth2ClientId = "HJdmKXV87DsmC9zSWB";
        public const string OAuth2ClientSecret = "wwWw47VB9ZHwMsD4Q4rAveHkbxNrMp3n";
        public static readonly Uri OAuth2RedirectUri = new Uri("http://localhost:34106/");

        public const string XOAuth2ClientId = "92ae854ce3cdffc55ce750844f971b80";
        public const string XOAuth2ClientSecret = "aa52721197c29ec39f65b8e45312c6493991844c314f48c7cee32bcee50d1ccd";
        public static readonly Uri XOAuth2RedirectUri = new Uri("http://localhost:34106/");


        public static readonly Uri OAuth2AuthorizationEndpoint = new Uri("https://bitbucket.org/site/oauth2/authorize");
        public static readonly Uri OAuth2TokenEndpoint = new Uri("https://bitbucket.org/site/oauth2/access_token");

        public static class OAuthScopes
        {
            public const string RepositoryWrite = "repository:write";
            public const string Account = "account";
        }

        public static class XOAuthScopes
        {
            public const string PublicRepos = "PUBLIC_REPOS";
            public const string RepoWrite = "REPO_WRITE";
            public const string RepoRead = "REPO_READ";
        }

        public static class EnvironmentVariables
        {
            public const string AuthenticationHelper = "GCM_BITBUCKET_HELPER";
            public const string DevOAuthClientId = "GCM_DEV_BITBUCKET_CLIENTID";
            public const string DevOAuthClientSecret = "GCM_DEV_BITBUCKET_CLIENTSECRET";
            public const string DevOAuthRedirectUri = "GCM_DEV_BITBUCKET_REDIRECTURI";
            public const string AuthenticationModes = "GCM_BITBUCKET_AUTHMODES";
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
            }
        }

        /// <summary>
        /// Supported authentication modes for Bitbucket.org/Bitbucket Server/DC
        /// </summary>
        public const AuthenticationModes SupportedAuthenticationModes = AuthenticationModes.Basic | AuthenticationModes.OAuth;
    }
}
