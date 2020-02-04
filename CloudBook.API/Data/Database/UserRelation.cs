using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    public class UserRelation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FromUserId { get; set; }

        [Required]
        public string ToUserId { get; set; }

        [Required, DataType("text")]
        public DateTime Date { get; set; }

        [ForeignKey("UserName_1")]
        public virtual User FromUser { get; set; }

        [ForeignKey("UserName_2")]
        public virtual User ToUser { get; set; }

        #region Overrides
        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType())
                return false;
            
            UserRelation temp = (UserRelation)obj;
            return Id == temp.Id &&
                FromUserId == temp.FromUserId &&
                ToUserId == temp.ToUserId &&
                Date.Date == temp.Date.Date;
        }

        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
