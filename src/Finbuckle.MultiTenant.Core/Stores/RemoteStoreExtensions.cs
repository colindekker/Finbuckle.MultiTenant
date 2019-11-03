namespace Finbuckle.MultiTenant.Stores
{
    /// <summary>
    /// Extension methods for use in combination with a <seealso cref="RemoteStore{TRemoteStoreHttpClient,TTenantInfo}"/>.
    /// </summary>
    public static class RemoteStoreExtensions
    {
        public static TenantInfo ToTenantInfo(this IRemoteStoreTenantInfo tenantInfo)
        {
            if (tenantInfo == null) return null;
            return new TenantInfo(tenantInfo.Id, tenantInfo.Identifier, tenantInfo.Name, tenantInfo.ConnectionString, tenantInfo.Items);
        }
    }
}