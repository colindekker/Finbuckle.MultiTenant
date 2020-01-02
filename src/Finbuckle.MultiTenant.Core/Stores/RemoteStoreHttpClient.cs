namespace Finbuckle.MultiTenant.Stores
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using IdentityModel.Client;
    using Newtonsoft.Json;
    using Polly;
    using Polly.Registry;
    using JetBrains.Annotations;

    public class RemoteStoreHttpClient<TTenantInfo>
        where TTenantInfo : class, IRemoteStoreTenantInfo
    {
        private readonly HttpClient _client;

        private readonly IAsyncPolicy<TTenantInfo> _cachePolicy;

        public RemoteStoreHttpClient(
            [NotNull] HttpClient client,
            IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            _client = client;
            _cachePolicy = policyRegistry.Get<IAsyncPolicy<TTenantInfo>>(typeof(TTenantInfo).Name);
        }

        public virtual string Authority { get; set; }

        public virtual string ClientId { get; set; }

        public virtual string ClientSecret { get; set; }

        public virtual string Scope { get; set; }

        public virtual string ContextEndpointUrl { get; set; }

        public virtual bool Authenticate { get; set; }

        private bool CanAuthenticate => !string.IsNullOrWhiteSpace(Authority)
                                     && !string.IsNullOrWhiteSpace(ClientId)
                                     && !string.IsNullOrWhiteSpace(ClientSecret)
                                     && !string.IsNullOrWhiteSpace(ContextEndpointUrl);

        public virtual async Task<TTenantInfo> TryGetAsync(string id)
        {
            if (Authenticate)
            {
                if (!CanAuthenticate)
                {
                    throw new Exception("The Remote Store is set to use Authentication but is missing required Configuration values.");
                }

                _client.SetBearerToken(await GetAccessToken().ConfigureAwait(false));
            }

            var url = $"{ContextEndpointUrl.TrimEnd('/')}/id/{id}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var res = await _client.SendAsync(req).ConfigureAwait(false);
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TTenantInfo>(json);
        }

        public virtual async Task<TTenantInfo> TryGetByIdentifierAsync(string identifier)
        {
            Context policyExecutionContext = new Context("GetByIdentifier-");
            policyExecutionContext["identifier"] = identifier;
            var tenant = await _cachePolicy.ExecuteAsync(_ => Get(identifier), policyExecutionContext).ConfigureAwait(false);//Get(identifier); // 
            return tenant;
        }

        private async Task<TTenantInfo> Get(string identifier)
        {
            if (Authenticate)
            {
                if (!CanAuthenticate)
                {
                    throw new Exception("The Remote Store is set to use Authentication but is missing required Configuration values.");
                }

                _client.SetBearerToken(await GetAccessToken().ConfigureAwait(false));
            }

            var url = $"{ContextEndpointUrl.TrimEnd('/')}/identifier/{identifier}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var res = await _client.SendAsync(req).ConfigureAwait(false);
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TTenantInfo>(json);
        }

        public Task<bool> TryAddAsync(TenantInfo tenantInfo)
        {
            throw new NotImplementedException("A remote store should not be able to create tenants.");
        }

        public Task<bool> TryRemoveAsync(string identifier)
        {
            throw new NotImplementedException("A remote store should not be able to delete tenants.");
        }

        public Task<bool> TryUpdateAsync(TenantInfo tenantInfo)
        {
            throw new NotImplementedException("A remote store should not be able to edit tenants.");
        }

        private async Task<string> GetAccessToken()
        {
            var disco = await _client.GetDiscoveryDocumentAsync(Authority).ConfigureAwait(false);
            if (disco.IsError) throw new Exception(disco.Error);
            var tokenEndpoint = disco.TokenEndpoint;

            var client = new HttpClient();

            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scope = Scope
            }).ConfigureAwait(false);

            return response.AccessToken;
        }
    }
}
