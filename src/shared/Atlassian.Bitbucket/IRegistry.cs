using System;
using Microsoft.Git.CredentialManager;

namespace Atlassian.Bitbucket
{
    public interface IRegistry<T> : IDisposable
    {
        T Get(InputArguments input);
    }
}