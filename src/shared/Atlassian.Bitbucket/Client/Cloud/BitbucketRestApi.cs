using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.Client.Cloud
{

    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;
        private readonly Uri _apiUri = BitbucketConstants.BitbucketApiUri;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<RestApiResult<UserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
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

                        return new RestApiResult<UserInfo>(response.StatusCode, obj);
                    }

                    return new RestApiResult<UserInfo>(response.StatusCode);
                }
            }
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        Task<RestApiResult<UserInfo>> IBitbucketRestApi.GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            throw new NotImplementedException();
        }
    }
}
