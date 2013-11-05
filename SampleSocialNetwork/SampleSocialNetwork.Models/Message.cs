using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SampleSocialNetwork.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime TimeStamp { get; set; }
        [Required]
        public virtual ChatContext Context { get; set; }

        [ForeignKey("Sender")]
        [Column("Sender_Id")]
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        public bool IsRead { get; set; }
    }
}
