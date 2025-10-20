using OfficeOpenXml;
using SmartRefillWasm.Models;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRefillWasm.Services
{
    public class RefillCalculationService : IRefillCalculationService
    {
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        private readonly Dictionary<string, string> _productMapping = new()
        {
            ["KG (Kirland Grapefruit Sparkling)"] = "KM (Grapefruit)",
            ["KLE (Kirland Lemon Sparkling)"] = "KM (Lemon)",
            ["KM (Kirland Lime Sparkling)"] = "KM (Lime)",
            ["SL (Sch Lemon)"] = "SS( Sch Lemon soda Can)"
        };

        private readonly Dictionary<string, int> _boxSizes = new()
        {
            ["BM (Bonaqua M)"] = 24, ["EM (Evian)"] = 24, ["ES (Evian Sparkling)"] = 24,
            ["KM (Grapefruit)"] = 10, ["KM (Lemon)"] = 10, ["KM (Lime)"] = 15,
            ["NU (NU Pure Water)"] = 40, ["SS (Sch Soda)"] = 24, ["SS( Sch Lemon soda Can)"] = 24,
            ["WM (Watsons M)"] = 24, ["PS (Perrier Sparkling)"] = 35, ["VV (Volvic)"] = 24
        };

        private readonly Dictionary<string, decimal> _adjustmentRates = new()
        {
            ["BM (Bonaqua M)"] = 0.15m, ["EM (Evian)"] = 0.35m, ["ES (Evian Sparkling)"] = 0.0m,
            ["KM (Grapefruit)"] = 0.5m, ["KM (Lemon)"] = 0.4m, ["KM (Lime)"] = 0.18m,
            ["NU (NU Pure Water)"] = 0.47m, ["SS (Sch Soda)"] = 0.25m,
            ["SS( Sch Lemon soda Can)"] = 0.11m, ["WM (Watsons M)"] = 0.5m
        };

        private readonly Dictionary<string, int> _maxCapacities = new()
        {
            ["BM (Bonaqua M)"] = 154,
            ["EM (Evian)"] = 291,
            ["WM (Watsons M)"] = 68,
            ["VV (Volvic)"] = 0,
            ["NU (NU Pure Water)"] = 80,
            ["ES (Evian Sparkling)"] = 10,
            ["PS (Perrier Sparkling)"] = 40,
            ["SS (Sch Soda)"] = 32,
            ["SS( Sch Lemon soda Can)"] = 32,
            ["KM (Lime)"] = 16,
            ["KM (Grapefruit)"] = 16,
            ["KM (Lemon)"] = 16
        };
        
        private readonly Dictionary<string, string> _productNameMappings;
        private readonly Dictionary<string, (string, int)> _productBoxSizes;

        public RefillCalculationService()
        {
            // Initialize product name mappings
            _productNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KG (Kirland Grapefruit Sparkling)"] = "KM (Grapefruit)",
                ["KLE (Kirland Lemon Sparkling)"] = "KM (Lemon)",
                ["KM (Kirland Lime Sparkling)"] = "KM (Lime)",
                ["SL (Sch Lemon)"] = "SS( Sch Lemon soda Can)"
            };

            _productBoxSizes = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                ["BM (Bonaqua M)"] = ("Bonaqua Mineral Water (BM)", 24),
                ["EM (Evian)"] = ("Evian", 24),
                ["ES (Evian Sparkling)"] = ("Evian Sparkling", 24),
                ["KM (Grapefruit)"] = ("KM (Grapefruit)", 10),
                ["KM (Lemon)"] = ("KM (Lemon)", 10),
                ["KM (Lime)"] = ("KM (Lime)", 15),
                ["NU (NU Pure Water)"] = ("NU Pure Water", 40),
                ["SS (Sch Soda)"] = ("SS (Sch Soda)", 24),
                ["SS( Sch Lemon soda Can)"] = ("SS( Sch Lemon soda Can)", 24),
                ["WM (Watsons M)"] = ("Watsons Mineral Water (WM)", 24),
                ["PS (Perrier Sparkling)"] = ("Perrier Sparkling", 35),
                ["VV (Volvic)"] = ("Volvic Mineral Water (VV)", 24)
            };
        }

        private async Task<MemoryStream> GetMemoryStream(IBrowserFile file)
        {
            var stream = new MemoryStream();
            await file.OpenReadStream(MaxFileSize).CopyToAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public async Task<List<string>> GetMachinesFromTransactionStreamAsync(IBrowserFile file)
        {
            await using var stream = await GetMemoryStream(file);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
                throw new InvalidOperationException("交易文件格式不正确或为空。");

            var machineSet = new HashSet<string>();
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var machineValue = worksheet.Cells[row, 4]?.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(machineValue))
                {
                    machineSet.Add(machineValue);
                }
            }
            if (machineSet.Count == 0) throw new InvalidOperationException("未在文件中找到任何机器信息。");
            return machineSet.OrderBy(m => m).ToList();
        }

        public async Task<Dictionary<string, DateTime>> GetLastRefillTimesFromHistoryStreamAsync(IBrowserFile file)
        {
            var machineRefillTimes = new Dictionary<string, DateTime>();
            await using var stream = await GetMemoryStream(file);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
                throw new InvalidOperationException("补货历史文件格式不正确或为空。");

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var refillTimeValue = worksheet.Cells[row, 1]?.Value;
                var machineValue = worksheet.Cells[row, 2]?.Value?.ToString();

                if (string.IsNullOrWhiteSpace(machineValue) || refillTimeValue == null) continue;

                DateTime refillTime = ParseDateTime(refillTimeValue);
                if (refillTime != DateTime.MinValue)
                {
                    if (!machineRefillTimes.ContainsKey(machineValue) || refillTime > machineRefillTimes[machineValue])
                    {
                        machineRefillTimes[machineValue] = refillTime;
                    }
                }
            }
            if (machineRefillTimes.Count == 0) throw new InvalidOperationException("未在历史文件中找到有效的时间数据。");
            return machineRefillTimes;
        }

        public async Task<List<RefillCalculationResult>> CalculateRefillNeedsAsync(IBrowserFile transactionFile, Dictionary<string, MachineRefillSetting> machineRefillSettings)
        {
            var salesDemand = new Dictionary<string, int>();
            await using var stream = await GetMemoryStream(transactionFile);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null) return new List<RefillCalculationResult>();

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var dateTimeValue = worksheet.Cells[row, 1]?.Value;
                var machineValue = worksheet.Cells[row, 4]?.Value?.ToString();
                var productValue = worksheet.Cells[row, 7]?.Value?.ToString();

                if (string.IsNullOrWhiteSpace(productValue) || string.IsNullOrWhiteSpace(machineValue) || dateTimeValue == null) continue;

                DateTime saleTime = ParseDateTime(dateTimeValue);
                if (saleTime != DateTime.MinValue && machineRefillSettings.TryGetValue(machineValue, out var setting) && saleTime > setting.GetFullDateTime())
                {
                    string standardProductName = _productMapping.GetValueOrDefault(productValue, productValue);
                    salesDemand[standardProductName] = salesDemand.GetValueOrDefault(standardProductName, 0) + 1;
                }
            }

            var results = salesDemand.Select(kvp => {
                var result = new RefillCalculationResult
                {
                    ProductName = kvp.Key,
                    TotalSales = kvp.Value,
                    AdjustmentRate = _adjustmentRates.GetValueOrDefault(kvp.Key, 0m),
                    BoxSize = _boxSizes.GetValueOrDefault(kvp.Key, 0),
                    MaxCapacity = GetMaxCapacityForProduct(kvp.Key)
                };
                result.Recalculate();
                return result;
            }).OrderBy(r => r.ProductName).ToList();

            return results;
        }
        
        private int GetMaxCapacityForProduct(string productName)
        {
            if (_maxCapacities.TryGetValue(productName, out int capacity))
            {
                return capacity;
            }
            
            foreach (var kvp in _maxCapacities)
            {
                if (productName.Contains(kvp.Key) || kvp.Key.Contains(productName))
                {
                    return kvp.Value;
                }
            }
            
            return 0;
        }

        private DateTime ParseDateTime(object? o)
        {
            if (o == null) return DateTime.MinValue;
            if (o is DateTime dt) return dt;
            if (double.TryParse(o.ToString(), out double oaDate))
            {
                try 
                { 
                    return DateTime.FromOADate(oaDate); 
                } 
                catch (ArgumentException) 
                { 
                    return DateTime.MinValue; 
                }
            }
            if (DateTime.TryParse(o.ToString(), out DateTime parsedDt)) return parsedDt;
            return DateTime.MinValue;
        }
    }
} 