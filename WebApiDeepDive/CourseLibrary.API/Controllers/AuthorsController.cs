﻿using AutoMapper;
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
    [Produces("application/json",
               "appliaction/vnd.marvin.hateoas+json",
               "appliaction/vnd.marvin.author.full+json",
               "appliaction/vnd.marvin.author.full.hateoas+json",
               "appliaction/vnd.marvin.author.friendly+json",
               "appliaction/vnd.marvin.author.friendly.hateoas+json")]
    // name = localtion we could say 
    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult> GetAuthor(Guid authorId, string? fields, [FromHeader(Name = "Accept")] string? mediaType) 
    {

        if(!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400,
                detail: $"Accept header media type value is not a valid media type. {mediaType}"));
        }
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                statusCode: 400,
                detail: $"Not all requested  data shaping fields exists on the resource: {fields}"));
        }

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);
        IEnumerable<LinkDto> links = new List<LinkDto>();

        if (includeLinks)
        {
            links = CreateLinkForAuthor(authorId, fields);
        }

        var primaryMediaType = includeLinks ?
            parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
                : parsedMediaType.SubTypeWithoutSuffix; 

        if(primaryMediaType == "vnd.marvin.author.full")
        {
            var friendlyResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
                .ShapeData(fields) as IDictionary<string, object?>;

            if (includeLinks)
            {
                friendlyResourceToReturn.Add("links", links);
            }
            return Ok(friendlyResourceToReturn);
        }
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

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