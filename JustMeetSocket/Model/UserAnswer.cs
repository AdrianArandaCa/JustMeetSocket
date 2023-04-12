namespace JustMeetSocket.Model
{
    public class UserAnswer
    {
        public Game game { get; set; }
        public User user { get; set; }
        public Question question { get; set; }
        public Answer answer { get; set; }
    }
}
