﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    public class Request
    {
        [Key]
        public Guid Id { get; set; }          // 16 bit autogenerated unsigned integer as ID

        [Required]
        public string Target { get; set; }      // The ID of the object being referred to from the User

        [Required]
        public string UserName { get; set; }

        [ForeignKey("UserName")]
        public User User { get; set; }

        [Required, DefaultValue(null)]
        public string Data { get; set; }        // Additional space to store relative data
    }
}