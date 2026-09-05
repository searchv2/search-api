using Microsoft.AspNetCore.Mvc;
using Shared.Model;

namespace SearchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly SearchLogic mSearchLogic;

        public SearchController(IDatabase database)
        {
            mSearchLogic = new SearchLogic(database);
        }

        [HttpPost]
        public ActionResult<SearchResult> Post([FromBody] SearchRequest request)
        {
            var result = mSearchLogic.Search(request.Query, request.CaseSensitive);
            return Ok(result);
        }
    }
}
