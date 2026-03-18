using Marten;
using PaymentService.CompiledQueries;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class CompiledPolicyAccountRepository : ICompiledPolicyAccountRepository
{
    private readonly IDocumentSession _session;

    public CompiledPolicyAccountRepository(IDocumentSession session)
    {
        _session = session;
    }

    public void Add(PolicyAccount account)
    {
        _session.Insert(account);
    }

    // Compiled Query Implementations using static methods
    public async Task<PolicyAccount?> FindByPolicyNumberCompiled(string policyNumber)
    {
        return await PolicyAccountQueries.FindByPolicyNumberCompiled(_session, policyNumber);
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindByOwnerNameCompiled(string ownerName)
    {
        return await PolicyAccountQueries.FindByOwnerNameCompiled(_session, ownerName);
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindByBalanceGreaterThanCompiled(decimal minBalance)
    {
        return await PolicyAccountQueries.FindByBalanceGreaterThanCompiled(_session, minBalance);
    }

    public async Task<IReadOnlyList<PolicyAccount>> FindByBalanceRangeCompiled(decimal minBalance, decimal maxBalance)
    {
        return await PolicyAccountQueries.FindByBalanceRangeCompiled(_session, minBalance, maxBalance);
    }

    public async Task<IReadOnlyList<PolicyAccount>> SearchByOwnerNameContainsCompiled(string searchTerm)
    {
        return await PolicyAccountQueries.SearchByOwnerNameContainsCompiled(_session, searchTerm);
    }

    public async Task<PolicyAccount?> FindByAccountNumberCompiled(string accountNumber)
    {
        return await PolicyAccountQueries.FindByAccountNumberCompiled(_session, accountNumber);
    }

    public async Task<int> CountByOwnerCompiled(string ownerName)
    {
        return await PolicyAccountQueries.CountByOwnerCompiled(_session, ownerName);
    }

    public async Task<decimal> SumBalanceByOwnerCompiled(string ownerName)
    {
        return await PolicyAccountQueries.SumBalanceByOwnerCompiled(_session, ownerName);
    }

    public async Task<IReadOnlyList<PolicyAccount>> GetTopAccountsByBalanceCompiled(int count)
    {
        return await PolicyAccountQueries.GetTopAccountsByBalanceCompiled(_session, count);
    }

    public async Task<IReadOnlyList<PolicyAccount>> GetAllOrderedByBalanceCompiled()
    {
        return await PolicyAccountQueries.GetAllOrderedByBalanceCompiled(_session);
    }

    public async Task<IReadOnlyList<PolicyAccount>> GetAccountsPagedCompiled(int skip, int take)
    {
        return await PolicyAccountQueries.GetAccountsPagedCompiled(_session, skip, take);
    }
}