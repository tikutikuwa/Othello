using Othello.Core.Game;

namespace Othello.Server.Models;

public record Session(Guid Id, string Name, Stone Color);
