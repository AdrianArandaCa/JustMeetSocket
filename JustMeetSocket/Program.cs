using System.Net.WebSockets;
using System.Text;
using JustMeetSocket.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

Console.Title = "JustMeetWebSocket";
var builder = WebApplication.CreateBuilder();
int usersConnected = 0;
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
builder.WebHost.UseUrls("http://172.16.24.123:45456");
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws/{idUser}", async (int idUser, HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
        {
            //Get user, location, socket and gametype
            User user = repository.GetUser(idUser);
            Location userLocation = repository.GetLocationByUser(user);
            user.Locations = userLocation;
            user.socket = webSocket;
            users.Add(user);
            usersConnected++;
            user.gameTypeToPlay = user.idSettingNavigation.IdGametypeNavigation;

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult rcvResult = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Take(rcvResult.Count).ToArray();
                text = System.Text.Encoding.UTF8.GetString(msgBytes);
                if (text.StartsWith("CHAT"))
                {
                    //Get idUser to chat
                    var idUserToChatting = Int32.Parse(text.Substring(4));
                    var idUserLocal = user.idUser;
                    //Created token
                    string tokenChat = idUserLocal.ToString() + "," + idUserToChatting.ToString();
                    user.token = tokenChat;
                    usersChating.Add(user);
                    string userTokenToConnect = getInversedToken(user);
                    foreach (var userGame in usersChating)
                    {
                        //Send welcome only a user with de invers token
                        if (userGame.token.Equals(userTokenToConnect))
                        {
                            rcvBufferName = Encoding.UTF8.GetBytes("USERCONNECT" + userGame.name);
                            await userGame.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    //Start the chat
                    while (webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult rcvResultChat = await webSocket.ReceiveAsync(rcvBuffer, cts.Token);
                        byte[] msgBytesChat = rcvBuffer.Take(rcvResultChat.Count).ToArray();
                        textChat = System.Text.Encoding.UTF8.GetString(msgBytesChat);
                        //Close the chat
                        if (textChat.Equals("CLOSE"))
                        {
                            if (userExistByWebSocket(webSocket, usersChating))
                            {
                                User userToDesconnect = getUserByWebSocket(webSocket, usersChating);
                                if (userToDesconnect != null)
                                {
                                    string userTokenToDesconnect = getInversedToken(userToDesconnect);
                                    usersChating.Remove(userToDesconnect);
                                    usersConnected--;

                                    foreach (var userGame in usersChating)
                                    {
                                        //Send bye only a user with the invers token
                                        if (userGame.token.Equals(userTokenToDesconnect))
                                        {
                                            rcvBufferName = Encoding.UTF8.GetBytes("USERLEAVE" + userToDesconnect.name);
                                            await userGame.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
                                        }
                                    }
                                    //Close webSocket
                                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando la conexión", CancellationToken.None);
                                }
                            }
                        }
                        //Send message only a user with the invers token
                        User userSend = getUserByWebSocket(webSocket, usersChating);
                        string userTokenToSend = getInversedToken(userSend);
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
                    //If users ready to play is 2, start check the conditions
                    if (usersWaiting.Count == 2)
                    {
                        gamePrepare();
                    }
                }
                if (text.StartsWith("GAMERESULT"))
                {
                    //When the game is finish, return the result of the match
                    gameResult();
                }
                if (text.Equals("CLOSE"))
                {
                    //check if any users have the webSocket
                    if (users.Any(x => x.socket == webSocket))
                    {
                        //Get user
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

//Check conditions
void gamePrepare()
{
    List<Setting> settings = new List<Setting>();
    List<User> userDesconect = new List<User>();
    userDesconect.AddRange(usersWaiting);

    //Separate settings from the users
    foreach (var userGame in usersWaiting)
    {
        settings.Add(userGame.idSettingNavigation);
    }
    
    //Calculate the distance from locations
    var distance = CalculateDistance(usersWaiting[0].Locations.latitud, usersWaiting[0].Locations.longitud, usersWaiting[1].Locations.latitud, usersWaiting[1].Locations.longitud);
    
    //Check max distance
    if (settings[0].maxDistance >= distance && settings[1].maxDistance >= distance) {
        //Check genre
        if (settings[0].genre == usersWaiting[1].genre && settings[1].genre == usersWaiting[0].genre)
        {
            //Check age
            if ((usersWaiting[1].birthday >= settings[0].minAge && usersWaiting[1].birthday <= settings[0].maxAge)
        && (usersWaiting[0].birthday >= settings[1].minAge && usersWaiting[0].birthday <= settings[1].maxAge))
            {
                //Check idGameType
                if (usersWaiting[0].gameTypeToPlay.idGameType == usersWaiting[1].gameTypeToPlay.idGameType)
                {
                    List<User> usersWithMatch = new List<User>();
                    usersWithMatch = repository.GetUsersFromGameWithMatch(usersWaiting[0]);

                    //Check previous match
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
                    closeSocket("CLOSEGAMETYPE", userDesconect);
                }
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
    } else {
        closeSocket("CLOSEDISTANCE", userDesconect);
    }   
}

//Start the game
async void startGame(List<User> usersPlay)
{
    gameType = usersPlay[0].gameTypeToPlay;

    //Get questions from gametype
    questions = repository.GetQuestionsByGameType(gameType);
    string jsonQuestion = JsonSerializer.Serialize(questions);
    Game game = repository.PostGame();
    foreach (User u in usersPlay)
    {
        //Send created game
        string jsonGame = "Game" + JsonSerializer.Serialize(game);
        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    foreach (User u in usersPlay)
    {
        //Send questions with answers
        rcvBufferName = Encoding.UTF8.GetBytes(jsonQuestion);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

//Get game result
void gameResult()
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

//Send game result
async Task matchUsers(List<User> usersGameFinish2, Game game)
{
    Game gameResume = repository.GetGameResume(game);
    foreach (User u in usersGameFinish2)
    {
        //Send put game with result
        string jsonGame = "GAMERESULT" + JsonSerializer.Serialize(gameResume);
        rcvBufferName = Encoding.UTF8.GetBytes(jsonGame);
        await u.socket.SendAsync(rcvBufferName, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

//Close socket and cleaning a lists
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

//Created inverse token
string getInversedToken(User userSend)
{
    var tokenSepared = userSend.token.Split(",");
    int user1 = Convert.ToInt32(tokenSepared[0]);
    int user2 = Convert.ToInt32(tokenSepared[1]);

    int temp = user1;
    user1 = user2;
    user2 = temp;
    return $"{user1},{user2}";
}

//Get if user exists
bool userExistByWebSocket(WebSocket webSocket, List<User> usersToExist)
{
    return usersToExist.Any(x => x.socket == webSocket);
}

//Get user by socket
User getUserByWebSocket(WebSocket webSocket, List<User> usersToGet)
{
    User user = usersToGet.Where(a => a.socket == webSocket).FirstOrDefault();
    return user;
}

//Get distance from two locations
double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
{
    const double earthRadius = 6371;
    var dLat = (lat2 - lat1) * Math.PI / 180.0;
    var dLon = (lon2 - lon1) * Math.PI / 180.0;

    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    var distance = earthRadius * c;

    return distance;
}
