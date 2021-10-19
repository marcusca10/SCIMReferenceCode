using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Controllers
{
    [Route("odata/v2/PerPerson")]
    [ApiController]
    public class SfEndpointPerson : ControllerBase
    {
        private readonly ILogger _logger;

        public SfEndpointPerson(ILogger<SfEndpointPerson> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> Get(
            [FromQuery(Name = "$format")] string format = null,
            [FromQuery(Name = "$filter")] string filter = null,
            [FromQuery(Name = "$expand")] string expand = null,
            [FromQuery(Name = "customPageSize")] int customPageSize = 0)
        {
            _logger.LogInformation($"Received SF request: {Request.QueryString}");
            _logger.LogInformation($"Authorization header: {Request.Headers["Authorization"]}");

            string scimFilter = string.Empty;
            string scimCount = string.Empty;
            string scimQuery = string.Empty;

            if (!string.IsNullOrEmpty(filter))
                scimFilter = "filter=" +
                    filter.Replace("'", "\"")
                        .Replace("personIdExternal in", "externalId eq", StringComparison.OrdinalIgnoreCase)
                        .Replace("personEmpTerminationInfoNav/activeEmploymentsCount ge 1", "active eq true", StringComparison.OrdinalIgnoreCase)
                        .Replace("lastModifiedDateTime", "meta.lastModified", StringComparison.OrdinalIgnoreCase);

            if (customPageSize > 0)
                scimCount = $"count={customPageSize}";

            scimQuery = !string.IsNullOrEmpty(scimFilter) ?
                $"?{scimFilter}" + (!string.IsNullOrEmpty(scimCount) ? $"&{scimCount}" : string.Empty) :
                !string.IsNullOrEmpty(scimCount) ? $"?{scimCount}" : string.Empty;

            //var scimRequest = 
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Request.Host}/scim/users{scimQuery}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            QueryResponse<Core2EnterpriseUser> scimResponse;

            if (response.IsSuccessStatusCode)
            {
                JsonSerializer serializer = new JsonSerializer();
                var responseStream = new StreamReader(await response.Content.ReadAsStreamAsync());
                JsonReader json = new JsonTextReader(responseStream);
                scimResponse = serializer.Deserialize<QueryResponse<Core2EnterpriseUser>>(json);
            }
            else
            {
                //getBranchesError = true;
                return NotFound();
            }

            // Transform result to oData
            sfResponse odataResponse = new sfResponse()
            {
                //Data = new sfResults<sfPerson>() { Results = new List<sfPerson>() }
                Data = new sfResults<Core2EnterpriseUser>() { Results = scimResponse.Resources.ToList() }
            };

            // Conversion from SCIM to SF oData
            //foreach (Core2EnterpriseUser user in scimResponse.Resources)
            //{
            //    ElectronicMailAddress userMail = user.ElectronicMailAddresses.FirstOrDefault(item => item.Primary == true);

            //    odataResponse.Data.Results.Add(
            //        new sfPerson()
            //        {
            //            PersonId = user.Identifier,
            //            PersonIdExternal = user.ExternalIdentifier,
            //            PersonUuid = user.Identifier,
            //            PersonEmpTerminationInfo = new SfPersonEmpTerminationInfo()
            //            {
            //                ActiveEmploymentsCount = user.Active ? 1 : 0,
            //                LatestTerminationDate = DateTime.UtcNow.AddYears(1)
            //            },
            //            Employment = new sfResults<SfEmployment>()
            //            {
            //                Results = new List<SfEmployment>()
            //                {
            //                    new SfEmployment()
            //                    {
            //                        StartDate = DateTime.UtcNow.AddMonths(-1)
            //                    }
            //                }
            //            },
            //            PersonalInfo = new sfResults<SfPersonalInfo>()
            //            {
            //                Results = new List<SfPersonalInfo>()
            //                {
            //                    new SfPersonalInfo()
            //                    {
            //                        FirstName = user.Name.GivenName,
            //                        LastName = user.Name.FamilyName
            //                    }
            //                }
            //            }
            //        });
            //}


            return Ok(odataResponse);
        }
    }

    [DataContract]
    public class sfResponse
    {
        [DataMember(Name = "d", Order = 0)]
        public sfResults<Core2EnterpriseUser> Data // Result in SCIM format
        //public sfResults<sfPerson> Data
        {
            get;
            set;
        }
    }

    [DataContract]
    public class sfResults<T>
    {
        [DataMember(Name = "results", Order = 0)]
        public List<T> Results
        {
            get;
            set;
        }
    }

    //[DataMember(Name = "__metadata", Order = 0)]
    [DataContract]
    public class sfMetadata
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
        public sfMetadata Metadata
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
        [DataMember(Name = "personUuid", Order = 3)]
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
        public sfResults<SfEmployment> Employment
        {
            get;
            set;
        }
        [DataMember(Name = "personalInfoNav", Order = 5)]
        public sfResults<SfPersonalInfo> PersonalInfo
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
}
