using System;

namespace Atlassian.Bitbucket.Client
{
    public interface IUserInfo
    {
        bool IsTwoFactorAuthenticationEnabled { get; set; }
        string UserName { get; set; }
    }
}
