using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{  //abstract modifier in class decoration indicates the class is intended only to be a base class of other classes
    public abstract class BookForManipulationDto   //https://www.guru99.com/c-sharp-abstract-class.html
    {
        [Required(ErrorMessage = "Please fill out a title")]
        [MaxLength(100, ErrorMessage = "The title shouldn't have more than 100 characters")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "The description shouldn't have more than 500 characters")]
        public virtual string Description { get; set; }  //virtual properties are great when you have an implementation in the base class, and we want to allow overrriding
    }
}
//htt...ps://csharp.net-tutorials.com/classes/abstract-classes/
//htt..ps://stackoverflow.com/questions/14728761/difference-between-virtual-and-abstract-methods  //**top answer!
//Virtual methods have an implementation and provide the derived classes with the option of overriding it. 
//Abstract methods do not provide an implementation and force the derived classes to override the method. So, abstract methods have no actual code in them, and subclasses HAVE TO override the method.
