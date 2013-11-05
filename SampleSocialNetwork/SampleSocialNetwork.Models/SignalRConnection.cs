using System;
using System.ComponentModel.DataAnnotations;

namespace SampleSocialNetwork.Models
{
    public class SignalRConnection
    {
        [Key]
        public int Id { get; set; }
        public string ConnectionId { get; set; }
    }
}
