using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search) => _search = search;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q)
        => Ok(await _search.SearchAsync(User.GetUserId(), q));
}
