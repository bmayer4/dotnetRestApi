using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        private LibraryContext _context;
        private IPropertyMappingService _propertyMappingService;

        public LibraryRepository(LibraryContext context, IPropertyMappingService propertyMappingService)
        {
            _context = context;
            _propertyMappingService = propertyMappingService;
        }

        public void AddAuthor(Author author)
        {
            author.Id = Guid.NewGuid();     //teacher says ef will create guids for us...
            _context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }

        public void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = GetAuthor(authorId);
            if (author != null)
            {
                // if there isn't an id filled out (ie: we're not upserting),
                // we should generate one
                if (book.Id == Guid.Empty) //post a new book that doest have one, generate it. 
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }

        public bool AuthorExists(Guid authorId)
        {
            return _context.Authors.Any(a => a.Id == authorId);
        }

        public void DeleteAuthor(Author author)
        {
            _context.Authors.Remove(author);
        }

        public void DeleteBook(Book book)
        {
            _context.Books.Remove(book);
        }

        public Author GetAuthor(Guid authorId)
        {
            return _context.Authors.FirstOrDefault(a => a.Id == authorId);  //either would work
            //return _context.Authors.Include(a => a.Books).FirstOrDefault();
        }

        public PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {  //deferred execution is at work here..Authors dbset implements iqueryable, orderby and thenby return iqueryable (put paging last because we want to page on the sorted filtered collection). Also, //only when toList is called that query is created and executed on database
           //var collectionBeforePaging = _context.Authors
           //.OrderBy(a => a.FirstName)
           //.ThenBy(a => a.LastName).AsQueryable();  //cast to asQueryable so the genre filter will work since Where returns a IQueryable and couldn't be converted to the IOrderedQueryable that this (OrderBy and ThenBy) returns
           //FYI probably did not need this, all this was created to add in multiple order by params.. (wrote this line 1/13/19)
            //since we can't sort by strings (using SortBy query), we will use linq (add nuget system.linq.dynamic.core)
            //issue here is name field on resource maps to two fields on entity, first and last (remember we only query on resource fields)
            //and sort ascending by age maps to sort descending by date of birth
            //could have used switch statement instead of building this service
            var collectionBeforePaging = _context.Authors
                .ApplySort(authorsResourceParameters.OrderBy, _propertyMappingService.GetPropertyMapping<AuthorDto, Author>());

            if (!string.IsNullOrEmpty(authorsResourceParameters.Genre))  //filtering
            {
                //trim and ignore casing
                var genreForWhereClause = authorsResourceParameters.Genre.Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging.Where(a => a.Genre.ToLowerInvariant() == genreForWhereClause);
            }

            if (!string.IsNullOrEmpty(authorsResourceParameters.SearchQuery))  //searching
            {
                var searchQueryWhereClause = authorsResourceParameters.SearchQuery.Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLowerInvariant().Contains(searchQueryWhereClause)
                    || a.FirstName.ToLowerInvariant().Contains(searchQueryWhereClause)
                    || a.LastName.ToLowerInvariant().Contains(searchQueryWhereClause));
            }

                return PagedList<Author>.Create(collectionBeforePaging, authorsResourceParameters.PageNumber, authorsResourceParameters.PageSize);
        }

        public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return _context.Authors.Where(a => authorIds.Contains(a.Id))
                .OrderBy(a => a.FirstName)
                .OrderBy(a => a.LastName)
                .ToList();
        }

        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }

        public Book GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return _context.Books
              .Where(b => b.AuthorId == authorId && b.Id == bookId).FirstOrDefault();
        }

        public IEnumerable<Book> GetBooksForAuthor(Guid authorId)
        {
            return _context.Books.Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToList();
        }

        public void UpdateBookForAuthor(Book book)
        {
            // no code in this implementation
        }

        public bool Save()
        {
            return (_context.SaveChanges() >= 0);
        }
    }
}
