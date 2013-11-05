using Microsoft.AspNet.Identity.EntityFramework;
using SampleSocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSocialNetwork.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<ChatContext> ChatContexts { get; set; }
        public DbSet<SignalRConnection> SignalRConnections { get; set; }

        public DbSet<UserRelation> UserRelations { get; set; }
    }
}
