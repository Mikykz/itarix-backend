using Itarix.Api.Models;
using itarixapi.Business;
using itarixapi.Models;
using itarixapi.Data; // For IToolReviewRepository
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ToolReviewsController : ControllerBase
{
    private readonly IToolReviewService _service;
    private readonly IToolReviewRepository _repository; // <-- Use your repository for DB access

    public ToolReviewsController(IToolReviewService service, IToolReviewRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpGet("tool/{toolId}")]
    public async Task<IActionResult> GetReviewsByTool(int toolId)
    {
        var reviews = await _service.GetReviewsByToolAsync(toolId);
        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] ToolReviewCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.CreateReviewAsync(dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpPut("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> EditReview(int reviewId, [FromBody] ToolReviewCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.EditReviewAsync(reviewId, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpDelete("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var userId = GetUserId();
        var result = await _service.DeleteReviewAsync(reviewId, userId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    // GET: /api/ToolReviews/tool/{toolId}/average
    [HttpGet("tool/{toolId}/average")]
    public async Task<IActionResult> GetAverage(int toolId)
    {
        var (average, count) = await _repository.GetAverageRatingAsync(toolId);
        return Ok(new { average, count });
    }

    [HttpPost("report")]
    [Authorize]
    public async Task<IActionResult> ReportReview([FromBody] ReviewReportCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.ReportReviewAsync(dto, userId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue("userId"));
    }
}
