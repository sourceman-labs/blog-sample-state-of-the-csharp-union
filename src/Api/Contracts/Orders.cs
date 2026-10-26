namespace Api.Contracts;

public sealed record OrderResponse(int Id, string State);

public sealed record ShipRequest(string TrackingNumber);

public sealed record NotFoundInfo(int Id, string Message);
