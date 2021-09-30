using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.DataCenter
{
    public class UserInfo : IUserInfo
    {
        [JsonProperty("has_2fa_enabled")]
        public bool IsTwoFactorAuthenticationEnabled { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }

    public class LoginOptions
    {
        [JsonProperty("results")]
        public List<LoginOption> Results { get; set; }
    }

    public class LoginOption
    {
        [JsonProperty("type")]
        public string Type { get ; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = BitbucketConstants.BitbucketApiUri;

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
            return await Task.Run(() => new RestApiResult<IUserInfo>(HttpStatusCode.OK, new UserInfo() { UserName = "   " }));
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