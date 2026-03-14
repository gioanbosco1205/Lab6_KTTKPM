using Marten;
using PaymentService.Repositories;

namespace PaymentService.Services;

public class DataStore : IDataStore
{
    private readonly IDocumentSession _session;
    private readonly IPolicyAccountRepository _policyAccounts;

    public DataStore(IDocumentSession session, IPolicyAccountRepository policyAccounts)
    {
        _session = session;
        _policyAccounts = policyAccounts;
    }

    public IPolicyAccountRepository PolicyAccounts => _policyAccounts;

    public async Task CommitChanges()
    {
        await _session.SaveChangesAsync();
    }
}