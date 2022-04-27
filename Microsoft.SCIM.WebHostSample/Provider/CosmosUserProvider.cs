using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.SCIM.WebHostSample.Provider
{
    public class CosmosUserProvider : CosmosProvider
    {
        private readonly CosmosStorage storage;

        public CosmosUserProvider()
        {
            this.storage = new CosmosStorage();
        }

        public override async Task<Resource> CreateAsync(Resource resource, string tenant, string correlationIdentifier)
        {
            if (resource.Identifier != null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            Core2EnterpriseUser user = resource as Core2EnterpriseUser;

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (await this.storage.UserNameExists(user.UserName))
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            // Update metadata
            DateTime created = DateTime.UtcNow;
            user.Metadata.Created = created;
            user.Metadata.LastModified = created;

            string resourceIdentifier = Guid.NewGuid().ToString();
            resource.Identifier = resourceIdentifier;
            await this.storage.Create(user);

            return resource;
        }

        public override async Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(resourceIdentifier?.Identifier))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            string identifier = resourceIdentifier.Identifier;

            if (await this.storage.Exists(identifier))
            {
                await this.storage.Delete(identifier);
            }
            else
                throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override async Task<(Resource[], int)> QueryAsync(IQueryParameters parameters, string tenant, string correlationIdentifier)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
            }

            string query = "SELECT * FROM c WHERE c.meta.resourceType = 'User'";
            string queryFilter = string.Empty;


            if (parameters.AlternateFilters.Count > 0)
            {
                foreach (IFilter orScimFilter in parameters.AlternateFilters)
                {
                    IFilter andScimFilter = orScimFilter;
                    IFilter currentScimFilter = andScimFilter;

                    string andFilter = string.Empty;
                    do
                    {
                        if (string.IsNullOrWhiteSpace(andScimFilter.AttributePath))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        else if (string.IsNullOrWhiteSpace(andScimFilter.ComparisonValue))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        // Id filter
                        else if (andScimFilter.AttributePath.Equals(AttributeNames.Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                            }

                            andFilter += andFilter == string.Empty ?
                                $"c.{AttributeNames.Identifier} = '{andScimFilter.ComparisonValue}'" :
                                $" AND c.{AttributeNames.Identifier} = '{andScimFilter.ComparisonValue}'";
                            Console.WriteLine("Id filter added: {0}\n", andFilter);
                        }

                        // UserName filter
                        else if (andScimFilter.AttributePath.Equals(AttributeNames.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                            }

                            andFilter += andFilter == string.Empty ?
                                $"c.{AttributeNames.UserName} = '{andScimFilter.ComparisonValue}'" :
                                $" AND c.{AttributeNames.UserName} = '{andScimFilter.ComparisonValue}'";
                            Console.WriteLine("UserName filter added: {0}\n", andFilter);
                        }

                        // ExternalId filter
                        else if (andScimFilter.AttributePath.Equals(AttributeNames.ExternalIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                            }

                            andFilter += andFilter == string.Empty ?
                                $"c.{AttributeNames.ExternalIdentifier} = '{andScimFilter.ComparisonValue}'" :
                                $" AND c.{AttributeNames.ExternalIdentifier} = '{andScimFilter.ComparisonValue}'";
                            Console.WriteLine("ExternalId filter added: {0}\n", andFilter);
                        }

                        // Active filter
                        else if (andScimFilter.AttributePath.Equals(AttributeNames.Active, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                            }

                            andFilter += andFilter == string.Empty ?
                                $"c.{AttributeNames.Active} = {andScimFilter.ComparisonValue}" :
                                $" AND c.{AttributeNames.Active} = {andScimFilter.ComparisonValue}";
                            Console.WriteLine("Active filter added: {0}\n", andFilter);
                        }

                        // Created filter
                        else if (andScimFilter.AttributePath.Equals($"{AttributeNames.Metadata}.{AttributeNames.Created}", StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrGreaterThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.Created} >= '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.Created} >= '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("Created filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.GreaterThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.Created} > '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.Created} > '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("Created filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.Created} <= '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.Created} <= '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("Created filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.Created} < '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.Created} < '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("Created filter added: {0}\n", andFilter);
                            }
                            else
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                        }
 
                        // LastModified filter
                        else if (andScimFilter.AttributePath.Equals($"{AttributeNames.Metadata}.{AttributeNames.LastModified}", StringComparison.OrdinalIgnoreCase))
                        {
                            if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrGreaterThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.LastModified} >= '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.LastModified} >= '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("LastModified filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.GreaterThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.LastModified} > '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.LastModified} > '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("LastModified filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.LastModified} <= '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.LastModified} <= '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("LastModified filter added: {0}\n", andFilter);
                            }
                            else if (andScimFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                andFilter += andFilter == string.Empty ?
                                    $"c.{AttributeNames.Metadata}.{AttributeNames.LastModified} < '{andScimFilter.ComparisonValue}'" :
                                    $" AND c.{AttributeNames.Metadata}.{AttributeNames.LastModified} < '{andScimFilter.ComparisonValue}'";
                                Console.WriteLine("LastModified filter added: {0}\n", andFilter);
                            }
                            else
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andScimFilter.FilterOperator));
                        }
                        else
                            throw new NotSupportedException(
                                string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterAttributePathNotSupportedTemplate, andScimFilter.AttributePath));

                        currentScimFilter = andScimFilter;
                        andScimFilter = andScimFilter.AdditionalFilter;

                    } while (currentScimFilter.AdditionalFilter != null);

                    queryFilter += queryFilter == string.Empty ?
                        $"({andFilter})" :
                        $" OR ({andFilter})";
                    Console.WriteLine("Query filter added: {0}\n", queryFilter);
                }

            }

            //if (parameters.PaginationParameters != null)
            //{
            //    int skip = parameters.PaginationParameters.StartIndex.HasValue ? parameters.PaginationParameters.StartIndex.Value - 1 : 0;
            //    int count = parameters.PaginationParameters.Count.HasValue ? parameters.PaginationParameters.Count.Value : 0;
            //    return Task.FromResult((results.Skip(skip).Take(count).ToArray(), total));
            //}
            //else
            //    return Task.FromResult((results.ToArray(), total));


            IEnumerable<Resource> results = await this.storage.Read(queryFilter == string.Empty ? query : $"{query} AND ({queryFilter})");
            int total = results.Count();

            return (results.ToArray(), total);
        }

        public override async Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string tenant, string correlationIdentifier)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (string.IsNullOrEmpty(parameters?.ResourceIdentifier?.Identifier))
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            string identifier = parameters.ResourceIdentifier.Identifier;

            await this.storage.InitializeAsync();
            string query = $"SELECT * FROM c WHERE c.meta.resourceType = 'User' AND c.id = '{identifier}'";
            IEnumerable<Resource> results = await this.storage.Read(query);

            if (results.Count() > 0)
                return results.FirstOrDefault();
            else
                throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task<Resource> ReplaceAsync(Resource resource, string correlationIdentifier)
        {
            throw new NotImplementedException();
        }

        public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            throw new NotImplementedException();
        }
    }
}
