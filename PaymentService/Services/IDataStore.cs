using PaymentService.Repositories;

namespace PaymentService.Services;

public interface IDataStore
{
    IPolicyAccountRepository PolicyAccounts { get; }
    Task CommitChanges();
}