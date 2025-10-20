using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using SmartRefillWasm;
using SmartRefillWasm.Services;
using OfficeOpenXml;

// 使用 EPPlus 8 的正确许可证设置方法
ExcelPackage.License.SetNonCommercialPersonal("SmartRefill User");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<SmartRefillWasm.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IRefillCalculationService, RefillCalculationService>();

await builder.Build().RunAsync();
