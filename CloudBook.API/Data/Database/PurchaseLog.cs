using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CloudBook.API.Data.Database
{
    [Serializable]
    public class PurchaseLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required, Column("user_id")]
        public string Username { get; set; }

        [Required, Column("time")]
        public DateTime Time { get; set; }

        [Required, Column("amount"), DefaultValue(0.0)]
        public double Amount { get; set; }

        [MaxLength(70), Column("summary")]
        public string Description { get; set; }

        [Column("receipt_loc")]
        public string ReceiptId { get; set; }

        [DefaultValue(false)]
        public bool isVerified { get; set; }

        public PurchaseLog() { }

        public PurchaseLog(ref CloudBook.Data.PurchaseLog model, string username, bool isVarified = false)
        {
            Time = model.Time;
            Amount = model.Amount;
            Description = model.Description;
            ReceiptId = model.ReceiptId;
            Username = username;
            isVerified = isVarified;
            model.Id = this.Id;
        }

        public void Update(ref CloudBook.Data.PurchaseLog model)
        {
            Time = model.Time;
            Amount = model.Amount;
            Description = model.Description;
        }

        /// <summary>
        /// Packs the class data for sharing
        /// </summary>
        /// <returns>A public PurchaseLog class made from the given class data</returns>
        public CloudBook.Data.PurchaseLog Pack()
        {
            return new CloudBook.Data.PurchaseLog()
            {
                Id = this.Id,
                Username = this.Username,
                Time = this.Time,
                Amount = this.Amount,
                Description = this.Description,
                ReceiptId = this.Id.ToString()
            };
        }

        #region Override
        // override object.Equals
        public override bool Equals(object obj)
        {            
            PurchaseLog log = obj as PurchaseLog;
            return log.GetType() == obj.GetType() &&
                   log.isVerified == this.isVerified &&
                   log.Time.Equals(this.Time) &&
                   log.Id == this.Id &&
                   log.Username == this.Username;
        }

        // override object.GetHashCode
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}