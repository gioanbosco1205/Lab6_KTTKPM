using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IPolicyAccountRepository
{
    void Add(PolicyAccount account);
    Task<PolicyAccount> FindByNumber(string number);
}