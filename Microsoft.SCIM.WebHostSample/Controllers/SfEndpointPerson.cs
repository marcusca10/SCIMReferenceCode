using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.SCIM.WebHostSample.Controllers
{
    [Route("odata/v2/PerPerson")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class SfEndpointPerson : ControllerBase
    {
        private readonly ILogger _logger;

        public SfEndpointPerson(ILogger<SfEndpointPerson> logger)
        {
            _logger = logger;
        }


        [HttpGet("{identifier}")]
        public async Task<ActionResult> Get(string identifier,
            [FromQuery(Name = "$format")] string format = null,
            [FromQuery(Name = "$filter")] string filter = null,
            [FromQuery(Name = "$expand")] string expand = null,
            [FromQuery(Name = "customPageSize")] int customPageSize = 0)

        {
            _logger.LogWarning($"Received SF request: {Request.Method}\n\t{Request.Path}{Request.QueryString}");
            _logger.LogWarning($"SF request headers: {string.Concat(Request.Headers.Select(i => $"\n\t{i.Key} : {i.Value}"))}");

            // https://userapps.support.sap.com/sap/support/knowledge/en/2359742
            if (identifier.Equals("$count", StringComparison.OrdinalIgnoreCase))
            {
                IEnumerable<Core2EnterpriseUser> scimUsers = await GetScimUsers(filter, customPageSize);

                if (scimUsers != null)
                    return Ok(scimUsers.Count().ToString());

                return BadRequest();
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<ActionResult> Get(
            [FromQuery(Name = "$format")] string format = null,
            [FromQuery(Name = "$filter")] string filter = null,
            [FromQuery(Name = "$expand")] string expand = null,
            [FromQuery(Name = "customPageSize")] int customPageSize = 0)
        {
            _logger.LogWarning($"Received SF request: {Request.Method}\n\t{Request.Path}{Request.QueryString}");
            _logger.LogWarning($"SF request headers: {string.Concat(Request.Headers.Select(i => $"\n\t{i.Key} : {i.Value}"))}");
            //_logger.LogWarning($"Authorization header: {Request.Headers["Authorization"]}");

            // Get company name: @<companyID>
            var company = User.Identity.Name.Split(new[] { '@' }, 2)[1];
            _logger.LogWarning($"Company (from username): {company}");

            try
            {
                IEnumerable<Core2EnterpriseUser> scimUsers = await GetScimUsers(filter, customPageSize);

                // Transform result to oData
                SfResponse odataResponse = new SfResponse()
                {
                    // Result in SCIM format
                    //Data = new sfResults<Core2EnterpriseUser>();

                    Data = new SfResults<sfPerson>() { Results = new List<sfPerson>() }
                };

                if (scimUsers != null)
                {
                    // Result in SCIM format
                    //Data.Results = scimResponse.Resources.ToList();

                    // Conversion from SCIM to SF oData
                    foreach (Core2EnterpriseUser user in scimUsers)
                    {
                        //ElectronicMailAddress userMail = user.ElectronicMailAddresses != null ? user.ElectronicMailAddresses.FirstOrDefault(item => item.Primary == true) : null;
                        PhoneNumber userPhone = user.PhoneNumbers != null ? user.PhoneNumbers.FirstOrDefault(item => item.Primary == true) : null;
                        Address userAddress = user.Addresses != null ? user.Addresses.FirstOrDefault(item => item.Primary == true) : null;

                        odataResponse.Data.Results.Add(
                            new sfPerson()
                            {
                                PersonId = user.Identifier,
                                PersonIdExternal = user.ExternalIdentifier,
                                PersonUuid = user.Identifier,
                                Metadata = new SfMetadata()
                                {
                                    Uri = $"https://{Request.Host}/odata/v2/public/PerPerson('{user.ExternalIdentifier}')",
                                    Type = "SFOData.PerPerson"
                                },
                                PersonEmpTerminationInfo = new SfPersonEmpTerminationInfo()
                                {
                                    ActiveEmploymentsCount = user.Active ? 1 : 0,
                                    LatestTerminationDate = DateTime.UtcNow.AddYears(1)
                                },
                                Employment = new SfResults<SfEmployment>()
                                {
                                    Results = new List<SfEmployment>()
                                    {
                                new SfEmployment()
                                {
                                    // startDate not available in SCIM user schema
                                    StartDate = user.Metadata.Created,
                                    JobInfo = new SfResults<SfJobInfo>()
                                    {
                                        Results = new List<SfJobInfo>()
                                        {
                                            new SfJobInfo()
                                            {
                                                Company = new SfCompany()
                                                {
                                                    NameLocalized = company.ToUpperInvariant()
                                                },
                                                Department = new SfDepartment()
                                                {
                                                    NameLocalized = user.EnterpriseExtension.Department
                                                },
                                                JobTitle = user.Title,
                                                Location = new SfLocation()
                                                {
                                                    AddressNavDEFLT = new SfAddressNavDEFLT()
                                                    {
                                                        Address1 = userAddress != null ? userAddress.StreetAddress : null,
                                                        City = userAddress != null ? userAddress.Locality : null,
                                                        ZipCode = userAddress != null ? userAddress.PostalCode : null,
                                                        State = new SfResults<SfState>()
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    User = new SfUser()
                                    {
                                        State = userAddress != null ? userAddress.Region : null,
                                        Country = userAddress != null ? userAddress.Country : null,
                                        Manager = new SfManager()
                                        {
                                            Empinfo = new SfEmpinfo()
                                            {
                                                PersonIdExternal = null
                                            }
                                        }
                                    }
                                }
                                    }
                                },
                                PersonalInfo = new SfResults<SfPersonalInfo>()
                                {
                                    Results = new List<SfPersonalInfo>()
                                    {
                                    new SfPersonalInfo()
                                    {
                                        FirstName = user.Name.GivenName,
                                        LastName = user.Name.FamilyName
                                    }
                                    }
                                },
                                Phone = new SfResults<SfPhone>()
                                {
                                    Results = new List<SfPhone>()
                                    {
                                    new SfPhone()
                                    {
                                        IsPrimary = true,
                                        PhoneNumber = userPhone != null ? userPhone.Value : null
                                    }
                                    }
                                },
                                Email = new SfResults<SfEmail>()
                                {
                                    Results = new List<SfEmail>()
                                }
                            });
                    }
                }

                // Handle SF Headers
                var xCsrfToken = Request.Headers["X-CSRF-Token"];
                var sessionCookie = Request.Cookies["SAP_SESSION_MSFT"];
                if (sessionCookie != null)
                    Response.Cookies.Append("SAP_SESSION_MSFT", sessionCookie);
                else
                    Response.Cookies.Append("SAP_SESSION_MSFT", Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
                if (xCsrfToken.Count() > 1)
                    Response.Headers.Add("X-CSRF-Token", xCsrfToken);
                else
                    Response.Headers.Add("X-CSRF-Token", Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

                _logger.LogWarning($"SF response headers: {string.Concat(Response.Headers.Select(i => $"\n\t{i.Key} : {i.Value}"))}");
                return Ok(odataResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(); 
            }
        }

        async Task<IEnumerable<Core2EnterpriseUser>> GetScimUsers(string filter, int customPageSize)
        {
            string scimFilter = string.Empty;
            if (!string.IsNullOrEmpty(filter))
            {
                // remove irrelevant clauses from filter
                if (filter.IndexOf(" or", StringComparison.OrdinalIgnoreCase) > 0)
                    filter = filter.Substring(0, filter.IndexOf(" or", StringComparison.OrdinalIgnoreCase));

                // adapt filter to SCIM
                filter = filter.Replace("'", "\"")
                        .Replace("personIdExternal in", "externalId eq", StringComparison.OrdinalIgnoreCase) // On Demand
                        .Replace("personEmpTerminationInfoNav/activeEmploymentsCount ne null", "active eq true", StringComparison.OrdinalIgnoreCase)
                        .Replace("lastModifiedDateTime", "meta.lastModified", StringComparison.OrdinalIgnoreCase)
                        .Replace("datetimeoffset", "", StringComparison.OrdinalIgnoreCase);
                scimFilter = "filter=" + filter;
            }

            string scimCount = (customPageSize > 0) ? $"count={customPageSize}" : string.Empty;

            string scimQuery = !string.IsNullOrEmpty(scimFilter) ?
                $"?{scimFilter}" + (!string.IsNullOrEmpty(scimCount) ? $"&{scimCount}" : string.Empty) :
                !string.IsNullOrEmpty(scimCount) ? $"?{scimCount}" : string.Empty;

            //var scimRequest = 
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Request.Host}/scim/users{scimQuery}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            IEnumerable<Core2EnterpriseUser> result;

            if (response.IsSuccessStatusCode)
            {
                JsonSerializer serializer = new JsonSerializer();
                var responseStream = new StreamReader(await response.Content.ReadAsStreamAsync());
                JsonReader json = new JsonTextReader(responseStream);
                QueryResponse<Core2EnterpriseUser> scimResponse = serializer.Deserialize<QueryResponse<Core2EnterpriseUser>>(json);
                result = scimResponse.Resources;
            }
            else
                result = null;

            return result;
        }
    }


    #region SF Data Model

    // https://msazure.visualstudio.com/One/_git/AD-IAM-Services-SyncFabric?path=/src/dev/Controller/Connectors/SuccessFactors/SuccessFactorsRestDataModel.cs
    [DataContract]
    public class SfResponse
    {
        [DataMember(Name = "d", Order = 0)]
        // Result in SCIM format
        //public sfResults<Core2EnterpriseUser> Data 
        public SfResults<sfPerson> Data
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfResults<T>
    {
        [DataMember(Name = "results", Order = 1)]
        public List<T> Results
        {
            get;
            set;
        }

        [DataMember(Name = "__next", Order = 0)]
        public string NextPage { get; set; }

    }

    //[DataMember(Name = "__metadata", Order = 0)]
    [DataContract]
    public class SfMetadata
    {
        [DataMember(Name = "uri", Order = 0)]
        public string Uri
        {
            get;
            set;
        }
        [DataMember(Name = "type", Order = 1)]
        public string Type
        {
            get;
            set;
        }
    }

    [DataContract]
    public class sfPerson
    {
        [DataMember(Name = "__metadata", Order = 0)]
        public SfMetadata Metadata
        {
            get;
            set;
        }
        [DataMember(Name = "personIdExternal", Order = 1)]
        public string PersonIdExternal
        {
            get;
            set;
        }
        [DataMember(Name = "personId", Order = 2)]
        public string PersonId
        {
            get;
            set;
        }
        [DataMember(Name = "perPersonUuid", Order = 3)]
        public string PersonUuid
        {
            get;
            set;
        }

        [DataMember(Name = "personEmpTerminationInfoNav", Order = 6)]
        public SfPersonEmpTerminationInfo PersonEmpTerminationInfo
        {
            get;
            set;
        }

        [DataMember(Name = "employmentNav", Order = 4)]
        public SfResults<SfEmployment> Employment
        {
            get;
            set;
        }

        [DataMember(Name = "personalInfoNav", Order = 5)]
        public SfResults<SfPersonalInfo> PersonalInfo
        {
            get;
            set;
        }

        [DataMember(Name = "phoneNav", Order = 6)]
        public SfResults<SfPhone> Phone
        {
            get;
            set;
        }

        [DataMember(Name = "emailNav", Order = 7)]
        public SfResults<SfEmail> Email
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfEmployment
    {
        [DataMember(Name = "startDate", Order = 0)]
        public DateTime StartDate
        {
            get;
            set;
        }

        [DataMember(Name = "jobInfoNav", Order = 1)]
        public SfResults<SfJobInfo> JobInfo
        {
            get;
            set;
        }

        [DataMember(Name = "userNav", Order = 2)]
        public SfUser User
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfPersonalInfo
    {
        [DataMember(Name = "firstName", Order = 0)]
        public string FirstName
        {
            get;
            set;
        }
        [DataMember(Name = "lastName", Order = 1)]
        public string LastName
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfPersonEmpTerminationInfo
    {
        [DataMember(Name = "activeEmploymentsCount", Order = 0)]
        public int ActiveEmploymentsCount
        {
            get;
            set;
        }
        [DataMember(Name = "latestTerminationDate", Order = 1)]
        public DateTime LatestTerminationDate
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfCompany
    {
        [DataMember(Name = "name_localized", Order = 0)]
        public string NameLocalized
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfDepartment
    {
        [DataMember(Name = "name_localized", Order = 0)]
        public string NameLocalized
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfAddressNavDEFLT
    {
        [DataMember(Name = "address1", Order = 0)]
        public string Address1
        {
            get;
            set;
        }

        [DataMember(Name = "city", Order = 1)]
        public string City
        {
            get;
            set;
        }

        [DataMember(Name = "zipCode", Order = 2)]
        public string ZipCode
        {
            get;
            set;
        }

        //employmentNav/jobInfoNav/locationNav/addressNavDEFLT/stateNav
        [DataMember(Name = "stateNav", Order = 3)]
        public SfResults<SfState> State
        {
            get;
            set;
        }
    }

    [DataContract]
    public class SfState
    {
        [DataMember(Name = "id", Order = 0)]
        public string Address1
        {
            get;
            set;
        }
    }


    //$.employmentNav.results[0].jobInfoNav.results[0].locationNav.addressNavDEFLT.address1
    //$.employmentNav.results[0].jobInfoNav.results[0].locationNav.addressNavDEFLT.city
    //$.employmentNav.results[0].jobInfoNav.results[0].locationNav.addressNavDEFLT.zipCode
    [DataContract]
    public class SfLocation
    {
        [DataMember(Name = "addressNavDEFLT", Order = 0)]
        public SfAddressNavDEFLT AddressNavDEFLT
        {
            get;
            set;
        }
    }


    [DataContract]
    public class SfJobInfo
    {
        [DataMember(Name = "companyNav", Order = 0)]
        public SfCompany Company
        {
            get;
            set;
        }

        //$.employmentNav.results[0].jobInfoNav.results[0].departmentNav.name_localized
        [DataMember(Name = "departmentNav", Order = 1)]
        public SfDepartment Department
        {
            get;
            set;
        }

        //$.employmentNav.results[0].jobInfoNav.results[0].jobTitle
        [DataMember(Name = "jobTitle", Order = 2)]
        public string JobTitle
        {
            get;
            set;
        }

        [DataMember(Name = "locationNav", Order = 3)]
        public SfLocation Location
        {
            get;
            set;
        }
    }

    //$.employmentNav.results[0].userNav.state
    //$.employmentNav.results[0].userNav.country
    [DataContract]
    public class SfUser
    {
        [DataMember(Name = "state", Order = 0)]
        public string State
        {
            get;
            set;
        }
        [DataMember(Name = "country", Order = 1)]
        public string Country
        {
            get;
            set;
        }

        //employmentNav/userNav/manager/empInfo/
        [DataMember(Name = "manager", Order = 2)]
        public SfManager Manager
        {
            get;
            set;
        }
    }
    [DataContract]
    public class SfManager
    {
        [DataMember(Name = "empinfo", Order = 0)]
        public SfEmpinfo Empinfo
        {
            get;
            set;
        }
    }
    [DataContract]
    public class SfEmpinfo
    {
        [DataMember(Name = "personIdExternal", Order = 0)]
        public string PersonIdExternal
        {
            get;
            set;
        }
    }



    //$.phoneNav.results[?(@.isPrimary == true)].phoneNumber
    [DataContract]
    public class SfPhone
    {
        [DataMember(Name = "isPrimary", Order = 0)]
        public bool IsPrimary
        {
            get;
            set;
        }

        [DataMember(Name = "phoneNumber", Order = 1)]
        public string PhoneNumber
        {
            get;
            set;
        }
    }

    //$.emailNav.results[?(@.isPrimary == true)].emailAddress
    [DataContract]
    public class SfEmail
    {
        [DataMember(Name = "isPrimary", Order = 0)]
        public bool IsPrimary
        {
            get;
            set;
        }

        [DataMember(Name = "emailAddress", Order = 1)]
        public string EmailAddress
        {
            get;
            set;
        }
    }

    #endregion
}
