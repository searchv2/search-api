using Microsoft.AspNetCore.Mvc;
using Shared.Model;
using SearchAPI.Api.Mapping;
using SearchAPI.Application;

namespace SearchAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search) => _search = search;

    [HttpPost]
    public ActionResult<SearchResult> Post([FromBody] SearchRequest request)
    {
        var outcome = _search.Search(request.Query, request.CaseSensitive);
        return Ok(outcome.ToContract());
    }
}
