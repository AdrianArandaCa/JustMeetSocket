using System.Net.WebSockets;
using System.Text;
using JustMeetSocket.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Globalization;

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
Repository repository = new Repository();
Game game = new Game();
List<Question> questions;
GameType gameType = new GameType();
List<User> users = new List<User>();
List<User> usersMatch = new List<User>();
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
            //gametype, minAge, maxAge, hombre o mujer y si han sido match, no pueden jugar.
            if (usersConnected < maxUsersConnected)
            {
                User user = repository.GetUser(idUser);
                //List<User> usersproba = repository.GetUsers();
                user.socket = webSocket;
                users.Add(user);
                usersConnected++;
                if (usersConnected == 1)
                {
                    gameType = user.idSettingNavigation.IdGametypeNavigation;
                }
            }

            if (usersConnected == maxUsersConnected)
            {
                List<float> ages = new List<float>();
                List<Setting> settings = new List<Setting>();
                List<User> userDesconect = new List<User>();
                userDesconect.AddRange(users);

                foreach (var user in users)
                {
                    settings.Add(user.idSettingNavigation);
                }

                if (settings[0].genre == users[1].genre && settings[1].genre == users[0].genre)
                {
                    if ((settings[0].minAge <= users[1].birthday && settings[0].maxAge >= users[1].birthday)
                    && (settings[1].minAge <= users[0].birthday && settings[1].maxAge >= users[0].birthday))
                    {
                        List<User> usersWithMatch = new List<User>();
                        usersWithMatch = repository.GetUsersFromGameWithMatch(users[0]);
                        if (!usersWithMatch.Any(x=>x.idUser == users[1].idUser))
                        {
                            Thread playGame = new Thread(new ThreadStart(() => startGame(users)));
                            playGame.Start();
                            playGame.Join();
                            usersMatch.AddRange(users);
                            users.Clear();
                            usersConnected = 0;
                        }
                        else
                        {
                            closeSocket("CLOSEMATCH", userDesconect);
                            users.Clear();
                            usersConnected = 0;
                        }
                    }
                    else
                    {
                        closeSocket("CLOSEAGE", userDesconect);
                        users.Clear();
                        usersConnected = 0;
                    }
                }
                else
                {
                    closeSocket("CLOSEGENRE", userDesconect);
                    users.Clear();
                    usersConnected = 0;
                }
            }

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult rcvResult = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Take(rcvResult.Count).ToArray();
                text = System.Text.Encoding.UTF8.GetString(msgBytes);

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
                }
                if (text.Equals("CLOSE"))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando la conexión", CancellationToken.None);
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

}
