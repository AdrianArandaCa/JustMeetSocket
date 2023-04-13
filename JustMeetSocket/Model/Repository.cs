using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace JustMeetSocket.Model
{
    public class Repository
    {
        string ws = "https://172.16.24.123:45455/api/";
        Random random = new Random();

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
            questions = questions.Where(a => a.idGameType == gameType.idGameType).OrderBy(a => random.Next()).Take(5).ToList();
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
