using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSocialNetwork.Models
{
    public class ChatContext
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Initiator")]
        [Column("Initiator_Id")]
        public string InitiatorId { get; set; }
        public virtual ApplicationUser Initiator { get; set; }

        [ForeignKey("OtherUser")]
        [Column("OtherUser_Id")]
        public string OtherUserId { get; set; }
        public virtual ApplicationUser OtherUser { get; set; }

        public virtual ICollection<Message> History { get; set; }
        public Nullable<DateTime> LastInteraction { get; set; }
    }
}
