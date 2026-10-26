namespace Api.Contracts;

public sealed record Card(string Number, int ExpiryYear);
public sealed record BankTransfer(string Iban);

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union PaymentMethod(Card, BankTransfer);
