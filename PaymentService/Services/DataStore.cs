using Marten;
using PaymentService.Repositories;

namespace PaymentService.Services;

public interface IDataStore
{
    IPolicyAccountRepository PolicyAccounts { get; }
    ICompiledPolicyAccountRepository CompiledPolicyAccounts { get; }
    Task CommitChanges();
}

public class DataStore : IDataStore
{
    private readonly IDocumentSession _session;
    private readonly IPolicyAccountRepository _policyAccounts;
    private readonly ICompiledPolicyAccountRepository _compiledPolicyAccounts;

    public DataStore(IDocumentSession session, IPolicyAccountRepository policyAccounts, ICompiledPolicyAccountRepository compiledPolicyAccounts)
    {
        _session = session;
        _policyAccounts = policyAccounts;
        _compiledPolicyAccounts = compiledPolicyAccounts;
    }

    public IPolicyAccountRepository PolicyAccounts => _policyAccounts;
    public ICompiledPolicyAccountRepository CompiledPolicyAccounts => _compiledPolicyAccounts;

    public async Task CommitChanges()
    {
        await _session.SaveChangesAsync();
    }
}