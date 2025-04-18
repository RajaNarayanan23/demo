namespace OnlineVotingSystem.Models
{
    public class VoteRequest
    {
        public int UserId { get; set; }
        public int CandidateId { get; set; }
        public int ElectionId { get; set; }
    }
}
