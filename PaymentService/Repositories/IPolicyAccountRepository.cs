using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IPolicyAccountRepository
{
    void Add(PolicyAccount account);
    Task<PolicyAccount> FindByNumber(string number);

    // LINQ Query Methods - Changed to IReadOnlyList to match Marten
    Task<IReadOnlyList<PolicyAccount>> GetAllAccounts();
    Task<IReadOnlyList<PolicyAccount>> FindByOwnerName(string ownerName);
    Task<IReadOnlyList<PolicyAccount>> FindAccountsWithBalanceGreaterThan(decimal minBalance);
    Task<IReadOnlyList<PolicyAccount>> FindAccountsWithBalanceBetween(decimal minBalance, decimal maxBalance);
    Task<IReadOnlyList<PolicyAccount>> SearchAccountsByOwnerNameContains(string searchTerm);
    Task<PolicyAccount?> FindByAccountNumber(string accountNumber);
    Task<int> CountAccountsByOwner(string ownerName);
    Task<decimal> GetTotalBalanceByOwner(string ownerName);
    Task<IReadOnlyList<PolicyAccount>> GetTopAccountsByBalance(int count);
    Task<IReadOnlyList<PolicyAccount>> FindAccountsOrderedByBalance(bool descending = true);
}
