namespace JustMeetSocket.Model
{
    public class Game
    {
        public int? idGame { get; set; }
        public string registrationDate { get; set; }
        public bool match { get; set; }
        //public List<User> users { get; set; }
        //public List<Question> questions { get; set; }

        public Game(int? idGame, string registrationDate, bool match)
        {
            this.idGame = idGame;
            this.registrationDate = registrationDate;
            this.match = match;
        }
    }
}
