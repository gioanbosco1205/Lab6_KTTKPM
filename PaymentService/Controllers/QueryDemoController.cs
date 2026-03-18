using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/query-demo")]
public class QueryDemoController : ControllerBase
{
    private readonly IDataStore _dataStore;

    public QueryDemoController(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet("complex-where")]
    public async Task<IActionResult> ComplexWhereQuery()
    {
        try
        {
            // LINQ: Multiple conditions with AND/OR
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            var result = accounts.Where(x => 
                (x.Balance > 1000 && x.OwnerName.Contains("Nguyen")) ||
                (x.Balance > 5000 && x.OwnerName.Contains("Tran"))
            ).ToList();

            return Ok(new { 
                description = "Accounts with (Balance > 1000 AND OwnerName contains 'Nguyen') OR (Balance > 5000 AND OwnerName contains 'Tran')",
                results = result 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in complex where query: {ex.Message}");
        }
    }

    [HttpGet("group-by-owner")]
    public async Task<IActionResult> GroupByOwnerQuery()
    {
        try
        {
            // LINQ: Group by owner and calculate statistics
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            var grouped = accounts
                .GroupBy(x => x.OwnerName)
                .Select(g => new {
                    OwnerName = g.Key,
                    AccountCount = g.Count(),
                    TotalBalance = g.Sum(x => x.Balance),
                    AverageBalance = g.Average(x => x.Balance),
                    MaxBalance = g.Max(x => x.Balance),
                    MinBalance = g.Min(x => x.Balance)
                })
                .OrderByDescending(x => x.TotalBalance)
                .ToList();

            return Ok(new { 
                description = "Accounts grouped by owner with statistics",
                results = grouped 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in group by query: {ex.Message}");
        }
    }

    [HttpGet("select-projection")]
    public async Task<IActionResult> SelectProjectionQuery()
    {
        try
        {
            // LINQ: Select specific fields (projection)
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            var projected = accounts
                .Select(x => new {
                    PolicyInfo = $"{x.PolicyNumber} - {x.PolicyAccountNumber}",
                    OwnerName = x.OwnerName.ToUpper(),
                    BalanceFormatted = $"${x.Balance:N2}",
                    BalanceCategory = x.Balance switch
                    {
                        < 1000 => "Low",
                        >= 1000 and < 5000 => "Medium",
                        >= 5000 => "High"
                    }
                })
                .OrderBy(x => x.OwnerName)
                .ToList();

            return Ok(new { 
                description = "Projected account data with formatted fields",
                results = projected 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in select projection query: {ex.Message}");
        }
    }

    [HttpGet("any-all-queries")]
    public async Task<IActionResult> AnyAllQueries()
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            
            // LINQ: Any and All operations
            var hasHighBalance = accounts.Any(x => x.Balance > 10000);
            var allHavePositiveBalance = accounts.All(x => x.Balance > 0);
            var hasNguyenOwner = accounts.Any(x => x.OwnerName.Contains("Nguyen"));
            var allHaveOwnerName = accounts.All(x => !string.IsNullOrEmpty(x.OwnerName));

            return Ok(new { 
                description = "Any/All query results",
                results = new {
                    hasAccountWithBalanceOver10k = hasHighBalance,
                    allAccountsHavePositiveBalance = allHavePositiveBalance,
                    hasOwnerWithNguyenInName = hasNguyenOwner,
                    allAccountsHaveOwnerName = allHaveOwnerName,
                    totalAccountsChecked = accounts.Count
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in any/all queries: {ex.Message}");
        }
    }

    [HttpGet("skip-take-paging/{page}/{pageSize}")]
    public async Task<IActionResult> SkipTakePaging(int page, int pageSize)
    {
        try
        {
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            var totalCount = accounts.Count;
            var pagedResults = accounts
                .OrderBy(x => x.OwnerName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new { 
                description = $"Paged results - Page {page} of {Math.Ceiling((double)totalCount / pageSize)}",
                pagination = new {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = Math.Ceiling((double)totalCount / pageSize),
                    hasNextPage = page * pageSize < totalCount,
                    hasPreviousPage = page > 1
                },
                results = pagedResults 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in paging query: {ex.Message}");
        }
    }

    [HttpGet("distinct-owners")]
    public async Task<IActionResult> DistinctOwnersQuery()
    {
        try
        {
            // LINQ: Distinct values
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            var distinctOwners = accounts
                .Select(x => x.OwnerName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return Ok(new { 
                description = "Distinct owner names",
                totalUniqueOwners = distinctOwners.Count,
                results = distinctOwners 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in distinct query: {ex.Message}");
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> StatisticsQuery()
    {
        try
        {
            // LINQ: Statistical operations
            var accounts = await _dataStore.PolicyAccounts.GetAllAccounts();
            
            if (!accounts.Any())
            {
                return Ok(new { message = "No accounts found for statistics" });
            }

            var stats = new {
                totalAccounts = accounts.Count,
                totalBalance = accounts.Sum(x => x.Balance),
                averageBalance = accounts.Average(x => x.Balance),
                maxBalance = accounts.Max(x => x.Balance),
                minBalance = accounts.Min(x => x.Balance),
                medianBalance = accounts.OrderBy(x => x.Balance)
                    .Skip(accounts.Count / 2)
                    .Take(1)
                    .Select(x => x.Balance)
                    .FirstOrDefault(),
                balanceDistribution = new {
                    under1000 = accounts.Count(x => x.Balance < 1000),
                    between1000And5000 = accounts.Count(x => x.Balance >= 1000 && x.Balance < 5000),
                    over5000 = accounts.Count(x => x.Balance >= 5000)
                }
            };

            return Ok(new { 
                description = "Account balance statistics",
                results = stats 
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error in statistics query: {ex.Message}");
        }
    }
}