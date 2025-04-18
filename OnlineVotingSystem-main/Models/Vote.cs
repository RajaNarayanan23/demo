namespace OnlineVotingSystem.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CandidateId { get; set; }
        public int ElectionId { get; set; }
        public DateTime Timestamp { get; set; } // ✅ Add this field
    }
}
