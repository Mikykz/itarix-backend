using Itarix.Api.Models;
using itarixapi.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin,moderator")]
public class AdminController : ControllerBase
{
    private readonly IModerationService _service;
    public AdminController(IModerationService service) { _service = service; }

    [HttpGet("reviews/pending")]
    public async Task<IActionResult> GetPendingReviews()
    {
        var list = await _service.GetPendingReviewsAsync();
        return Ok(list);
    }

    [HttpPut("reviews/approve/{reviewId}")]
    public async Task<IActionResult> ApproveReview(int reviewId)
    {
        var result = await _service.ApproveReviewAsync(reviewId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpDelete("reviews/{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var result = await _service.DeleteReviewAsync(reviewId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    // Add more endpoints for comment moderation if needed
}
