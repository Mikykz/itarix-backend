using Itarix.Api.Models;
using itarixapi.Business;
using itarixapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AIToolsController : ControllerBase
{
    private readonly IAIToolService _service;
    public AIToolsController(IAIToolService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tools = await _service.GetAllAsync();
        return Ok(tools);
    }

    [HttpGet("{toolId}")]
    public async Task<IActionResult> GetById(int toolId)
    {
        var tool = await _service.GetByIdAsync(toolId);
        return tool != null ? Ok(tool) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> Create([FromBody] AIToolCreateDto dto)
    {
        // Now expects dto.CategoryId, not Category
        var result = await _service.CreateAsync(dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpPut("{toolId}")]
    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> Edit(int toolId, [FromBody] AIToolCreateDto dto)
    {
        var result = await _service.EditAsync(toolId, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpDelete("{toolId}")]
    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> Delete(int toolId)
    {
        var result = await _service.DeleteAsync(toolId);
        return result.Success ? Ok() : BadRequest(result.Message);
    }
}
