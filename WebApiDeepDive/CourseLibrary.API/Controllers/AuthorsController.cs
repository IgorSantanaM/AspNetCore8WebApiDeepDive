using AutoMapper;
using CourseLibrary.API.ActionsConstraints;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Net.Http.Headers;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IPropertyCheckerService _propertyCheckerService;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper, 
        IPropertyMappingService propertyMappingService,
        ProblemDetailsFactory problemDetailsFactory,
        IPropertyCheckerService propertyCheckerService)
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ??
            throw new ArgumentNullException(nameof(propertyMappingService));
        _problemDetailsFactory = problemDetailsFactory ??
            throw new ArgumentNullException(nameof(problemDetailsFactory));
        _propertyCheckerService = propertyCheckerService;
    }

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    public async Task<ActionResult> GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {

        if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            return BadRequest();

        if (_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                statusCode: 400,
                detail: $"Not all requested  data shaping fields exists on the resource: {authorsResourceParameters.Fields}"));
        }

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameters);

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages
        };

        // self-descriptive constraint - the response body needs to specify hot to execute it
        Response.Headers.Add("X-Pagination",
            JsonSerializer.Serialize(paginationMetadata));

        // create links
        var links = CreateLinksForAuthors(authorsResourceParameters,
            authorsFromRepo.HasNext,
            authorsFromRepo.HasPrevious);

        //shape the auuthors
        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields);


        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object?>;
            var authorLinks = CreateLinkForAuthor((Guid)authorAsDictionary["id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links = links
        };

        // return them
        return Ok(linkedCollectionResource);
    }
    private string? CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        OrderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber - 1,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                new
                {
                    fields = authorsResourceParameters.Fields,
                    OrderBy = authorsResourceParameters.OrderBy,
                    pageNumber = authorsResourceParameters.PageNumber + 1,
                    pageSize = authorsResourceParameters.PageSize,
                    mainCategory = authorsResourceParameters.MainCategory,
                    searchQuery = authorsResourceParameters.SearchQuery
                });
            case ResourceUriType.CurrentPage:   
            default:
                return Url.Link("GetAuthors",
                new
                {
                    fields = authorsResourceParameters.Fields,
                    OrderBy = authorsResourceParameters.OrderBy,
                    pageNumber = authorsResourceParameters.PageNumber,
                    pageSize = authorsResourceParameters.PageSize,
                    mainCategory = authorsResourceParameters.MainCategory,
                    searchQuery = authorsResourceParameters.SearchQuery
                });
        }
    }
    [RequestHeaderMatchesMediaType("Accept",
         "application/json",
         "application/vnd.marvin.author.friendly+json")]
    [Produces("application/json",
         "application/vnd.marvin.author.friendly+json")]
    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<IActionResult> GetAuthorWithoutLinks(Guid authorId,
         string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
                (fields))
        {
            return BadRequest(
              _problemDetailsFactory.CreateProblemDetails(HttpContext,
                  statusCode: 400,
                  detail: $"Not all requested data shaping fields exist on " +
                  $"the resource: {fields}"));
        }

        // get author from repo
        var authorFromRepo = await _courseLibraryRepository
            .GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // friendly author
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields);

        return Ok(friendlyResourceToReturn);
    }
    [RequestHeaderMatchesMediaType("Accept",
        "application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [Produces("application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [HttpGet("{authorId}")]
    public async Task<IActionResult> GetAuthorWithLinks(Guid authorId,
        string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
                (fields))
        {
            return BadRequest(
              _problemDetailsFactory.CreateProblemDetails(HttpContext,
                  statusCode: 400,
                  detail: $"Not all requested data shaping fields exist on " +
                  $"the resource: {fields}"));
        }

        // get author from repo
        var authorFromRepo = await _courseLibraryRepository
            .GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }
        IEnumerable<LinkDto> links = CreateLinkForAuthor(authorId, fields);

        // friendly author
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object?>;

        friendlyResourceToReturn.Add("links", links);

        return Ok(friendlyResourceToReturn);
    }
    [RequestHeaderMatchesMediaType("Accept",
        "application/vnd.marvin.author.full+json")]
    [Produces("application/vnd.marvin.author.full+json")]
    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<IActionResult> GetFullAuthorWithoutLinks(Guid authorId,
        string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
                (fields))
        {
            return BadRequest(
              _problemDetailsFactory.CreateProblemDetails(HttpContext,
                  statusCode: 400,
                  detail: $"Not all requested data shaping fields exist on " +
                  $"the resource: {fields}"));
        }

        // get author from repo
        var authorFromRepo = await _courseLibraryRepository
            .GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
            .ShapeData(fields);

        return Ok(fullResourceToReturn);
    }
    [RequestHeaderMatchesMediaType("Accept",
        "application/vnd.marvin.author.full.hateoas+json")]
    [Produces("application/vnd.marvin.author.full.hateoas+json")]
    [HttpGet("{authorId}")]
    public async Task<IActionResult> GetFullAuthorWithLinks(Guid authorId,
        string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
                (fields))
        {
            return BadRequest(
              _problemDetailsFactory.CreateProblemDetails(HttpContext,
                  statusCode: 400,
                  detail: $"Not all requested data shaping fields exist on " +
                  $"the resource: {fields}"));
        }

        // get author from repo
        var authorFromRepo = await _courseLibraryRepository
            .GetAuthorAsync(authorId);
            
        if (authorFromRepo == null)
        {
            return NotFound();
        }

        IEnumerable<LinkDto> links = CreateLinkForAuthor(authorId, fields);

        var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object?>;

        fullResourceToReturn.Add("links", links);
        return Ok(fullResourceToReturn);
    }

    //[Produces("application/json", 
    //    "application/vnd.marvin.hateoas+json",
    //    "application/vnd.marvin.author.full+json", 
    //    "application/vnd.marvin.author.full.hateoas+json",
    //    "application/vnd.marvin.author.friendly+json", 
    //    "application/vnd.marvin.author.friendly.hateoas+json")]
    //[HttpGet("{authorId}", Name = "GetAuthor")]
    //public async Task<IActionResult> GetAuthor(Guid authorId,
    //    string? fields,
    //    [FromHeader(Name = "Accept")] string? mediaType)
    //{
    //    // check if the inputted media type is a valid media type
    //    if (!MediaTypeHeaderValue.TryParse(mediaType,
    //               out var parsedMediaType))
    //    {
    //        return BadRequest(
    //        _problemDetailsFactory.CreateProblemDetails(HttpContext,
    //            statusCode: 400,
    //            detail: $"Accept header media type value is not a valid media type."));
    //    }

    //    if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
    //       (fields))
    //    {
    //        return BadRequest(
    //          _problemDetailsFactory.CreateProblemDetails(HttpContext,
    //              statusCode: 400,
    //              detail: $"Not all requested data shaping fields exist on " +
    //              $"the resource: {fields}"));
    //    }

    //    // get author from repo
    //    var authorFromRepo = await _courseLibraryRepository
    //        .GetAuthorAsync(authorId);

    //    if (authorFromRepo == null)
    //    {
    //        return NotFound();
    //    }

    //    var includeLinks = parsedMediaType.SubTypeWithoutSuffix
    //            .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);
    //    IEnumerable<LinkDto> links = new List<LinkDto>();

    //    if (includeLinks)
    //    {
    //        links = CreateLinksForAuthor(authorId, fields);
    //    }

    //    var primaryMediaType = includeLinks ?
    //            parsedMediaType.SubTypeWithoutSuffix.Substring(
    //                0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
    //            : parsedMediaType.SubTypeWithoutSuffix;

    //    // full author
    //    if (primaryMediaType == "vnd.marvin.author.full")
    //    {
    //        var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
    //            .ShapeData(fields) as IDictionary<string, object?>;

    //        if (includeLinks)
    //        {
    //            fullResourceToReturn.Add("links", links);
    //        }

    //        return Ok(fullResourceToReturn);
    //    }

    //    // friendly author
    //    var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
    //        .ShapeData(fields) as IDictionary<string, object?>;

    //    if (includeLinks)
    //    {
    //        friendlyResourceToReturn.Add("links", links);
    //    }

    //    return Ok(friendlyResourceToReturn);
    //}

    #region HATEOAS
    private IEnumerable<LinkDto> CreateLinkForAuthor(Guid authorId, string? fields)
    {

        var links = new List<LinkDto>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(new(Url.Link("GetAuthor", new { authorId }),
                "self",
                "GET"));
        }
        else
        {
            links.Add(new(Url.Link("GetAuthor", new { authorId }),
                "self",
                "GET"));
        }

        links.Add(
            new(Url.Link("CreateCourseForAuthor", new { authorId }),
            "create_course_for_author",
            "POST"));
        links.Add(
            new(Url.Link("GetCoursesForAuthor", new { authorId }),
            "courses",
            "GET"));

        return links;
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, bool hasPrevious, bool hasNext)
    {
        var links = new List<LinkDto>();

        // self
        links.Add(
            new(CreateAuthorsResourceUri(authorsResourceParameters,
            ResourceUriType.CurrentPage),
            "self", "GET"));
        if (hasNext)
        {
            links.Add(new(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                "nextPage",
                "GET"));
        }
        if (hasPrevious)
        {
            links.Add(new(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                "previousPage",
                "GET"));
        }

        return links;
    }
    #endregion

    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [RequestHeaderMatchesMediaType("Content-Type",
        "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        // create links
        var links = CreateLinkForAuthor(authorToReturn.Id, null);

        // add 
        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object?>;
        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            linkedResourceToReturn);
    }

    [HttpPost (Name = "CreateAuthor")]
    [RequestHeaderMatchesMediaType("Content-Type", 
        "application/json", 
        "application/vnd.marvin.authorforcreation+json")]
    [Consumes("application/json", "application/vnd.marvin.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        // create links
        var links = CreateLinkForAuthor(authorToReturn.Id, null);

        // add 
        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object?>;
        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            linkedResourceToReturn);
    }
    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET,HEAD,POST,OPTIONS");
        return Ok();
    }
}