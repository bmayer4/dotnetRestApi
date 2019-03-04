using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{

    public class BookForUpdateDto: BookForManipulationDto
    {
        [Required(ErrorMessage = "You should fill out this description")]  //validation on base class will still apply, and this one will be added
        public override string Description
        {   //not sure why teacher did this instead of public override string Description { get; set; } (probably because you cant change get and set of property using auto init properties
            get => base.Description;
            set => base.Description = value;
        }
    }

    //commenting below after adding manipulationdto to reduce duplicate code
    //public class BookForUpdateDto  //no id because it is already part of uri, if we put it we would need another check to make sure what is in body matches uri
    //{
    //    [Required(ErrorMessage = "Please fill out a title")]
    //    [MaxLength(100, ErrorMessage = "The title shouldn't have more than 100 characters")]
    //    public string Title { get; set; }

    //    [Required(ErrorMessage = "Please fill out a description")]  //not required on createdto, another reason to have seperate dtos
    //    [MaxLength(500, ErrorMessage = "The description shouldn't have more than 500 characters")]
    //    public string Description { get; set; }
    //}
}
