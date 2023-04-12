namespace JustMeetSocket.Model
{
    public class Question
    {
        public int idQuestion { get; set; }
        public string question { get; set; }
        public GameType gameType { get; set; }
        public List<Answer> answers { get; set; }
    }
}
