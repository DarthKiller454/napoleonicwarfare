using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Alliance.Client.Extensions.CustomName
{
    public static class NicknameSender
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task SendNicknameToServer(string playerId, string nickname)
        {
            try
            {
                var url = "http://109.230.239.42/username_api/username.php"; // Or your public server URL

                var data = new
                {
                    playerId = playerId,
                    nickname = nickname
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Nickname sync response: {responseContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending nickname: {ex.Message}");
            }
        }
        public static async Task<string> GetNicknameFromServer(string playerId)
        {
            try
            {
                var url = $"http://109.230.239.42/username_api/get_username.php?playerId={playerId}";
                var response = await HttpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseContent = await response.Content.ReadAsStringAsync();

                // Your PHP script should return a JSON like: { "nickname": "..." }
                var parsed = JsonConvert.DeserializeObject<NicknameResponse>(responseContent);

                return parsed?.nickname;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving nickname: {ex.Message}");
                return null;
            }
        }

        private class NicknameResponse
        {
            public string nickname { get; set; }
        }
    }
}