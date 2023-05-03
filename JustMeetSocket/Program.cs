using System.Net.WebSockets;
using System.Text;
using JustMeetSocket.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

Console.Title = "JustMeetWebSocket";
var builder = WebApplication.CreateBuilder();
int maxUsersConnected = 2;
int usersConnected = 0;
//int minUsersConnected = 1;
//int usersConnectedMatch = 0;
byte[] rcvBufferName;
var rcvBytes = new byte[256];
Random random = new Random();
var cts = new CancellationTokenSource();
ArraySegment<byte> rcvBuffer = new ArraySegment<byte>(rcvBytes);
string text;
string textChat;
Repository repository = new Repository();
Game game = new Game();
List<Question> questions;
GameType gameType = new GameType();
List<User> users = new List<User>();
List<User> usersMatch = new List<User>();
List<User> usersChating = new List<User>();
List<User> usersWaiting = new List<User>();
var gameresult = 0;
builder.WebHost.UseUrls("http://172.16.24.123:45456");
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws/{idUser}", async (int idUser, HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
        {
            //if (usersConnected < maxUsersConnected)
            //{
            User user = repository.GetUser(idUser);
            user.socket = webSocket;
            users.Add(user);
            usersConnected++;
            if (usersConnected == 1)
            {
                gameType = user.idSettingNavigation.IdGametypeNavigation;
            }
            //}

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult rcvResult = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Take(rcvResult.Count).ToArray();
                text = System.Text.Encoding.UTF8.GetString(msgBytes);
                if (text.StartsWith("CHAT"))
                {
                    var idUserToChatting = Int32.Parse(text.Substring(4));
                    var idUserLocal = user.idUser;
                    string tokenChat = idUserLocal.ToString() + "," + idUserToChatting.ToString();
                    user.token = tokenChat;
                    //User userChat = repository.GetUser(idUserToChatting);
                    usersChating.Add(user);
                    string userTokenToConnect = getToken(user);
                    foreach (var userGame in usersChating)
                    {
                        if (userGame.token.Equals(userTokenToConnect))
                        {
                            rcvBufferName = Encoding.UTF8.GetBytes("USERCONNECT" + userGame.name);
                            await userGame.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }

                    while (webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult rcvResultChat = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                        byte[] msgBytesChat = rcvBuffer.Take(rcvResultChat.Count).ToArray();
                        textChat = System.Text.Encoding.UTF8.GetString(msgBytesChat);
                        if (textChat.Equals("CLOSE"))
                        {
                            if (usersChating.Any(x => x.socket == webSocket))
                            {
                                User userToDesconnect = usersChating.Where(a => a.socket == webSocket).FirstOrDefault();
                                if (userToDesconnect != null)
                                {
                                    string userTokenToDesconnect = getToken(userToDesconnect);
                                    usersChating.Remove(userToDesconnect);
                                    usersConnected--;
                                    
                                    foreach (var userGame in usersChating)
                                    {
                                        if (userGame.token.Equals(userTokenToDesconnect))
                                        {
                                            rcvBufferName = Encoding.UTF8.GetBytes("USERLEAVE" + userToDesconnect.name);
                                            await userGame.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                                        }
                                    }
                                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando la conexión", CancellationToken.None);
                                }
                            }
                        }
                        User userSend = usersChating.Where(a => a.socket == webSocket).FirstOrDefault();
                        string userTokenToSend = getToken(userSend);
                        foreach (var userGame in usersChating)
                        {
                            if (userGame.token.Equals(userTokenToSend))
                            {
                                await userGame.socket.SendAsync(msgBytesChat, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    }
                }

                if (text.StartsWith("STARTGAME"))
                {
                    usersWaiting.Add(user);
                    if (usersWaiting.Count == 2)
                    {
                        List<Setting> settings = new List<Setting>();
                        List<User> userDesconect = new List<User>();
                        userDesconect.AddRange(usersWaiting);

                        foreach (var userGame in usersWaiting)
                        {
                            settings.Add(userGame.idSettingNavigation);
                        }

                        if (settings[0].genre == usersWaiting[1].genre && settings[1].genre == usersWaiting[0].genre)
                        {
                            if ((usersWaiting[1].birthday >= settings[0].minAge && usersWaiting[1].birthday <= settings[0].maxAge)
                        && (usersWaiting[0].birthday >= settings[1].minAge && usersWaiting[0].birthday <= settings[1].maxAge))
                            {
                                List<User> usersWithMatch = new List<User>();
                                usersWithMatch = repository.GetUsersFromGameWithMatch(usersWaiting[0]);
                                //if (!usersWithMatch.Any(x => x.idUser == users[1].idUser))
                                //{
                                Thread playGame = new Thread(new ThreadStart(() => startGame(usersWaiting)));
                                playGame.Start();
                                playGame.Join();
                                usersMatch.AddRange(usersWaiting);
                                usersWaiting.Clear();
                                //}
                                //else
                                //{
                                //    closeSocket("CLOSEMATCH", userDesconect);
                                //}
                            }
                            else
                            {
                                closeSocket("CLOSEAGE", userDesconect);
                            }
                        }
                        else
                        {
                            closeSocket("CLOSEGENRE", userDesconect);
                        }
                    }
                }
                if (text.StartsWith("GAMERESULT"))
                {
                    var textParse = Int32.Parse(text.Substring(10));
                    Game gameFromUser = repository.GetGame(textParse);
                    List<User> usersGameFinish = new List<User>();
                    usersGameFinish.AddRange(usersMatch);

                    Thread matchGame = new Thread(new ThreadStart(() => matchUsers(usersMatch, gameFromUser)));
                    matchGame.Start();
                    matchGame.Join();
                    usersGameFinish.Clear();
                    usersMatch.Clear();
                    usersWaiting.Clear();
                }
                if (text.Equals("CLOSE"))
                {
                    if (users.Any(x => x.socket == webSocket))
                    {
                        User userToDesconnect = users.Where(a => a.socket == webSocket).FirstOrDefault();
                        if (userToDesconnect != null)
                        {
                            users.Remove(userToDesconnect);
                            usersConnected--;
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando la conexión", CancellationToken.None);
                        }
                    }
                }
            }
        }
    }
});

await app.RunAsync();
cts.Dispose();

async void startGame(List<User> usersPlay)
{
    questions = repository.GetQuestionsByGameType(gameType);
    string jsonQuestion = JsonSerializer.Serialize(questions);
    Game game = repository.PostGame();
    foreach (User u in usersPlay)
    {
        string jsonGame = "Game" + JsonSerializer.Serialize(game);
        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    foreach (User u in usersPlay)
    {
        rcvBufferName = Encoding.UTF8.GetBytes(jsonQuestion);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

async Task matchUsers(List<User> usersGameFinish2, Game game)
{
    Game gameResume = repository.GetGameResume(game);
    foreach (User u in usersGameFinish2)
    {
        string jsonGame = "GAMERESULT" + JsonSerializer.Serialize(gameResume);
        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

async void closeSocket(string txt, List<User> userList)
{
    rcvBufferName = Encoding.UTF8.GetBytes(txt);
    foreach (var user in userList)
    {
        await user.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    users.Clear();
    usersWaiting.Clear();
    usersConnected = 0;
}

string getToken(User userSend)
{
    var tokenSepared = userSend.token.Split(",");
    int user1 = Convert.ToInt32(tokenSepared[0]);
    int user2 = Convert.ToInt32(tokenSepared[1]);

    int temp = user1;
    user1 = user2;
    user2 = temp;
    return $"{user1},{user2}";

}
