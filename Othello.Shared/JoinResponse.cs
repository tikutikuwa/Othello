using Othello.Core.Game;

namespace Othello.Shared;

public record JoinResponse(Guid SessionId, string MatchId, Stone AssignedColor);
