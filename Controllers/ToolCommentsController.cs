using Itarix.Api.Models;
using itarixapi.Business;
using itarixapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ToolCommentsController : ControllerBase
{
    private readonly IToolCommentService _service;
    public ToolCommentsController(IToolCommentService service) { _service = service; }

    [HttpGet("tool/{toolId}")]
    public async Task<IActionResult> GetCommentsByTool(int toolId)
    {
        var comments = await _service.GetCommentsByToolAsync(toolId);
        return Ok(comments);
    }

    [HttpGet("tool/{toolId}/count")]
    public async Task<IActionResult> GetCommentsCountByTool(int toolId)
    {
        var count = await _service.GetCommentsCountByToolAsync(toolId);
        return Ok(new { count });
    }


    [HttpGet("review/{reviewId}")]
    public async Task<IActionResult> GetCommentsByReview(int reviewId)
    {
        var comments = await _service.GetCommentsByReviewAsync(reviewId);
        return Ok(comments);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateComment([FromBody] ToolCommentCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.CreateCommentAsync(dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpPut("{commentId}")]
    [Authorize]
    public async Task<IActionResult> EditComment(int commentId, [FromBody] ToolCommentCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.EditCommentAsync(commentId, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpDelete("{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = GetUserId();
        var result = await _service.DeleteCommentAsync(commentId, userId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("report")]
    [Authorize]
    public async Task<IActionResult> ReportComment([FromBody] CommentReportCreateDto dto)
    {
        var userId = GetUserId();
        var result = await _service.ReportCommentAsync(dto, userId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue("userId"));
    }
}
