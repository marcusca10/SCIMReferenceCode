using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Authentication
{
    public class User
    {
        public string Id { get; internal set; }
        public string Username { get; internal set; }
    }


    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
    }
}
