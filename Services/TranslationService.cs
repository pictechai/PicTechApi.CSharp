// TranslationService.cs
// 放在一个名为 "Services" 的文件夹中

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PicTechApi.CSharp.Clients;
using Microsoft.Extensions.Configuration; // 【新增】
using Microsoft.Extensions.Logging;     // 【新增】

namespace PicTechApi.CSharp.Services
{
    /// <summary>
    /// 封装图片翻译的核心业务逻辑
    /// </summary>
    public class TranslationService
    {
        private readonly ImageTranslationApiClient _apiClient;
        private readonly ILogger<TranslationService> _logger; // 【新增】
        private readonly string _uploadDir;                  // 【新增】
        private readonly IHttpContextAccessor _httpContextAccessor; // 【新增】字段

      // 【修改】构造函数，注入更多所需服务
        public TranslationService(ImageTranslationApiClient apiClient, IConfiguration configuration,
        ILogger<TranslationService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
            _uploadDir = configuration["FileUpload:UploadDir"];
        }

        public Task<Dictionary<string, object>> SubmitTaskFromUrlAsync(string imageUrl, string sourceLanguage, string targetLanguage)
        {
            return _apiClient.SubmitTranslationTaskWithUrlAsync(imageUrl, sourceLanguage, targetLanguage);
        }

        public Task<Dictionary<string, object>> SubmitTaskFromBase64Async(string imageBase64, string sourceLanguage, string targetLanguage)
        {
            return _apiClient.SubmitTranslationTaskWithBase64Async(imageBase64, sourceLanguage, targetLanguage);
        }

        public async Task<Dictionary<string, object>> SubmitTaskFromFileAsync(IFormFile file, string sourceLanguage, string targetLanguage)
        {
            var imageBase64 = await ConvertFileToBase64Async(file);
            return await _apiClient.SubmitTranslationTaskWithBase64Async(imageBase64, sourceLanguage, targetLanguage);
        }

        public Task<Dictionary<string, object>> QueryTaskResultAsync(string requestId)
        {
            return _apiClient.QueryTranslationTaskResultAsync(requestId);
        }
        
        private async Task<string> ConvertFileToBase64Async(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            return Convert.ToBase64String(fileBytes);
        }

        #region 【新增】图像修复 (Inpainting) 相关服务

        /// <summary>
        /// 【新增】处理擦除逻辑，调用API，保存结果，并返回新图片的Base64编码
        /// </summary>
        public async Task<string> IopaintAsync(string sourceImageBase64, string maskImageBase64)
        {
            try
            {
                // 1. 调用API客户端执行AI服务，直接获取修复后的图片字节
                var imageBytes = await _apiClient.InpaintImageSyncAsync(sourceImageBase64, maskImageBase64);

                // 2. 准备保存路径和文件名 (用于调试或归档)
                var uniqueImageName = Guid.NewGuid().ToString();
                var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

                // 中文备注：使用 Path.Combine 确保跨平台路径兼容性
                var directoryPath = Path.Combine(_uploadDir, "iopaint", dateFolder);
                Directory.CreateDirectory(directoryPath); // 确保目录存在

                var newImageNameWithExt = $"{uniqueImageName}.png";
                var filePath = Path.Combine(directoryPath, newImageNameWithExt);

                // 3. 将API返回的图片字节保存到文件
                await File.WriteAllBytesAsync(filePath, imageBytes);
                _logger.LogInformation("Inpainted image saved to: {FilePath}", filePath);

                // 4. 将图片字节编码为 Base64 字符串，直接返回给前端，避免了文件再读取的IO操作
                var newImageBase64 = Convert.ToBase64String(imageBytes);
                return newImageBase64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IopaintAsync] 处理过程中发生错误。");
                throw new Exception($"iopaint 处理失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 【新增】接收 Inpaint 后的 Base64 图片，保存到项目静态资源目录并返回可访问 URL。
        /// </summary>
        public async Task<string> UploadIoInpaintImageAsync(string imageBase64)
        {
            try
            {
                // 1. 解码 Base64
                var imageBytes = Convert.FromBase64String(CleanBase64Prefix(imageBase64));

                // 2. 确定保存路径
                var savePath = "iopaint_front";
                var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                var directoryPath = Path.Combine(_uploadDir, savePath, dateFolder);
                Directory.CreateDirectory(directoryPath);

                // 3. 生成唯一文件名并保存
                var uniqueFileName = $"{Guid.NewGuid()}.png";
                var physicalFilePath = Path.Combine(directoryPath, uniqueFileName);

                await File.WriteAllBytesAsync(physicalFilePath, imageBytes);

                // 4. 构造并返回前端可访问的 URL
                // 中文备注：URL路径分隔符应始终为 '/'。同时需要加上静态文件服务的根路径（通常是上传目录名）
                var publicRoot = Path.GetFileName(_uploadDir);
                var finalUrl = $"/{savePath}/{dateFolder}/{uniqueFileName}".Replace('\\', '/');

                _logger.LogInformation("返回给前端的 URL: {FinalUrl}", finalUrl);
                return finalUrl;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Base64 解码失败。");
                throw new ArgumentException("无效的Base64数据", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "文件写入或路径查找时发生IO异常。");
                throw new IOException("文件保存失败，服务器IO错误", ex);
            }
        }

        #endregion

        // ... (私有方法 ConvertFileToBase64Async 保持不变) ...

        /// <summary>
        /// 辅助方法，用于移除 Base64 字符串的 "data:image/..." 前缀
        /// </summary>
        private string CleanBase64Prefix(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String)) return string.Empty;

            var commaIndex = base64String.IndexOf(',');
            return commaIndex > -1 ? base64String.Substring(commaIndex + 1) : base64String;
        }

    }
}