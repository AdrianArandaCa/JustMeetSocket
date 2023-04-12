using System.Net.WebSockets;
using System.Net;
using System.Text;
using JustMeetSocket.Model;
using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

Console.Title = "JustMeetWebSocket";
var builder = WebApplication.CreateBuilder();
int maxUsersConnected = 1;
int minUsersConnected = 1;
int usersConnected = 0;

Repository repository = new Repository();
List<Question> questions;
GameType gameType = new GameType();
List<User> users = new List<User>(); 
builder.WebHost.UseUrls("http://localhost:6666");
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
                    gameType = user.setting.gameType;
                }
            }

            byte[] rcvBufferName;
            var rcvBytes = new byte[256];
            var cts = new CancellationTokenSource();
            ArraySegment<byte> rcvBuffer = new ArraySegment<byte>(rcvBytes);
            string text;

            if (usersConnected == maxUsersConnected)
            {
                questions = repository.GetQuestionsByGameType(gameType);
                string jsonQuestion = JsonSerializer.Serialize(questions);
                foreach (User u in users) 
                {
                    rcvBufferName = Encoding.UTF8.GetBytes(jsonQuestion);
                    await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);

                    //foreach (Question q in questions)
                    //{
                    //    rcvBufferName = Encoding.UTF8.GetBytes("idQuestion"+q.idQuestion+" "+q.question);
                    //    await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                    //    foreach (Answer a in q.answers)
                    //    {
                    //        rcvBufferName = Encoding.UTF8.GetBytes("idAnswer" + a.idAnswer + " " + a.asnwer);
                    //        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                    //    }

                    //}
                }
            }

            
            
            while (true)
            {
                WebSocketReceiveResult rcvResult = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Take(rcvResult.Count).ToArray();
                text = System.Text.Encoding.UTF8.GetString(msgBytes);
            }
        }
    }
});

await app.RunAsync();
