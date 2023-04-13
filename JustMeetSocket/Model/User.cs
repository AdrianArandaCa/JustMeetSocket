using System.Net.WebSockets;

namespace JustMeetSocket.Model
{
    public class User
    {
        public int idUser { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string birthday { get; set; }
        public string genre { get; set; }
        public string photo { get; set; }
        public string description { get; set; }
        public bool premium { get; set; }
        public int? idSetting { get; set; }
        public WebSocket socket { get; set; }
        public Setting IdSettingNavigation { get; set; }
        public Location locations { get; set; }


    }



}
