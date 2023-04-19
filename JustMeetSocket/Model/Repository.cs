﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JustMeetSocket.Model
{
    public class Repository
    {
        string ws = "https://172.16.24.123:45455/api/";
        Random random = new Random();
        int totalQuestions;
        List<int?> user1Answers = new List<int?>();
        List<int?> user2Answers = new List<int?>();

        public List<User> GetUsers() 
        {
            List<User> users = null;
            users = (List<User>)MakeRequest(string.Concat(ws, "users"), null, "GET", "application/json", typeof(List<User>));
            return users;
        }

        public User GetUser(int id)
        {
            User user = null;
            user = (User)MakeRequest(string.Concat(ws, "user/",id), null, "GET", "application/json", typeof(User));
            return user;
        }

        public User PostUser(User user)
        {
            User userPost = (User)MakeRequest(string.Concat(ws, "user/"),
                user, "POST", "application/json", typeof(User));
            return userPost;
        }
        public List<Question> GetQuestions()
        {
            List<Question> questions = null;
            questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
            for (var i = 0; i < questions.Count; i++)
            {
                questions[i].answers = GetAnswersFromQuestion(questions[i].idQuestion);
            }
            return questions;
        }

        public List<Question> GetQuestionsByGameType(GameType gameType)
        {
            List<Question> questions = null;
            questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
            questions = questions.Where(a => a.idGameType == gameType.idGameType).OrderBy(a => random.Next()).Take(2).ToList();
            for (var i = 0; i < questions.Count; i++)
            {
                questions[i].answers = GetAnswersFromQuestion(questions[i].idQuestion);
            }
            return questions;
        }

        public List<Answer> GetAnswersFromQuestion(int id) 
        {
            List<Answer> answers = null;
            answers = (List<Answer>)MakeRequest(string.Concat(ws, "questionWithAnswer/", id), null, "GET", "application/json", typeof(List<Answer>));
            return answers;
        }

        public Setting GetSetting(int id) 
        {
            Setting setting = null;
            setting = (Setting)MakeRequest(string.Concat(ws, "setting/", id), null, "GET", "application/json", typeof(Setting));
            //setting.gameType = GetGameTypeFromSetting((int)setting.idGameType);
            return setting;
        }

        public GameType GetGameTypeFromSetting(int id)
        {
            GameType gameType = null;
            gameType = (GameType)MakeRequest(string.Concat(ws, "gameType/", id), null, "GET", "application/json", typeof(GameType));
            return gameType;
        }
        public Game GetGame(int id)
        {
            Game game = null;
            game = (Game)MakeRequest(string.Concat(ws, "game/", id), null, "GET", "application/json", typeof(Game));
            //setting.gameType = GetGameTypeFromSetting((int)setting.idGameType);
            return game;
        }
        public Game PostGame()
        {
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            Game game = new Game(0, date, false, 0);
            Game gamePost = (Game)MakeRequest(string.Concat(ws, "game"),
                game, "POST", "application/json", typeof(Game));
            return gamePost;
        }

        public List<UserAnswer> UserAnswerFromGame (int idGame) {
            List<UserAnswer> listUserAnswer = null;
            listUserAnswer = (List<UserAnswer>)MakeRequest(string.Concat(ws, "userAnswerFromGame/", idGame), null, "GET", "application/json", typeof(List<UserAnswer>));
            return listUserAnswer;
        }

        public List<User> GetUsersFromGame(Game game) 
        {
            List<User> users = null;
            users = (List<User>)MakeRequest(string.Concat(ws, "usersFromGame/", game.idGame), null, "GET", "application/json", typeof(List<User>));
            return users;
        }

        public Game PutGame(Game game) 
        {
            Game gamePut= (Game)MakeRequest(string.Concat(ws, "game/", game.idGame),
                game, "PUT", "application/json", typeof(Game));
            return gamePut;
        }

        public Game GetGameResume(Game game)
        {
            List<User> users = GetUsersFromGame(game);
            List<UserAnswer> listUserAnswer = UserAnswerFromGame((int)game.idGame);
            totalQuestions = listUserAnswer.Count / 2;
            user1Answers = listUserAnswer.Where(x => x.idUser == users[0].idUser).Select(x => x.idAnswer).ToList();
            user2Answers = listUserAnswer.Where(x => x.idUser == users[1].idUser).Select(x => x.idAnswer).ToList();
            List<int?> listEqualAnswers = user1Answers.Intersect(user2Answers).ToList();
            float result = (listEqualAnswers.Count / totalQuestions) * 100;
            game.percentage = result;
            if (result >= ((double)totalQuestions / 2))
            {
                game.match = true;
            }
            else 
            {
                game.match = false;
            }
            return PutGame(game);
        }

        public static object MakeRequest(string requestUrl, object JSONRequest, string JSONmethod, string JSONContentType, Type JSONResponseType)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                string sb = JsonConvert.SerializeObject(JSONRequest);
                request.Method = JSONmethod;  // "GET"/"POST"/"PUT"/"DELETE";  

                if (JSONmethod != "GET")
                {
                    request.ContentType = JSONContentType; // "application/json";   
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
    }
}
