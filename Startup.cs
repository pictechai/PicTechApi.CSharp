// Startup.cs (完整修复版)

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using PicTechApi.CSharp.Clients; // 确保引用了这些命名空间
using PicTechApi.CSharp.Services;

namespace PicTechApi.CSharp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddSingleton<ImageTranslationApiClient>();
            services.AddTransient<TranslationService>();
            
            // ============================================================
            // =      【核心修复】为 services.AddCors(...) 提供完整实现      =
            // ============================================================
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    // 允许任何来源、任何方法、任何头部的请求
                    // 这对于开发环境非常方便
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        // Configure 方法保持不变，因为它已经是正确的
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            var uploadDir = Configuration["FileUpload:UploadDir"];
            if (!string.IsNullOrEmpty(uploadDir) && Directory.Exists(uploadDir))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(uploadDir),
                    RequestPath = "/uploads"
                });
            }

            app.UseRouting();
            
            // 启用CORS中间件
            app.UseCors();
            
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}