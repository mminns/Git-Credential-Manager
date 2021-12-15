using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.Cloud
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

    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = CloudConstants.BitbucketApiUri;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            var requestUri = new Uri(_apiUri, "2.0/user");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                if (isBearerToken)
                {
                    request.AddBearerAuthenticationHeader(password);
                }
                else
                {
                    request.AddBasicAuthenticationHeader(userName, password);
                }

                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                    string json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var obj = JsonConvert.DeserializeObject<UserInfo>(json, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        return new RestApiResult<IUserInfo>(response.StatusCode, obj);
                    }

                    return new RestApiResult<IUserInfo>(response.StatusCode);
                }
            }
        }

        public async Task<bool> IsOAuthInstalledAsync()
        {
            return await Task.FromResult(true);
        }

        public async Task<List<AuthenticationMethod>> GetAuthenticationMethodsAsync()
        {
            // HACK NEVER USED
            return await Task.FromResult(new List<AuthenticationMethod>());
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
