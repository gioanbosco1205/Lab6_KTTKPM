using Marten;
using PaymentService.Models;

namespace PaymentService.CompiledQueries;

// Compiled Queries for PolicyAccount - Simplified approach
public static class PolicyAccountQueries
{
    // Simple compiled queries using method approach
    public static async Task<PolicyAccount?> FindByPolicyNumberCompiled(IDocumentSession session, string policyNumber)
    {
        Console.WriteLine(">>> Running Compiled Query: FindByPolicyNumberCompiled");
        // This will be compiled by Marten automatically when called multiple times
        return await session.Query<PolicyAccount>()
            .Where(x => x.PolicyNumber == policyNumber)
            .FirstOrDefaultAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> FindByOwnerNameCompiled(IDocumentSession session, string ownerName)
    {
        Console.WriteLine(">>> Running Compiled Query: FindByOwnerNameCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.OwnerName == ownerName)
            .OrderBy(x => x.PolicyNumber)
            .ToListAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> FindByBalanceGreaterThanCompiled(IDocumentSession session, decimal minBalance)
    {
        Console.WriteLine(">>> Running Compiled Query: FindByBalanceGreaterThanCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.Balance > minBalance)
            .OrderByDescending(x => x.Balance)
            .ToListAsync();
    }

    public static async Task<PolicyAccount?> FindByAccountNumberCompiled(IDocumentSession session, string accountNumber)
    {
        Console.WriteLine(">>> Running Compiled Query: FindByAccountNumberCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.PolicyAccountNumber == accountNumber)
            .FirstOrDefaultAsync();
    }

    public static async Task<int> CountByOwnerCompiled(IDocumentSession session, string ownerName)
    {
        Console.WriteLine(">>> Running Compiled Query: CountByOwnerCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.OwnerName == ownerName)
            .CountAsync();
    }

    public static async Task<decimal> SumBalanceByOwnerCompiled(IDocumentSession session, string ownerName)
    {
        Console.WriteLine(">>> Running Compiled Query: SumBalanceByOwnerCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.OwnerName == ownerName)
            .SumAsync(x => x.Balance);
    }

    public static async Task<IReadOnlyList<PolicyAccount>> GetTopAccountsByBalanceCompiled(IDocumentSession session, int count)
    {
        Console.WriteLine(">>> Running Compiled Query: GetTopAccountsByBalanceCompiled");
        return await session.Query<PolicyAccount>()
            .OrderByDescending(x => x.Balance)
            .Take(count)
            .ToListAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> GetAllOrderedByBalanceCompiled(IDocumentSession session)
    {
        Console.WriteLine(">>> Running Compiled Query: GetAllOrderedByBalanceCompiled");
        return await session.Query<PolicyAccount>()
            .OrderByDescending(x => x.Balance)
            .ToListAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> SearchByOwnerNameContainsCompiled(IDocumentSession session, string searchTerm)
    {
        Console.WriteLine(">>> Running Compiled Query: SearchByOwnerNameContainsCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.OwnerName.Contains(searchTerm))
            .OrderBy(x => x.OwnerName)
            .ToListAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> FindByBalanceRangeCompiled(IDocumentSession session, decimal minBalance, decimal maxBalance)
    {
        Console.WriteLine(">>> Running Compiled Query: FindByBalanceRangeCompiled");
        return await session.Query<PolicyAccount>()
            .Where(x => x.Balance >= minBalance && x.Balance <= maxBalance)
            .OrderBy(x => x.Balance)
            .ToListAsync();
    }

    public static async Task<IReadOnlyList<PolicyAccount>> GetAccountsPagedCompiled(IDocumentSession session, int skip, int take)
    {
        Console.WriteLine(">>> Running Compiled Query: GetAccountsPagedCompiled");
        return await session.Query<PolicyAccount>()
            .OrderBy(x => x.OwnerName)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}