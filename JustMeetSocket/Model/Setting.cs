namespace JustMeetSocket.Model
{
    public class Setting
    {
        public int idSetting { get; set; }
        public double maxDistance { get; set; }
        public int minAge { get; set; }
        public int maxAge { get; set; }
        public string genre { get; set; }
        public int? idGameType { get; set; }
        public GameType? IdGametypeNavigation { get; set; }

    }
}
