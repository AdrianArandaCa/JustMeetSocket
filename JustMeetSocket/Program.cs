using System.Net.WebSockets;
using System.Net;
using System.Text;
using JustMeetSocket.Model;
using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Runtime.CompilerServices;

Console.Title = "JustMeetWebSocket";
var builder = WebApplication.CreateBuilder();
int maxUsersConnected = 1;
int minUsersConnected = 1;
int usersConnected = 0;
int usersConnectedMatch = 0;
byte[] rcvBufferName;
var rcvBytes = new byte[256];
var cts = new CancellationTokenSource();
ArraySegment<byte> rcvBuffer = new ArraySegment<byte>(rcvBytes);
string text;
Repository repository = new Repository();
Game game = new Game();
List<Question> questions;
GameType gameType = new GameType();
List<User> users = new List<User>();
List<User> usersMatch = new List<User>();
builder.WebHost.UseUrls("http://172.16.24.123:45456");
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws/{idUser}", async (int idUser, HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
        {
            if (usersConnected < maxUsersConnected)
            {
                User user = repository.GetUser(idUser);
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
                Thread playGame = new Thread(new ThreadStart(() => startGame(users)));
                playGame.Start();
                playGame.Join();
                //users.RemoveRange(0, maxUsersConnected);
                //usersConnected = 0;
            }

            while (true)
            {
                WebSocketReceiveResult rcvResult = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Take(rcvResult.Count).ToArray();
                text = System.Text.Encoding.UTF8.GetString(msgBytes);
                if (text.StartsWith("GAMERESULT"))
                {
                    var textParse = Int32.Parse(text.Substring(10));
                    game = repository.GetGame(textParse);
                    usersConnectedMatch++;
                    Game gameResume = repository.GetGameResume(game);
                    foreach (User u in users)
                    {
                        string jsonGame = "GAMERESULT" + JsonSerializer.Serialize(gameResume);
                        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
                        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    //Thread matchGame = new Thread(new ThreadStart(() => matchUsers(users, game)));
                    //matchGame.Start();
                    //matchGame.Join();

                }
            }
        }
    }
});

await app.RunAsync();

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

async void matchUsers(List<User> userMatch, Game game)
{
    Game gameResume = repository.GetGameResume(game);
    foreach (User u in userMatch)
    {
        string jsonGame = "GAMERESULT" + JsonSerializer.Serialize(gameResume);
        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
