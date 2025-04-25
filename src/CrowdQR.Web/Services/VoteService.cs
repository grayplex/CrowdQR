using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing votes through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the VoteService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class VoteService(ApiService apiService, ILogger<VoteService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<VoteService> _logger = logger;
    private const string BaseEndpoint = "api/vote";

    /// <summary>
    /// Gets all votes for a request.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <returns>A list of votes for the request.</returns>
    public async Task<List<VoteDto>> GetVotesByRequestAsync(int requestId)
    {
        var votes = await _apiService.GetAsync<List<VoteDto>>($"{BaseEndpoint}/request/{requestId}");
        return votes ?? [];
    }

    /// <summary>
    /// Creates a new vote.
    /// </summary>
    /// <param name="voteDto">The vote data.</param>
    /// <returns>The created vote and success flag.</returns>
    public async Task<(bool Success, VoteDto? Vote)> CreateVoteAsync(VoteCreateDto voteDto)
    {
        var (success, response) = await _apiService.PostAsync<VoteCreateDto, VoteDto>(BaseEndpoint, voteDto);
        if (!success)
        {
            _logger.LogError("Failed to create vote");
            return (false, null);
        }
        if (response == null)
        {
            _logger.LogError("Failed to create vote: No response received");
            return (false, null);
        }
        _logger.LogInformation("Vote created successfully: {VoteId}", response.VoteId);
        return (true, response);
    }

    /// <summary>
    /// Deletes a vote by user ID and request ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="requestId">The request ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteVoteByUserAndRequestAsync(int userId, int requestId)
    {
        return await _apiService.DeleteAsync($"{BaseEndpoint}/user/{userId}/request/{requestId}");
    }
}