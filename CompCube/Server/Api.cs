using System.Net;
using System.Net.Http;
using CompCube_Models.Models.Events;
using CompCube_Models.Models.Server;
using CompCube.Configuration;
using CompCube.Interfaces;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SiraUtil.Logging;
using Zenject;

namespace CompCube.Server
{
    public class Api : IApi, IInitializable
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _siraLog = null!;
        
        private readonly HttpClient _client = new();

        public void Initialize()
        {
            _client.BaseAddress = new Uri($"https://{_config.ServerIp}:{_config.ServerApiPort}/");
        }

        public async Task<CompCube_Models.Models.ClientData.UserInfo?> GetUserInfo(string id)
        {
            var response = await _client.GetAsync($"/api/user/id/{id}");

            return response.StatusCode == HttpStatusCode.NotFound ? null : JsonConvert.DeserializeObject<CompCube_Models.Models.ClientData.UserInfo>(await response.Content.ReadAsStringAsync());
        }

        public async Task<CompCube_Models.Models.ClientData.UserInfo[]?> GetLeaderboardRange(int start, int range)
        {
            var response = await _client.GetAsync($"/api/leaderboard/range?start={start}&range={range}");
            
            return JsonConvert.DeserializeObject<CompCube_Models.Models.ClientData.UserInfo[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<CompCube_Models.Models.ClientData.UserInfo[]?> GetAroundUser(string id)
        {
            var response = await _client.GetAsync($"/api/leaderboard/aroundUser/{id}");
            return response.StatusCode == HttpStatusCode.NotFound ? null : JsonConvert.DeserializeObject<CompCube_Models.Models.ClientData.UserInfo[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<ServerStatus?> GetServerStatus()
        {
            // _siraLog.Info("getting server status");
            var response = await _client.GetAsync("/api/server/status");
            // _siraLog.Info(response.Content.ReadAsStringAsync().Result);
            return response.StatusCode == HttpStatusCode.NotFound ? null : JsonConvert.DeserializeObject<ServerStatus>(await response.Content.ReadAsStringAsync());
        }

        public async Task<string[]?> GetMapHashes()
        {
            var response = await _client.GetAsync("/api/maps/hashes");
            return JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<EventData[]?> GetEvents()
        {
            var response = await _client.GetAsync("/api/events/events");
            return JsonConvert.DeserializeObject<EventData[]>(await response.Content.ReadAsStringAsync());
        }
    }
}