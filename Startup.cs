// Startup.cs (精准修复版)

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using PicTechApi.CSharp.Clients;
using PicTechApi.CSharp.Services;
using Microsoft.Extensions.Logging; // 【新增】引入日志

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
            // ... 您现有的 services 代码保持不变 ...
            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddSingleton<ImageTranslationApiClient>();
            services.AddTransient<TranslationService>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        // 修改 Configure 方法以正确配置静态文件服务
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection(); // 如果您不需要强制HTTPS，可以注释掉

            // 中文备注：这个 UseStaticFiles() 用于服务 wwwroot 目录下的文件，例如 index.html。保留它。
            app.UseStaticFiles();

            // ===================== 【核心修复代码】 =====================

            // 1. 从配置中读取上传目录的名称 (例如 "uploads")
            var uploadDirName = Configuration.GetValue<string>("FileUpload:UploadDir");

            // 2. 检查配置是否存在
            if (!string.IsNullOrEmpty(uploadDirName))
            {
                // 3. 【关键修复】构造一个相对于项目根目录的绝对物理路径
                // env.ContentRootPath 能确保无论在哪里运行，都能找到正确的项目根目录
                var physicalPath = Path.Combine(env.ContentRootPath, uploadDirName);

                // 4. 【最佳实践】确保物理目录存在，如果不存在则创建它
                Directory.CreateDirectory(physicalPath);

                // 5. 【关键修复】使用构造好的绝对路径来配置静态文件服务
                app.UseStaticFiles(new StaticFileOptions
                {
                    // 使用绝对物理路径
                    FileProvider = new PhysicalFileProvider(physicalPath),

                    // 将 URL /uploads 映射到这个物理路径
                    // 这个 RequestPath 必须与您在 Service 层生成的 URL 前缀一致
                    RequestPath = $"/{uploadDirName}"
                });
			 // 【新增的、与Java对齐的配置】
			    // 为了能让 /iopaint_front/... 这样的URL也能工作，
			    // 我们需要为 'uploads' 文件夹下的 'iopaint_front' 子目录再创建一个映射
			    var iopaintFrontPhysicalPath = Path.Combine(physicalPath, "iopaint_front");
			    Directory.CreateDirectory(iopaintFrontPhysicalPath);

			    app.UseStaticFiles(new StaticFileOptions
			    {
			        FileProvider = new PhysicalFileProvider(iopaintFrontPhysicalPath),
			        // 将 URL /iopaint_front 直接映射到物理的 uploads/iopaint_front 文件夹
			        RequestPath = "/iopaint_front"
			    });

			    logger.LogInformation("为 iopaint_front 目录启用静态文件服务。URL '/iopaint_front' 已映射到物理路径 '{PhysicalPath}'", iopaintFrontPhysicalPath);
			                // 中文备注：添加日志以确认配置成功加载，这对于调试非常有用
                logger.LogInformation(
                    "为上传目录启用静态文件服务。URL '/{RequestPath}' 已映射到物理路径 '{PhysicalPath}'",
                    uploadDirName,
                    physicalPath);
            }
            else
            {
                // 中文备注：如果配置缺失，也打印一条警告日志
                logger.LogWarning("配置项 'FileUpload:UploadDir' 未找到，上传的文件将无法通过URL直接访问。");
            }
            // ===================== 【核心修复代码结束】 =====================

            app.UseRouting();

            // 启用CORS中间件，位置正确
            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // 中文备注：这个回退路由确保所有未匹配的API请求都返回 index.html，适用于单页面应用(SPA)。保留它。
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}