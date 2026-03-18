using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/compiled")]
public class CompiledQueryController : ControllerBase
{
    private readonly IDataStore _dataStore;

    public CompiledQueryController(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet("policy/{policyNumber}")]
    public async Task<IActionResult> FindByPolicyNumberCompiled(string policyNumber)
    {
        try
        {
            var account = await _dataStore.CompiledPolicyAccounts.FindByPolicyNumberCompiled(policyNumber);
            if (account == null)
                return NotFound($"Account with policy number {policyNumber} not found");

            return Ok(new { 
                message = "Found using COMPILED QUERY",
                account 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("owner/{ownerName}")]
    public async Task<IActionResult> FindByOwnerNameCompiled(string ownerName)
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.FindByOwnerNameCompiled(ownerName);
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("balance/greater-than/{minBalance}")]
    public async Task<IActionResult> FindByBalanceGreaterThanCompiled(decimal minBalance)
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.FindByBalanceGreaterThanCompiled(minBalance);
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                minBalance,
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("balance/range/{minBalance}/{maxBalance}")]
    public async Task<IActionResult> FindByBalanceRangeCompiled(decimal minBalance, decimal maxBalance)
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.FindByBalanceRangeCompiled(minBalance, maxBalance);
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                range = $"{minBalance} - {maxBalance}",
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("search/owner/{searchTerm}")]
    public async Task<IActionResult> SearchByOwnerNameContainsCompiled(string searchTerm)
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.SearchByOwnerNameContainsCompiled(searchTerm);
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                searchTerm,
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("account-number/{accountNumber}")]
    public async Task<IActionResult> FindByAccountNumberCompiled(string accountNumber)
    {
        try
        {
            var account = await _dataStore.CompiledPolicyAccounts.FindByAccountNumberCompiled(accountNumber);
            if (account == null)
                return NotFound($"Account with account number {accountNumber} not found");

            return Ok(new { 
                message = "Found using COMPILED QUERY",
                account 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("count/owner/{ownerName}")]
    public async Task<IActionResult> CountByOwnerCompiled(string ownerName)
    {
        try
        {
            var count = await _dataStore.CompiledPolicyAccounts.CountByOwnerCompiled(ownerName);
            return Ok(new { 
                message = "Counted using COMPILED QUERY",
                ownerName,
                accountCount = count 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("sum-balance/owner/{ownerName}")]
    public async Task<IActionResult> SumBalanceByOwnerCompiled(string ownerName)
    {
        try
        {
            var totalBalance = await _dataStore.CompiledPolicyAccounts.SumBalanceByOwnerCompiled(ownerName);
            return Ok(new { 
                message = "Summed using COMPILED QUERY",
                ownerName,
                totalBalance 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("top/{count}")]
    public async Task<IActionResult> GetTopAccountsByBalanceCompiled(int count)
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.GetTopAccountsByBalanceCompiled(count);
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                requestedCount = count,
                actualCount = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("all-ordered")]
    public async Task<IActionResult> GetAllOrderedByBalanceCompiled()
    {
        try
        {
            var accounts = await _dataStore.CompiledPolicyAccounts.GetAllOrderedByBalanceCompiled();
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("paged/{page}/{pageSize}")]
    public async Task<IActionResult> GetAccountsPagedCompiled(int page, int pageSize)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var accounts = await _dataStore.CompiledPolicyAccounts.GetAccountsPagedCompiled(skip, pageSize);
            
            return Ok(new { 
                message = "Found using COMPILED QUERY",
                page,
                pageSize,
                count = accounts.Count,
                accounts 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in compiled query: {ex.Message}");
        }
    }

    [HttpGet("performance-comparison/{policyNumber}")]
    public async Task<IActionResult> PerformanceComparison(string policyNumber)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Console.WriteLine("=== PERFORMANCE COMPARISON START ===");
            Console.WriteLine($"Testing PolicyNumber: {policyNumber}");
            
            // Regular LINQ query
            Console.WriteLine("\n--- Testing Regular LINQ Query ---");
            stopwatch.Restart();
            var regularResult = await _dataStore.PolicyAccounts.FindByNumber(policyNumber);
            var regularTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Regular LINQ Query completed in: {regularTime}ms");
            
            // Compiled query
            Console.WriteLine("\n--- Testing Compiled Query ---");
            stopwatch.Restart();
            var compiledResult = await _dataStore.CompiledPolicyAccounts.FindByPolicyNumberCompiled(policyNumber);
            var compiledTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Compiled Query completed in: {compiledTime}ms");
            
            Console.WriteLine("=== PERFORMANCE COMPARISON END ===\n");
            
            return Ok(new { 
                message = "Performance comparison completed - Check console logs!",
                policyNumber,
                results = new {
                    regularQueryTime = $"{regularTime}ms",
                    compiledQueryTime = $"{compiledTime}ms",
                    improvement = regularTime > 0 ? $"{((double)(regularTime - compiledTime) / regularTime * 100):F1}%" : "N/A",
                    bothFoundSameResult = (regularResult.Id != Guid.Empty) == (compiledResult != null),
                    note = "Check console output to see '>>> Running Compiled Query' vs '>>> Running Regular LINQ Query' logs"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in performance comparison: {ex.Message}");
        }
    }

    [HttpGet("demo-logs/{policyNumber}")]
    public async Task<IActionResult> DemoLogs(string policyNumber)
    {
        try
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("DEMO: COMPILED QUERY vs REGULAR LINQ QUERY");
            Console.WriteLine(new string('=', 60));
            
            Console.WriteLine("\n🔥 CALLING REGULAR LINQ QUERY:");
            var regularResult = await _dataStore.PolicyAccounts.FindByNumber(policyNumber);
            
            Console.WriteLine("\n⚡ CALLING COMPILED QUERY:");
            var compiledResult = await _dataStore.CompiledPolicyAccounts.FindByPolicyNumberCompiled(policyNumber);
            
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("DEMO COMPLETED - Check console output above!");
            Console.WriteLine(new string('=', 60) + "\n");
            
            return Ok(new { 
                message = "Demo completed! Check console logs to see the difference:",
                instructions = new[] {
                    "1. Look for '>>> Running Regular LINQ Query: FindByNumber'",
                    "2. Look for '>>> Running Compiled Query: FindByPolicyNumberCompiled'",
                    "3. This proves which type of query is being executed!"
                },
                results = new {
                    regularFound = regularResult.Id != Guid.Empty,
                    compiledFound = compiledResult != null,
                    bothReturnedSameResult = (regularResult.Id != Guid.Empty) == (compiledResult != null)
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in demo: {ex.Message}");
        }
    }
}