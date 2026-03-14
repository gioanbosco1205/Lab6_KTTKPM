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
}