using PaymentService.Models;

namespace PaymentService.Repositories;

public interface ICompiledPolicyAccountRepository
{
    void Add(PolicyAccount account);
    
    // Compiled Query Methods
    Task<PolicyAccount?> FindByPolicyNumberCompiled(string policyNumber);
    Task<IReadOnlyList<PolicyAccount>> FindByOwnerNameCompiled(string ownerName);
    Task<IReadOnlyList<PolicyAccount>> FindByBalanceGreaterThanCompiled(decimal minBalance);
    Task<IReadOnlyList<PolicyAccount>> FindByBalanceRangeCompiled(decimal minBalance, decimal maxBalance);
    Task<IReadOnlyList<PolicyAccount>> SearchByOwnerNameContainsCompiled(string searchTerm);
    Task<PolicyAccount?> FindByAccountNumberCompiled(string accountNumber);
    Task<int> CountByOwnerCompiled(string ownerName);
    Task<decimal> SumBalanceByOwnerCompiled(string ownerName);
    Task<IReadOnlyList<PolicyAccount>> GetTopAccountsByBalanceCompiled(int count);
    Task<IReadOnlyList<PolicyAccount>> GetAllOrderedByBalanceCompiled();
    Task<IReadOnlyList<PolicyAccount>> GetAccountsPagedCompiled(int skip, int take);
}