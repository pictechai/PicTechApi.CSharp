// Program.cs (新增文件)

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PicTechApi.CSharp
{
    public class Program
    {
        // 【核心修复】为程序提供一个明确的入口点 Main 方法
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // 这个方法告诉 .NET Core 如何构建 Web 主机
        // 【关键】它会去加载并使用我们现有的 Startup.cs 文件
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}