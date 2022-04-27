using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Provider
{
    public class CosmosProvider : ProviderBase
    {
        private readonly ProviderBase userProvider;
        private readonly ProviderBase groupProvider;

        public CosmosProvider()
        {
            this.groupProvider = new CosmosGroupProvider();
            this.userProvider = new CosmosUserProvider();
        }

        public override async Task<Resource> CreateAsync(Resource resource, string tenant, string correlationIdentifier)
        {
            if (resource is Core2EnterpriseUser)
            {
                return await this.userProvider.CreateAsync(resource, tenant, correlationIdentifier);
            }

            if (resource is Core2Group)
            {
                return await this.groupProvider.CreateAsync(resource, tenant, correlationIdentifier);
            }

            throw new NotImplementedException();
        }

        public override Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (resourceIdentifier.SchemaIdentifier.Equals(SchemaIdentifiers.Core2EnterpriseUser))
            {
                return this.userProvider.DeleteAsync(resourceIdentifier, correlationIdentifier);
            }

            //if (resourceIdentifier.SchemaIdentifier.Equals(SchemaIdentifiers.Core2Group))
            //{
            //    return this.groupProvider.DeleteAsync(resourceIdentifier, correlationIdentifier);
            //}

            throw new NotImplementedException();
        }

        public override Task<(Resource[], int)> QueryAsync(IQueryParameters parameters,  string tenant, string correlationIdentifier)
        {
            if (parameters.SchemaIdentifier.Equals(SchemaIdentifiers.Core2EnterpriseUser))
            {
                return this.userProvider.QueryAsync(parameters, tenant, correlationIdentifier);
            }

            if (parameters.SchemaIdentifier.Equals(SchemaIdentifiers.Core2Group))
            {
                return this.groupProvider.QueryAsync(parameters, tenant, correlationIdentifier);
            }

            throw new NotImplementedException();
        }

        public override Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string tenant, string correlationIdentifier)
        {
            if (parameters.SchemaIdentifier.Equals(SchemaIdentifiers.Core2EnterpriseUser))
            {
                return this.userProvider.RetrieveAsync(parameters, tenant, correlationIdentifier);
            }

            //if (parameters.SchemaIdentifier.Equals(SchemaIdentifiers.Core2Group))
            //{
            //    return this.groupProvider.RetrieveAsync(parameters, correlationIdentifier);
            //}

            throw new NotImplementedException();
        }

        public override Task<Resource> ReplaceAsync(Resource resource, string correlationIdentifier)
        {
            if (resource is Core2EnterpriseUser)
            {
                return this.userProvider.ReplaceAsync(resource, correlationIdentifier);
            }

            //if (resource is Core2Group)
            //{
            //    return this.groupProvider.ReplaceAsync(resource, correlationIdentifier);
            //}

            throw new NotImplementedException();
        }

        public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            if (patch == null)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(nameof(patch));
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(nameof(patch));
            }

            if (patch.ResourceIdentifier.SchemaIdentifier.Equals(SchemaIdentifiers.Core2EnterpriseUser))
            {
                return this.userProvider.UpdateAsync(patch, correlationIdentifier);
            }

            //if (patch.ResourceIdentifier.SchemaIdentifier.Equals(SchemaIdentifiers.Core2Group))
            //{
            //    return this.groupProvider.UpdateAsync(patch, correlationIdentifier);
            //}

            throw new NotImplementedException();
        }

        public override string GetTenant(HttpRequestMessage request)
        {
            return string.Empty;
        }
    }
}

