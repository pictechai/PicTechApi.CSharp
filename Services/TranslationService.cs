// TranslationService.cs
// 放在一个名为 "Services" 的文件夹中

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PicTechApi.CSharp.Clients;

namespace PicTechApi.CSharp.Services
{
    /// <summary>
    /// 封装图片翻译的核心业务逻辑
    /// </summary>
    public class TranslationService
    {
        private readonly ImageTranslationApiClient _apiClient;

        public TranslationService(ImageTranslationApiClient apiClient)
        {
            _apiClient = apiClient;
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
    }
}