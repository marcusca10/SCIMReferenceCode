// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

namespace Microsoft.SCIM.WebHostSample.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.SCIM;

    public class InMemoryUserProvider : ProviderBase
    {
        private readonly InMemoryStorage storage;

        public InMemoryUserProvider()
        {
            this.storage = InMemoryStorage.Instance;
        }

        public override Task<Resource> CreateAsync(Resource resource, string tenant, string correlationIdentifier)
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

            IEnumerable<Core2EnterpriseUser> exisitingUsers = this.storage.Users.Values;
            if
            (
                exisitingUsers.Any(
                    (Core2EnterpriseUser exisitingUser) =>
                        string.Equals(exisitingUser.UserName, user.UserName, StringComparison.OrdinalIgnoreCase))
            )
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            // Update metadata
            DateTime created = DateTime.UtcNow;
            user.Metadata.Created = created;
            user.Metadata.LastModified = created;

            string resourceIdentifier = Guid.NewGuid().ToString();
            resource.Identifier = resourceIdentifier;
            this.storage.Users.Add(resourceIdentifier, user);

            return Task.FromResult(resource);
        }

        public override Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(resourceIdentifier?.Identifier))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            string identifier = resourceIdentifier.Identifier;

            if (this.storage.Users.ContainsKey(identifier))
            {
                this.storage.Users.Remove(identifier);
            }

            return Task.CompletedTask;
        }

        public override Task<(Resource[], int)> QueryAsync(IQueryParameters parameters, string tenant, string correlationIdentifier)
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

            IEnumerable<Resource> results;
            var predicate = PredicateBuilder.False<Core2EnterpriseUser>();
            Expression<Func<Core2EnterpriseUser, bool>> predicateAnd;

            //IEnumerable<Resource> results = new List<Core2EnterpriseUser>();

            if (parameters.AlternateFilters.Count <= 0)
            {
                results = this.storage.Users.Values.Select(
                    (Core2EnterpriseUser user) => user as Resource);
            }
            else
            {
                //results = new List<Core2EnterpriseUser>();

                foreach (IFilter queryFilter in parameters.AlternateFilters)
                {
                    predicateAnd = PredicateBuilder.True<Core2EnterpriseUser>();
                    //IEnumerable<Core2EnterpriseUser> users = this.storage.Users.Values;

                    IFilter andFilter = queryFilter;
                    IFilter currentFilter = andFilter;
                    do
                    {
                        if (string.IsNullOrWhiteSpace(andFilter.AttributePath))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        else if (string.IsNullOrWhiteSpace(andFilter.ComparisonValue))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        // UserName filter
                        else if (andFilter.AttributePath.Equals(AttributeNames.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            string userName = andFilter.ComparisonValue;
                            predicateAnd = predicateAnd.And(p => string.Equals(p.UserName, userName, StringComparison.OrdinalIgnoreCase));

                            //users =
                            //    users.Where(
                            //        item =>
                            //           string.Equals(
                            //                item.UserName,
                            //               andFilter.ComparisonValue,
                            //               StringComparison.OrdinalIgnoreCase));

                            //return Task.FromResult(results);
                        }

                        // ExternalId filter
                        else if (andFilter.AttributePath.Equals(AttributeNames.ExternalIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            string externalIdentifier = andFilter.ComparisonValue;
                            predicateAnd = predicateAnd.And(p => string.Equals(p.ExternalIdentifier, externalIdentifier, StringComparison.OrdinalIgnoreCase));

                            //users =
                            //    users.Where(
                            //        item =>
                            //           string.Equals(
                            //                item.ExternalIdentifier,
                            //               andFilter.ComparisonValue,
                            //               StringComparison.OrdinalIgnoreCase)).ToList();

                            //return Task.FromResult(results);
                        }

                        //
                        else if (andFilter.AttributePath.Equals(AttributeNames.Active, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            bool active = bool.Parse(andFilter.ComparisonValue);
                            predicateAnd = predicateAnd.And(p => p.Active == active);

                            //users =
                            //    users.Where(
                            //        item =>
                            //           item.Active == bool.Parse(andFilter.ComparisonValue)).ToList();

                            //return Task.FromResult(results);
                        }

                        //
                        else if (andFilter.AttributePath.Equals($"{AttributeNames.Metadata}.{AttributeNames.LastModified}", StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator == ComparisonOperator.EqualOrGreaterThan)
                            {
                                DateTime comparisonValue = DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime();
                                predicateAnd = predicateAnd.And(p => p.Metadata.LastModified >= comparisonValue);

                                //users =
                                //    users.Where(
                                //        item =>
                                //           item.Metadata.LastModified >= DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime()).ToList();
                            }
                            else if (andFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                DateTime comparisonValue = DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime();
                                predicateAnd = predicateAnd.And(p => p.Metadata.LastModified <= comparisonValue);

                                //users =
                                //    users.Where(
                                //        item =>
                                //           item.Metadata.LastModified <= DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime()).ToList();
                            }
                            else
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));



                            //return Task.FromResult(results);
                        }
                        else
                            throw new NotSupportedException(
                                string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterAttributePathNotSupportedTemplate, andFilter.AttributePath));

                        currentFilter = andFilter;
                        andFilter = andFilter.AdditionalFilter;

                    } while (currentFilter.AdditionalFilter != null);

                    predicate = predicate.Or(predicateAnd);

                    //results = results.Union(users.Select((Core2EnterpriseUser user) => user as Resource).ToList());
                }

                results = this.storage.Users.Values.Where(predicate.Compile());
            }

            var final = results.ToList();
            int total = results.ToList().Count();
            if (parameters.PaginationParameters != null)
            {
                int skip = parameters.PaginationParameters.StartIndex.HasValue ? parameters.PaginationParameters.StartIndex.Value - 1 : 0;
                int count = parameters.PaginationParameters.Count.HasValue ? parameters.PaginationParameters.Count.Value : 0;
                return Task.FromResult((results.Skip(skip).Take(count).ToArray(), total));
            }
            else
                return Task.FromResult((results.ToArray(), total));
        }

        public override Task<Resource> ReplaceAsync(Resource resource, string correlationIdentifier)
        {
            if (resource.Identifier == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            Core2EnterpriseUser user = resource as Core2EnterpriseUser;

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            IEnumerable<Core2EnterpriseUser> exisitingUsers = this.storage.Users.Values;
            if
            (
                exisitingUsers.Any(
                    (Core2EnterpriseUser exisitingUser) =>
                        string.Equals(exisitingUser.UserName, user.UserName, StringComparison.Ordinal) &&
                        !string.Equals(exisitingUser.Identifier, user.Identifier, StringComparison.OrdinalIgnoreCase))
            )
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            if (!this.storage.Users.TryGetValue(user.Identifier, out Core2EnterpriseUser _))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // Update metadata
            user.Metadata.LastModified = DateTime.UtcNow;

            this.storage.Users[user.Identifier] = user;
            Resource result = user as Resource;
            return Task.FromResult(result);
        }

        public override Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string tenant, string correlationIdentifier)
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
            var predicate = PredicateBuilder.False<Core2EnterpriseUser>();
            predicate = predicate.Or(p => p.Identifier == identifier);

            var result = this.storage.Users.Values.AsQueryable().Where(predicate);

            if (result.Count() > 0)
                return Task.FromResult(result.First() as Resource);
            else
                throw new HttpResponseException(HttpStatusCode.NotFound);

            //Resource result = null;

            //if (this.storage.Users.ContainsKey(identifier))
            //{
            //    if (this.storage.Users.TryGetValue(identifier, out Core2EnterpriseUser user))
            //    {
            //        result = user as Resource;
            //        return Task.FromResult(result);
            //    }
            //}

            //throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation);
            }

            PatchRequest2 patchRequest =
                patch.PatchRequest as PatchRequest2;

            if (null == patchRequest)
            {
                string unsupportedPatchTypeName = patch.GetType().FullName;
                throw new NotSupportedException(unsupportedPatchTypeName);
            }

            if (this.storage.Users.TryGetValue(patch.ResourceIdentifier.Identifier, out Core2EnterpriseUser user))
            {
                user.Apply(patchRequest);

                // Update metadata
                user.Metadata.LastModified = DateTime.UtcNow;
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return Task.CompletedTask;
        }

        public override string GetTenant(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

    }
}
