using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Controllers
{
    [Route("odata/v2/PerPerson")]
    [ApiController]
    public class SfEndpointPerson : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Get(
            [FromQuery(Name = "$format")]string format = null,
            [FromQuery(Name = "$filter")]string filter = null,
            [FromQuery(Name = "$expand")]string expand = null,
            [FromQuery(Name = "$customPageSize")]int customPageSize = 100)
        {       




            //var scimRequest = 
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Request.Host}/scim/users");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                var users = await JsonSerializer.DeserializeAsync<IEnumerable<Resource[]>>(responseStream);
            }
            else
            {
                //getBranchesError = true;
                return NotFound();
            }


            return Ok();
        }
    }
}
