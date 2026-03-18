using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IDataStore _dataStore;

    public AccountController(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] PolicyAccount account)
    {
        try
        {
            if (account.Id == Guid.Empty)
                account.Id = Guid.NewGuid();

            _dataStore.PolicyAccounts.Add(account);
            await _dataStore.CommitChanges();
            
            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating account: {ex.Message}");
        }
    }

    [HttpGet("{number}")]
    public async Task<IActionResult> GetAccountByNumber(string number)
    {
        try
        {
            var account = await _dataStore.PolicyAccounts.FindByNumber(number);
            if (account.Id == Guid.Empty)
                return NotFound($"Account with policy number {number} not found");

            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving account: {ex.Message}");
        }
    }

    // LINQ Query Endpoints
    [HttpGet]
    public async Task<IActionResult> GetAllAccounts()
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving accounts: {ex.Message}");
        }
    }

    [HttpGet("owner/{ownerName}")]
    public async Task<IActionResult> GetAccountsByOwner(string ownerName)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.FindByOwnerName(ownerName);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving accounts by owner: {ex.Message}");
        }
    }

    [HttpGet("balance/greater-than/{minBalance}")]
    public async Task<IActionResult> GetAccountsWithBalanceGreaterThan(decimal minBalance)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.FindAccountsWithBalanceGreaterThan(minBalance);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving accounts by balance: {ex.Message}");
        }
    }

    [HttpGet("balance/between/{minBalance}/{maxBalance}")]
    public async Task<IActionResult> GetAccountsWithBalanceBetween(decimal minBalance, decimal maxBalance)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.FindAccountsWithBalanceBetween(minBalance, maxBalance);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving accounts by balance range: {ex.Message}");
        }
    }

    [HttpGet("search/owner/{searchTerm}")]
    public async Task<IActionResult> SearchAccountsByOwnerName(string searchTerm)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.SearchAccountsByOwnerNameContains(searchTerm);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error searching accounts: {ex.Message}");
        }
    }

    [HttpGet("account-number/{accountNumber}")]
    public async Task<IActionResult> GetAccountByAccountNumber(string accountNumber)
    {
        try
        {
            var account = await _dataStore.PolicyAccounts.FindByAccountNumber(accountNumber);
            if (account == null)
                return NotFound($"Account with account number {accountNumber} not found");

            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving account: {ex.Message}");
        }
    }

    [HttpGet("count/owner/{ownerName}")]
    public async Task<IActionResult> GetAccountCountByOwner(string ownerName)
    {
        try
        {
            var count = await _dataStore.PolicyAccounts.CountAccountsByOwner(ownerName);
            return Ok(new { ownerName, accountCount = count });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error counting accounts: {ex.Message}");
        }
    }

    [HttpGet("total-balance/owner/{ownerName}")]
    public async Task<IActionResult> GetTotalBalanceByOwner(string ownerName)
    {
        try
        {
            var totalBalance = await _dataStore.PolicyAccounts.GetTotalBalanceByOwner(ownerName);
            return Ok(new { ownerName, totalBalance });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error calculating total balance: {ex.Message}");
        }
    }

    [HttpGet("top/{count}")]
    public async Task<IActionResult> GetTopAccountsByBalance(int count)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.GetTopAccountsByBalance(count);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving top accounts: {ex.Message}");
        }
    }

    [HttpGet("ordered-by-balance")]
    public async Task<IActionResult> GetAccountsOrderedByBalance([FromQuery] bool descending = true)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.FindAccountsOrderedByBalance(descending);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving ordered accounts: {ex.Message}");
        }
    }
}