using System;
using Microsoft.Git.CredentialManager;

namespace Atlassian.Bitbucket
{
    public static class BitbucketHelper
    {
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
