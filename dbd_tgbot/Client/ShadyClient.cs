using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dbd_tgbot.Constant;
using dbd_tgbot.Model;
using Newtonsoft.Json;

namespace dbd_tgbot.Client
{
    internal class ShadyClient
    {
        private HttpClient _client;
        private static string _address;

        public ShadyClient()
        {
            _address = Constants.address;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_address);
        }
        public async Task<UserStats?> GetUserStatsForGameAsync(string SteamID)
        {
            var responce = await _client.GetAsync($"/Stat?SteamID={SteamID}");
            var content = responce.Content.ReadAsStringAsync().Result;
            try
            {
                var result = JsonConvert.DeserializeObject<UserStats>(content);
                return result;
            }
            catch (Exception){}

            return null;
        }
        public async Task<List<Perk>?> GetPerksAsync()
        {
            var responce = await _client.GetAsync($"/Perks");
            var content = responce.Content.ReadAsStringAsync().Result;
            try
            {
                var result = JsonConvert.DeserializeObject<List<Perk>>(content);
                return result;
            }
            catch (Exception) { }

            return null;
        }
        public async Task<List<Killer>?> GetKillersAsync()
        {
            var responce = await _client.GetAsync($"/Killers");
            var content = responce.Content.ReadAsStringAsync().Result;
            try
            {
                var result = JsonConvert.DeserializeObject<List<Killer>>(content);
                return result;
            }
            catch (Exception) { }

            return null;
        }
        public async Task<List<Survivor>?> GetSurvivorsAsync()
        {
            var responce = await _client.GetAsync($"/Survivors");
            var content = responce.Content.ReadAsStringAsync().Result;
            try
            {
                var result = JsonConvert.DeserializeObject<List<Survivor>>(content);
                return result;
            }
            catch (Exception) { }

            return null;
        }
    }
}
