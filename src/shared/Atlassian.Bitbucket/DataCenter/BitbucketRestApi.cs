using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.DataCenter
{
    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = null;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _apiUri = new Uri(BitbucketHelper.GetBaseUri(_context.Settings) + "/rest/");
        }

        public async Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            // TODO SHPLII-74 HACKY
            // No REST API in BBS that can be used to return just my user account based on my login AFAIK.
            return await Task.Run(() => new RestApiResult<IUserInfo>(HttpStatusCode.OK, new UserInfo() { UserName = String.Empty }));
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<bool> IsOAuthInstalledAsync()
        {
            var requestUri = new Uri(_apiUri.AbsoluteUri + "oauth2/1.0/client");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int)response.StatusCode} [{response.StatusCode}]");

                    if (HttpStatusCode.Unauthorized == response.StatusCode)
                    {
                        // accessed anonymously so no access but it does exist.
                        return true;
                    }

                    return false;
                }
            }
        }

        public async Task<List<AuthenticationMethod>> GetAuthenticationMethodsAsync()
        {
            var authenticationMethods = new List<AuthenticationMethod>();

            var requestUri = new Uri(_apiUri.AbsoluteUri + "authconfig/1.0/login-options");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int)response.StatusCode} [{response.StatusCode}]");

                    string json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var loginOptions = JsonConvert.DeserializeObject<LoginOptions>(json, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if(loginOptions.Results.Any(r => "LOGIN_FORM".Equals(r.Type)))
                        {
                            authenticationMethods.Add(AuthenticationMethod.BASIC_AUTH);
                        }

                        if (loginOptions.Results.Any(r => "IDP".Equals(r.Type)))
                        {
                            authenticationMethods.Add(AuthenticationMethod.SSO);
                        }
                    }
                }
            }

            return authenticationMethods;
        }
    }
}