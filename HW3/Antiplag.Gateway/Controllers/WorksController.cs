using System.Net.Http.Json;
using Antiplag.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace Antiplag.Gateway.Controllers;

[ApiController]
[Route("api/v1/works")]
[Tags("Works")]
public class WorksController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorksController> _logger;

    public WorksController(
        IHttpClientFactory httpClientFactory,
        ILogger<WorksController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает список отчётов по всем сдачам указанной работы.
    /// Используется преподавателем для просмотра результатов проверки.
    /// </summary>
    [HttpGet("{workId}/reports")]
    public async Task<ActionResult<WorkReportsSummary>> GetReports(string workId)
    {
        if (string.IsNullOrWhiteSpace(workId))
            return BadRequest("workId is required");

        var client = _httpClientFactory.CreateClient("analysis");

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"/internal/analysis/works/{workId}/reports");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Analysis for work {WorkId}", workId);
            return StatusCode(503, "Analysis service unavailable");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Analysis returned {Status} for work {WorkId}",
                response.StatusCode, workId);

            return StatusCode((int)response.StatusCode, "Could not get reports");
        }

        var summary = await response.Content.ReadFromJsonAsync<WorkReportsSummary>();
        if (summary == null)
        {
            return StatusCode(500, "Invalid response from Analysis");
        }

        return Ok(summary);
    }
}