namespace TicketSystem.Dtos.Token;

public sealed record IssuedTokenDto(string AccessToken, DateTimeOffset ExpiresAt);
