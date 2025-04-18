using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using OnlineVotingSystem.Models;

[Route("api/vote")]
[ApiController]
public class VoteController : ControllerBase
{
    private readonly ApplicationDbContext _context; // ✅ Use correct DbContext

    public VoteController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CastVote([FromBody] VoteRequest voteRequest)
    {
        if (voteRequest == null || voteRequest.UserId == 0 || voteRequest.CandidateId == 0)
        {
            return BadRequest(new { message = "Invalid vote request!" });
        }

        // Check if user has already voted in the same election
        var existingVote = await _context.Votes.FirstOrDefaultAsync(v => v.UserId == voteRequest.UserId && v.ElectionId == voteRequest.ElectionId);
        if (existingVote != null)
        {
            return BadRequest(new { message = "User has already voted in this election!" });
        }

        var vote = new Vote
        {
            UserId = voteRequest.UserId,
            CandidateId = voteRequest.CandidateId,
            ElectionId = voteRequest.ElectionId,
            Timestamp = DateTime.UtcNow // ✅ Ensure this exists in your Vote model
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vote casted successfully!" });
    }
}
