using Othello.Shared;
using System.Net.Http;
using System.Net.Http.Json;
using Othello.Client.Wpf.Models;

using Point = Othello.Core.Game.Point;


namespace Othello.Client.Wpf.Services
{
    public class OthelloApiClient
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };

        public async Task<GameStateDto> GetStateAsync(Guid sessionId, string matchId)
        {
            var url = $"http://localhost:5000/state?sessionId={sessionId}&matchId={matchId}";
            return await _http.GetFromJsonAsync<GameStateDto>(url)
                ?? throw new Exception("状態の取得に失敗しました");
        }

        public async Task<MoveResultDto?> PostMoveAsync(Point p, Guid sessionId, string matchId)
        {
            var url = $"http://localhost:5000/move?sessionId={sessionId}&matchId={matchId}";
            var res = await _http.PostAsJsonAsync(url, new MoveRequest(p.Row, p.Col));
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<MoveResultDto>() : null;
        }

        public async Task<JoinResponse?> JoinAsync(string name, string? matchId = null, bool isObserver = false)
        {
            var req = new JoinRequest(name, matchId, isObserver);
            var res = await _http.PostAsJsonAsync("http://localhost:5000/join", req);
            return res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<JoinResponse>()
                : null;
        }

    }
}
