using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSocialNetwork.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
            : base()
        {
            this.SignalRConnections = new HashSet<SignalRConnection>();
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<DateTime> BirthDate { get; set; }
        public string AvatarURL { get; set; }
        public virtual ICollection<SignalRConnection> SignalRConnections { get; set; }
    }
}
