using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using System.Linq.Dynamic;
using CourseLibrary.API.ResourceParameters;
using Microsoft.EntityFrameworkCore;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.Services;

public class CourseLibraryRepository : ICourseLibraryRepository
{
    private readonly CourseLibraryContext _context;
    private readonly IPropertyMappingService _propertyMappingService;

    public CourseLibraryRepository(CourseLibraryContext context, IPropertyMappingService propertyMappingService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
    }

    public void AddCourse(Guid authorId, Course course)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (course == null)
        {
            throw new ArgumentNullException(nameof(course));
        }

        // always set the AuthorId to the passed-in authorId
        course.AuthorId = authorId;
        _context.Courses.Add(course);
    }

    public void DeleteCourse(Course course)
    {
        _context.Courses.Remove(course);
    }

    public async Task<Course> GetCourseAsync(Guid authorId, Guid courseId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(courseId));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return await _context.Courses
          .Where(c => c.AuthorId == authorId && c.Id == courseId).FirstOrDefaultAsync();
#pragma warning restore CS8603 // Possible null reference return.
    }

    public async Task<IEnumerable<Course>> GetCoursesAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Courses
                    .Where(c => c.AuthorId == authorId)
                    .OrderBy(c => c.Title).ToListAsync();
    }

    public void UpdateCourse(Course course)
    {
        // no code in this implementation
    }

    public void AddAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        // the repository fills the id (instead of using identity columns)
        author.Id = Guid.NewGuid();

        foreach (var course in author.Courses)
        {
            course.Id = Guid.NewGuid();
        }

        _context.Authors.Add(author);
    }

    public async Task<bool> AuthorExistsAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Authors.AnyAsync(a => a.Id == authorId);
    }

    public void DeleteAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        _context.Authors.Remove(author);
    }

    public async Task<Author> GetAuthorAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

#pragma warning disable CS8603 // Possível retorno de referência nula.
        return await _context.Authors.FirstOrDefaultAsync(a => a.Id == authorId);
#pragma warning restore CS8603 // Possível retorno de referência nula.
    }
   
    public async Task<IEnumerable<Author>> GetAuthorsAsync()
    {
        return await _context.Authors.ToListAsync();
    }

    public async Task<IEnumerable<Author>> GetAuthorsAsync(IEnumerable<Guid> authorIds)
    {
        if (authorIds == null)
        {
            throw new ArgumentNullException(nameof(authorIds));
        }

        return await _context.Authors.Where(a => authorIds.Contains(a.Id))
            .OrderBy(a => a.FirstName)
            .OrderBy(a => a.LastName)
            .ToListAsync();
    }

    public void UpdateAuthor(Author author)
    {
        // no code in this implementation
    }

    public async Task<bool> SaveAsync()
    {
        return (await _context.SaveChangesAsync() >= 0);
    }
    public async Task<PagedList<Author>> GetAuthorsAsync(AuthorsResourceParameters authorsResourceParameters)
    {
        if (authorsResourceParameters == null)
        {
            throw new ArgumentException(nameof(authorsResourceParameters));
        }
        //if (string.IsNullOrWhiteSpace(authorsResourceParameters.MainCategory)
        //    && string.IsNullOrWhiteSpace(authorsResourceParameters.SearchQuery))
        //{
        //    return await GetAuthorsAsync();
        //}

        var collection = _context.Authors as IQueryable<Author>;

        if (string.IsNullOrWhiteSpace(authorsResourceParameters.MainCategory))
        {
#pragma warning disable CS8602
            authorsResourceParameters.MainCategory = authorsResourceParameters.MainCategory.Trim();
#pragma warning restore CS8602 
            collection = collection.Where(a => a.MainCategory == authorsResourceParameters.MainCategory);
        }
        if (string.IsNullOrWhiteSpace(authorsResourceParameters.MainCategory))
        {
#pragma warning disable CS8602 
            authorsResourceParameters.SearchQuery = authorsResourceParameters.SearchQuery.Trim();
#pragma warning restore CS8602 

            collection = collection.Where(a => a.MainCategory.Contains(authorsResourceParameters.SearchQuery)
                || a.FirstName.Contains(authorsResourceParameters.SearchQuery)
                || a.LastName.Contains(authorsResourceParameters.SearchQuery));

        }

        if (!string.IsNullOrWhiteSpace(authorsResourceParameters.OrderBy))
        {

            // get property mapping dictionary

            var authorPropertyMappingDictionary = _propertyMappingService.GetPropertyMappping<AuthorDto, Author>();


            collection = collection.ApplySort(authorsResourceParameters.OrderBy, authorPropertyMappingDictionary);
            if(authorsResourceParameters.OrderBy.ToLowerInvariant() == "nome")
            {
                collection = collection.OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName);
            }
        }

        return await PagedList<Author>.CreateAsync(collection,
            authorsResourceParameters.PageNumber,
            authorsResourceParameters.PageSize);
    }
}
