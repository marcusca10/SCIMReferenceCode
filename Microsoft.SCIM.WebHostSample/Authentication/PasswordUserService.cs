using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Authentication
{
    public class PasswordUserService : IUserService
    {
        public Task<User> Authenticate(string username, string password)
        {
            string validUser = "sf2aad-svc-account";
            string validPassword = "qwertz";

            return Task.FromResult(username.StartsWith(validUser + "@", StringComparison.OrdinalIgnoreCase) & password.Equals(validPassword) ? 
                new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = username
                } :
                null);
        }
    }
}
