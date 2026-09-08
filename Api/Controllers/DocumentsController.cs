using Microsoft.AspNetCore.Mvc;
using SearchAPI.Application;

namespace SearchAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents) => _documents = documents;

    /// <summary>Returns the raw text of an indexed document.</summary>
    [HttpGet("{id:int}")]
    public ActionResult GetContent(int id)
    {
        var text = _documents.GetContent(id);
        return text is null
            ? NotFound()
            : Content(text, "text/plain; charset=utf-8");
    }
}
