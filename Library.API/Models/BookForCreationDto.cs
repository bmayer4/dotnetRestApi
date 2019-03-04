using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{

    public class BookForCreationDto: BookForManipulationDto
    {

    }

    //public class BookForCreationDto  //teacher says cleaner not to add AuthorId to this dto, then we have to check if author id from req body matches what is in the uri
    //{  //good to put validations on dto and model, but only for inputs (post, delete, put, etc.)
    //    [Required(ErrorMessage = "Please fill out a title")]
    //    [MaxLength(100, ErrorMessage = "The title shouldn't have more than 100 characters")]
    //    public string Title { get; set; }

    //    [MaxLength(100, ErrorMessage = "The description shouldn't have more than 500 characters")]
    //    public string Description { get; set; }
    //}
}
