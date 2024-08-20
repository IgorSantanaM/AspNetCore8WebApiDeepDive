using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Formats.Asn1;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;

        public AuthorCollectionsController(
            ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }
        [HttpGet("({authorIds})", Name = "GetAuthorsCollection")]
        public async Task<ActionResult<IEnumerable<AuthorForCreationDto>>> GetAuthorCollection([FromRoute] IEnumerable<Guid> authorIds)
        {
            var authorEntities = _mapper.Map<IEnumerable<Author>>(authorIds);
            if(authorIds.Count() != authorEntities.Count())
            {
                return NotFound();
            }
            var authorsToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<AuthorDto>>> CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            var authorEntities = _mapper.Map<IEnumerable<Author>>(authorCollection);
            foreach (var author in authorEntities)
            {
                _courseLibraryRepository.AddAuthor(author);
            }
            await _courseLibraryRepository.SaveAsync();

            // return the response body
            var authorCollectionToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            var authorsIdAsString = string.Join(",", authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorsCollection",
                new {authorsIds = authorsIdAsString},
                authorCollectionToReturn);
        }
    }
}
