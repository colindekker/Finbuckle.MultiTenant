namespace Finbuckle.MultiTenant.Stores
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A multi-tenant store that uses EFCore.
    /// </summary>
    public class RemoteStore<TRemoteStoreHttpClient, TTenantInfo>
        : IMultiTenantStore
        where TRemoteStoreHttpClient : RemoteStoreHttpClient<TTenantInfo>
        where TTenantInfo : class, IRemoteStoreTenantInfo, new()
    {
        private readonly TRemoteStoreHttpClient _remoteStoreClient;

        public RemoteStore(TRemoteStoreHttpClient remoteStoreClient)
        {
            _remoteStoreClient = remoteStoreClient ?? throw new ArgumentNullException(nameof(remoteStoreClient));
        }

        public async Task<TenantInfo> TryGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return (await _remoteStoreClient.TryGetAsync(id).ConfigureAwait(false)).ToTenantInfo();
        }

        public async Task<TenantInfo> TryGetByIdentifierAsync(string identifier)
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                if (identifier.ToLowerInvariant() != "favicon.ico")
                {
                    var t = await _remoteStoreClient.TryGetByIdentifierAsync(identifier).ConfigureAwait(false);
                    return t.ToTenantInfo();
                }
            }

            return null;
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
    }
}