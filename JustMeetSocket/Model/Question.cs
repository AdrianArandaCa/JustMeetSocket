namespace JustMeetSocket.Model
{
    public class Question
    {
        public int idQuestion { get; set; }
        public string question1 { get; set; }
        public int? idGameType { get; set; }
        public GameType gameType { get; set; }
        public List<Answer> answers { get; set; }
    }
}
