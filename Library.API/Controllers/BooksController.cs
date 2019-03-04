using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController: Controller  //books are children of author (author has many books, each book as one author)
    {
        private ILibraryRepository _libraryRepository;
        private ILogger<BooksController> _logger;

        public BooksController(ILibraryRepository libraryRepository, ILogger<BooksController> logger)
        {
            _libraryRepository = libraryRepository;
            _logger = logger;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            return Ok(booksForAuthor);
        }


        [HttpGet("{id}", Name="GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(bookForAuthor);
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)  //this is here because model state will be valid if body can't be deserialized to the exptected type
            {
                return NotFound();
            }

            if (book.Description == book.Title) {  //custom validation
            
                //first param (key) can be property but is often class name
                ModelState.AddModelError(nameof(BookForCreationDto), "The provided description should be different than the title");
            }

            if (!ModelState.IsValid)
            {
                //return 422
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity); //lets us create author w or w/out books

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Creating a book for author {authorId} failed on save.");
            }

            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteBook(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Deleting book for author {authorId} failed on save.");
            }

            _logger.LogInformation($"book {id} with {authorId} was deleted");

            return NoContent();
        }

        [HttpPut("{id}")]  //Full update (in rest api patch is preferred over put)
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different than the title");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                //return NotFound();    //added below to add upserting feature (when consumer can create uri)
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd); //we have to have our id a real guid format or it wouldn't pass check in repository and a new guid would be created

                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting book for author {authorId} failed on save.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);  

                //upserting is creating a resource
                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
            }

            //here existing destination is entity
            //taking EVERYTHING from ***dto*** and merging into entity, so ids would still be there since part of entity and not in dto, would only remove properties that are part of dto and not included
            Mapper.Map(book, bookForAuthorFromRepo);  //entity state is modified, need to save it for changes to take place.

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);  //does not do anything, here to follow repository pattern (tests may need it)

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Updating book for author {authorId} failed on save.");
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)  //dont want id in dto, dont want it to be changed and wed have to do an extra check to see if it matches passed in id
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                //return NotFound();   //removed this when adding upserting feature
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different than the title");
                }

                TryValidateModel(bookDto);

                if (!ModelState.IsValid) 
                {
                    return new UnprocessableEntityObjectResult(ModelState);
                }

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);  //still no authord id on book at this point

                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting book for author {authorId} failed on save.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);   //now has an authorid, save is what caused it

                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
            }

            //we need to transfrom book from store to BookForUpdateDto
            //here new destination is dto
            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);  //update is on resource(dto) and not entity 

            patchDoc.ApplyTo(bookToPatch, ModelState);  //now any errors in the patch doc will make the modelstate invalid
           
            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different than the title");
            }

            TryValidateModel(bookToPatch);  //any errors will end up in the model state

            if (!ModelState.IsValid)  //inputted value is jsonpatchdoc, not dto, so it may contain errors
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);  //***did this for put, the difference here is our input dto got merged into the entity via apply method first (entity had to be converted to dto first) so missing fields from dto would be there still (unlike put) before being used in this mapper function as first param

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);  //does not do anything, here to follow repository pattern (tests may need it)

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Patching book for author {authorId} failed on save.");
            }

            return NoContent();

        }
    }
}
