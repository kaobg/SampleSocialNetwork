using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSocialNetwork.Models
{
    public class UserRelation
    {
        public int Id { get; set; }
        public ApplicationUser RelatingUser { get; set; }
        public ApplicationUser RelatedUser { get; set; }
        public UserRelationType RelationType { get; set; }
    }
}
