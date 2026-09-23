namespace api.Models;

public class DuplicateClientPurchaseIdException(Guid clientPurchaseId)
    : Exception($"A purchase with ClientPurchaseId '{clientPurchaseId}' already exists.")
{
    public Guid ClientPurchaseId { get; } = clientPurchaseId;
}
