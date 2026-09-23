namespace api.Models;

public record Purchase(long Id, string Description, DateTime Date, decimal Amount, Guid ClientPurchaseId);
