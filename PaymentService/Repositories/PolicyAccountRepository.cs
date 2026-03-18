using Marten;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class PolicyAccountRepository : IPolicyAccountRepository
{
    private readonly IDocumentSession _session;

    public PolicyAccountRepository(IDocumentSession session)
    {
        _session = session;
    }

    public void Add(PolicyAccount account)
    {
        _session.Insert(account);
    }

    public async Task<PolicyAccount> FindByNumber(string number)
    {
        return await _session.Query<PolicyAccount>()
            .FirstOrDefaultAsync(x => x.PolicyNumber == number) ?? new PolicyAccount();
    }

    // LINQ Query Methods Implementation - Fixed return types
    public async Task<IReadOnlyList<PolicyAccount>> GetAllAccounts()
    {
        return await _session.Query<PolicyAccount>()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindByOwnerName(string ownerName)
    {
        return await _session.Query<PolicyAccount>()
            .Where(x => x.OwnerName == ownerName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindAccountsWithBalanceGreaterThan(decimal minBalance)
    {
        return await _session.Query<PolicyAccount>()
            .Where(x => x.Balance > minBalance)
            .OrderByDescending(x => x.Balance)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindAccountsWithBalanceBetween(decimal minBalance, decimal maxBalance)
    {
        return await _session.Query<PolicyAccount>()
            .Where(x => x.Balance >= minBalance && x.Balance <= maxBalance)
            .OrderBy(x => x.Balance)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PolicyAccount>> SearchAccountsByOwnerNameContains(string searchTerm)
    {
        return await _session.Query<PolicyAccount>()
            .Where(x => x.OwnerName.Contains(searchTerm))
            .OrderBy(x => x.OwnerName)
            .ToListAsync();
    }

    public async Task<PolicyAccount?> FindByAccountNumber(string accountNumber)
    {
        return await _session.Query<PolicyAccount>()
            .FirstOrDefaultAsync(x => x.PolicyAccountNumber == accountNumber);
    }

    public async Task<int> CountAccountsByOwner(string ownerName)
    {
        return await _session.Query<PolicyAccount>()
            .CountAsync(x => x.OwnerName == ownerName);
    }

    public async Task<decimal> GetTotalBalanceByOwner(string ownerName)
    {
        return await _session.Query<PolicyAccount>()
            .Where(x => x.OwnerName == ownerName)
            .SumAsync(x => x.Balance);
    }

    public async Task<IReadOnlyList<PolicyAccount>> GetTopAccountsByBalance(int count)
    {
        return await _session.Query<PolicyAccount>()
            .OrderByDescending(x => x.Balance)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindAccountsOrderedByBalance(bool descending = true)
    {
        var query = _session.Query<PolicyAccount>();

        return descending
            ? await query.OrderByDescending(x => x.Balance).ToListAsync()
            : await query.OrderBy(x => x.Balance).ToListAsync();
    }
}
