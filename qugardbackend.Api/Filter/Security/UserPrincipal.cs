using System.Security.Claims;

namespace examportal.Filter.Security
{
    public class UserPrincipal : ClaimsPrincipal
    {
        public UserPrincipal(ClaimsPrincipal principal) : base(principal)
        {

        }

        private string GetClaimValue(string key)
        {
            var claim = this.Claims.FirstOrDefault(c => c.Type == key);
            return claim?.Value;
        }

        public String UserName
        {
            get
            {
                if (this.FindFirst(ClaimTypes.GivenName) == null)
                    return String.Empty;

                return this.FindFirst(ClaimTypes.GivenName).Value;
            }
        }

        public string UserId
        {
            get
            {
                if (this.FindFirst(ClaimTypes.NameIdentifier) == null)
                    return String.Empty;

                return GetClaimValue(ClaimTypes.NameIdentifier);
            }
        }

        public String Name
        {
            get
            {
                if (this.FindFirst(ClaimTypes.Name) == null)
                    return String.Empty;

                return this.FindFirst(ClaimTypes.Name).Value;
            }
        }
        public string Email
        {
            get
            {
                if (this.FindFirst(ClaimTypes.Email) == null)
                    return String.Empty;

                return this.FindFirst(ClaimTypes.Email).Value;
            }
        }

        public bool IsSystemAdmin
        {
            get
            {
                if (this.FindFirst(ClaimTypes.Role) == null)
                    return false;

                return this.FindAll(ClaimTypes.Role).Any(c =>
                {
                    if ("system admin".Equals(c.Value, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else
                        return false;
                });
            }
        }
        public bool IsTenantAdmin
        {
            get
            {
                if (this.FindFirst(ClaimTypes.Role) == null)
                    return false;

                return this.FindAll(ClaimTypes.Role).Any(c =>
                {
                    if ("tenant admin".Equals(c.Value, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else
                        return false;
                });
            }
        }

        public bool IsStudent
        {
            get
            {
                if (this.FindFirst(ClaimTypes.Role) == null)
                    return false;

                return this.FindAll(ClaimTypes.Role).Any(c =>
                {
                    if ("student".Equals(c.Value, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else
                        return false;
                });
            }
        }
    }
}