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

        public List<Question> GetQuestions()
        {
            List<Question> questions = null;
            questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
            for (var i = 0; i < questions.Count; i++)
            {
                questions[i].answers = (List<Answer>)MakeRequest(string.Concat(ws, "questionWithAnswer/", questions[i].idQuestion), null, "GET", "application/json", typeof(List<Answer>));
            }
            return questions;
        }

        public List<Question> GetQuestionsByGameType(GameType gameType)
        {
            List<Question> questions = null;
            questions = (List<Question>)MakeRequest(string.Concat(ws, "questions"), null, "GET", "application/json", typeof(List<Question>));
            questions = questions.Where(a => a.gameType.idGameType == gameType.idGameType).OrderBy(a => random.Next()).Take(5).ToList();
            for (var i = 0; i < questions.Count; i++)
            {
                questions[i].answers = (List<Answer>)MakeRequest(string.Concat(ws, "questionWithAnswer/", questions[i].idQuestion), null, "GET", "application/json", typeof(List<Answer>));
            }
            return questions;
        }

        public static object MakeRequest(string requestUrl, object JSONRequest, string JSONmethod, string JSONContentType, Type JSONResponseType)
        //  requestUrl: Url completa del Web Service, amb l'opció sol·licitada
        //  JSONrequest: objecte que se li passa en el body 
        //  JSONmethod: "GET"/"POST"/"PUT"/"DELETE"
        //  JSONContentType: "application/json" en els casos que el Web Service torni objectes
        //  JSONRensponseType:  tipus d'objecte que torna el Web Service (typeof(tipus))
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest; //WebRequest WR = WebRequest.Create(requestUrl);   
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
