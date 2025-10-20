using Microsoft.AspNetCore.Components.Forms;
using SmartRefillWasm.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRefillWasm.Services
{
    public interface IRefillCalculationService
    {
        Task<List<string>> GetMachinesFromTransactionStreamAsync(IBrowserFile file);

        Task<Dictionary<string, DateTime>> GetLastRefillTimesFromHistoryStreamAsync(IBrowserFile file);

        Task<List<RefillCalculationResult>> CalculateRefillNeedsAsync(
            IBrowserFile transactionFile,
            Dictionary<string, MachineRefillSetting> machineRefillSettings);
    }
} 