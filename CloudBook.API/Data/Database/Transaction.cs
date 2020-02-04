using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CloudBook.API.Data.Database
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required, JsonProperty("user_id"), Column("user_id")]
        public string UserId { get; set; }

        [Required, JsonProperty("payee_id"), Column("payee_id")]
        public string PayeeId { get; set; }

        [Required, DefaultValue(0.0)]
        public double Due { get; set; }

        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        [ForeignKey("payee_id")]
        public virtual User Payee { get; set; }

        public Transaction() { }

        public Transaction(User user, User payee)
        {
            User = user;
            Payee = payee;
            Due = 0.0;
        }
    }
}