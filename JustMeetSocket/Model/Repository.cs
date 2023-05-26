using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JustMeetSocket.Model
{
    public class Repository
    {
        //string ws = "https://172.16.24.123:45455/api/"; //DISCO SDD
        //string ws = "https://172.16.24.24:45455/api/"; //DISCO HDD
        string ws = "https://172.16.24.24:45455/api/";
        Random random = new Random();
        int totalQuestions = 5;
        double minMatch = 60;

        //User
        public User GetUser(int id)
        {
            try
            {
                User user = null;
                user = (User)MakeRequest(string.Concat(ws, "user/", id), null, "GET", "application/json", typeof(User));
                return user;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public List<User> GetUsersFromGameWithMatch(User user)
        {
            try
            {
                List<User> users = null;
                users = (List<User>)MakeRequest(string.Concat(ws, "userGameList/", user.idUser), null, "GET", "application/json", typeof(List<User>));
                return users;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public List<User> GetUsersFromGame(Game game)
        {
            try
            {
                List<User> users = null;
                users = (List<User>)MakeRequest(string.Concat(ws, "usersFromGame/", game.idGame), null, "GET", "application/json", typeof(List<User>));
                return users;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public Location GetLocationByUser(User user)
        {
            try
            {
                Location location = null;
                location = (Location)MakeRequest(string.Concat(ws, "locationByUser/", user.idUser), null, "GET", "application/json", typeof(Location));
                return location;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        //Question
        public List<Question> GetQuestionsByGameType(GameType gameType)
        {
            try
            {
                List<Question> questions = null;
                questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
                questions = questions.Where(a => a.idGameType == gameType.idGameType).OrderBy(a => random.Next()).Take(totalQuestions).ToList();
                //for (var i = 0; i < questions.Count; i++)
                //{
                //    questions[i].answers = GetAnswersFromQuestion(questions[i].idQuestion);
                //}
                return questions;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public List<Answer> GetAnswersFromQuestion(int id)
        {
            try
            {
                List<Answer> answers = null;
                answers = (List<Answer>)MakeRequest(string.Concat(ws, "questionWithAnswer/", id), null, "GET", "application/json", typeof(List<Answer>));
                return answers;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        //Game
        public Game GetGame(int id)
        {
            try
            {
                Game game = null;
                game = (Game)MakeRequest(string.Concat(ws, "game/", id), null, "GET", "application/json", typeof(Game));
                game.registrationDate = game.registrationDate.Substring(0, 10);
                return game;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }
        public Game PostGame()
        {
            try
            {
                string date = DateTime.Today.ToString("yyyy-MM-dd");
                Game game = new Game(0, date, false, 0);
                Game gamePost = (Game)MakeRequest(string.Concat(ws, "game"),
                    game, "POST", "application/json", typeof(Game));
                return gamePost;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public Game PutGame(Game game)
        {
            try
            {
                Game gamePut = (Game)MakeRequest(string.Concat(ws, "game/", game.idGame),
                                game, "PUT", "application/json", typeof(Game));
                return gamePut;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public Game GetGameResume(Game game)
        {
            try
            {
                double result = 0;
                List<User> users = GetUsersFromGame(game);
                List<UserAnswer> listUserAnswer = UserAnswerFromGame((int)game.idGame);
                List<int?> user2Answers = new List<int?>();
                var totalQuestionsResult = listUserAnswer.Count / 2;
                List<int?> user1Answers = listUserAnswer.Where(x => x.idUser == users[0].idUser).Select(x => x.idAnswer).ToList();
                if (users.Count > 1)
                {
                    user2Answers = listUserAnswer.Where(x => x.idUser == users[1].idUser).Select(x => x.idAnswer).ToList();
                }

                List<int?> listEqualAnswers = new List<int?>();
                for (var i = 0; i < user1Answers.Count; i++)
                {
                    if (user1Answers[i] == user2Answers[i] && (user1Answers[i] != null || user2Answers[i] != null))
                    {
                        listEqualAnswers.Add(user1Answers[i]);
                    }
                }
                var count = listEqualAnswers.Count;
                if (totalQuestionsResult != 0)
                {
                    result = ((Convert.ToSingle(count) / Convert.ToSingle(totalQuestionsResult)) * 100);
                }
                else
                {
                    result = 0.0;
                }
                double roundedValue = Math.Round(result, 2);
                game.percentage = roundedValue;
                // Mirar que haga bien el match
                if (result >= minMatch)
                {
                    game.match = true;
                }
                else
                {
                    game.match = false;
                }
                PutGame(game);
                Game newGame = GetGame((int)game.idGame);

                return newGame;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }
        public List<UserAnswer> UserAnswerFromGame(int idGame)
        {
            try
            {
                List<UserAnswer> listUserAnswer = null;
                listUserAnswer = (List<UserAnswer>)MakeRequest(string.Concat(ws, "userAnswerFromGame/", idGame), null, "GET", "application/json", typeof(List<UserAnswer>));
                return listUserAnswer;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public static object MakeRequest(string requestUrl, object JSONRequest, string JSONmethod, string JSONContentType, Type JSONResponseType)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                string sb = JsonConvert.SerializeObject(JSONRequest);
                request.Method = JSONmethod;

                if (JSONmethod != "GET")
                {
                    request.ContentType = JSONContentType;
                    Byte[] bt = Encoding.UTF8.GetBytes(sb);
                    Stream st = request.GetRequestStream();
                    st.Write(bt, 0, bt.Length);
                    st.Close();
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).", response.StatusCode, response.StatusDescription));

                    Stream stream1 = response.GetResponseStream();
                    StreamReader sr = new StreamReader(stream1);
                    string strsb = sr.ReadToEnd();
                    object objResponse = JsonConvert.DeserializeObject(strsb, JSONResponseType);
                    return objResponse;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        //Discard

        //public User PostUser(User user)
        //{
        //    User userPost = (User)MakeRequest(string.Concat(ws, "user/"),
        //        user, "POST", "application/json", typeof(User));
        //    return userPost;
        //}

        //public Setting GetSetting(int id)
        //{
        //    Setting setting = null;
        //    setting = (Setting)MakeRequest(string.Concat(ws, "setting/", id), null, "GET", "application/json", typeof(Setting));
        //    //setting.gameType = GetGameTypeFromSetting((int)setting.idGameType);
        //    return setting;
        //}

        //public List<Question> GetQuestions()
        //{
        //    List<Question> questions = null;
        //    questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
        //    for (var i = 0; i < questions.Count; i++)
        //    {
        //        questions[i].answers = GetAnswersFromQuestion(questions[i].idQuestion);
        //    }
        //    return questions;
        //}

        //public GameType GetGameTypeFromSetting(int id)
        //{
        //    GameType gameType = null;
        //    gameType = (GameType)MakeRequest(string.Concat(ws, "gameType/", id), null, "GET", "application/json", typeof(GameType));
        //    return gameType;
        //}

        //public List<User> GetUsers() 
        //{
        //    List<User> users = null;
        //    users = (List<User>)MakeRequest(string.Concat(ws, "users"), null, "GET", "application/json", typeof(List<User>));
        //    return users;
        //}
    }
}
