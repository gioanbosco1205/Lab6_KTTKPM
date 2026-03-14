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
}