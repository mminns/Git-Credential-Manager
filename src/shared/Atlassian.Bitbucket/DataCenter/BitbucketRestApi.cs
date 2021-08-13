using System;
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

    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = BitbucketConstants.BitbucketApiUri;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _apiUri = _context.Settings.RemoteUri;
        }

        public async Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            // TODO SHPLII-74 HACKY
            // No REST API in BBS that can be used to return just my user account based on my login AFAIK.
            return await Task.Run(() => new RestApiResult<IUserInfo>(HttpStatusCode.OK, new UserInfo() { UserName = "   " }));

            /*
            var requestUri = new Uri(_apiUri.AbsoluteUri + "rest/api/1.0/users?filter=" + userName);
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
            */
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
