using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CloudBook.API.Data.Database
{
    [Table("AspNetUser")]
    public class User : IdentityUser<Guid>
    {
        [Required, MaxLength(15)]
        public string FirstName { get; set; }

        [Required, MaxLength(20)]
        public string LastName { get; set; }

        [Required]
        public char Gender { get; set; }

        [Required, MaxLength(3)]
        public string Location { get; set; }

        [Required]
        public DateTime Birthday { get; set; }

        public string University { get; set; }

        [Required, DefaultValue(false)]
        public bool IsActive { get; set; }      // Indicates if a user's account is active or not. Will be depricated

        [DefaultValue(0.0f)]
        public float TotalSpending { get; set; }

        /// <summary>
        /// Push notification Id
        /// </summary>
        public string VAPID { get; set; }

        public User() => Id = Guid.NewGuid();

        public User(ref CloudBook.Data.Registration form)
        {
            Id = Guid.NewGuid();
            UserName = form.UserName;
            FirstName = form.FirstName;
            LastName = form.LastName;
            Gender = form.Gender;
            Location = form.Location;
            Birthday = form.Birthday;
            University = form.University;
        }

        // Updates the current user data based on the one provided
        public void Update(ref CloudBook.Data.User model)
        {
            FirstName = model.FirstName;
            LastName = model.LastName;
            Location = model.Location;
            University = model.University;
            Location = model.Location;
        }

        // Creates a client-side version of user data
        public CloudBook.Data.User Pack() => new CloudBook.Data.User()
        {
            UserName = this.UserName,
            FirstName = this.FirstName,
            LastName = this.LastName,
            Gender = this.Gender,
            Location = this.Location,
            Birthday = this.Birthday,
            University = this.University
        };
    }
}