using System;
using Microsoft.Git.CredentialManager;

namespace Atlassian.Bitbucket
{
    public static class BitbucketHelper
    {
        public static string GetBaseUri(ISettings settings)
        {
            // TODO SHPLII-74 HACKY
            var pathParts = settings.RemoteUri.PathAndQuery.Split('/');
            var pathPart = settings.RemoteUri.PathAndQuery.StartsWith("/") ? pathParts[1] : pathParts[0];
            var path = !string.IsNullOrWhiteSpace(pathPart) ? "/" + pathPart : null;
            return $"{settings.RemoteUri.Scheme}://{settings.RemoteUri.Host}:{settings.RemoteUri.Port}{path}";
        }

        public static bool IsBitbucketOrg(InputArguments input)
        {
            return IsBitbucketOrg(input.GetRemoteUri());
        }

        public static bool IsBitbucketOrg(string targetUrl)
        {
            return Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri uri) && IsBitbucketOrg(uri);
        }

        public static bool IsBitbucketOrg(Uri targetUri)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(targetUri.Host, BitbucketConstants.BitbucketBaseUrlHost);
        }
    }
}
