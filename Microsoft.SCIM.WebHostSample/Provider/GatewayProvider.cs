using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.SCIM.WebHostSample.Provider
{
    public class GatewayProvider : ProviderBase
    {
        List<GatewayProviderConfig> config = new List<GatewayProviderConfig>()
        {
            new GatewayProviderConfig()
            {
                Name = "memory",
                Type = "InMemoryProvider"
            },
            new GatewayProviderConfig()
            {
                Name = "hrscimtest",
                Type = "CosmosProvider"
            },
            new GatewayProviderConfig()
            {
                Name = "omg",
                Type = "AzureFunctionProvider"
            },

        };

        public override Task<Resource> CreateAsync(Resource resource, string tenant, string correlationIdentifier)
        {
            GatewayProviderConfig providerConfig = config.FirstOrDefault(c => c.Name.Equals(tenant, StringComparison.OrdinalIgnoreCase));

            if (providerConfig != null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetTypes().First(t => t.Name == providerConfig.Type);

                var provider = (ProviderBase)Activator.CreateInstance(type);

                return provider.CreateAsync(resource, tenant, correlationIdentifier);
            }

            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            throw new NotImplementedException();
        }

        public override Task<(Resource[], int)> QueryAsync(IQueryParameters parameters, string tenant, string correlationIdentifier)
        {
            GatewayProviderConfig providerConfig = config.FirstOrDefault(c => c.Name.Equals(tenant, StringComparison.OrdinalIgnoreCase));

            if (providerConfig != null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetTypes().First(t => t.Name == providerConfig.Type);

                var provider = (ProviderBase)Activator.CreateInstance(type);

                return provider.QueryAsync(parameters, tenant, correlationIdentifier);
            }

            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task<Resource> ReplaceAsync(Resource resource, string correlationIdentifier)
        {
            throw new NotImplementedException();
        }

        public override Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string tenant, string correlationIdentifier)
        {
            GatewayProviderConfig providerConfig = config.FirstOrDefault(c => c.Name.Equals(tenant, StringComparison.OrdinalIgnoreCase));

            if (providerConfig != null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetTypes().First(t => t.Name == providerConfig.Type);
                                
                var provider = (ProviderBase)Activator.CreateInstance(type);

                return provider.RetrieveAsync(parameters, tenant, correlationIdentifier);
            }

            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            throw new NotImplementedException();
        }

        public override string GetTenant(HttpRequestMessage request)
        {
            return request.RequestUri.Segments[1].Equals("scim", StringComparison.OrdinalIgnoreCase) ?
                string.Empty :
                request.RequestUri.Segments[1].TrimEnd('/');
        }
    }


    class GatewayProviderConfig
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
