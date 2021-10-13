using System;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Client.Cloud;

namespace Atlassian.Bitbucket.Client
{
    public interface IBitbucketRestApi : IDisposable
    {
        Task<RestApiResult<UserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken);
    }
}