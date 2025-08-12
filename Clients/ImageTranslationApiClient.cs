 // ImageTranslationApiClient.cs (修复版)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PicTechApi.CSharp.Clients
{
    public class ImageTranslationApiClient
    {
        private readonly ILogger<ImageTranslationApiClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private static readonly HttpClient HttpClient = new HttpClient();

        private const string TranslationSubmitEndpoint = "/submit_task";
        private const string TranslationQueryEndpoint = "/query_result";
        private const string BgRemovalSubmitEndpoint = "/submit_remove_background_task";
        private const string BgRemovalQueryEndpoint = "/query_remove_background_result";

        public ImageTranslationApiClient(IConfiguration configuration, ILogger<ImageTranslationApiClient> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["PicTechApi:BaseUrl"];
            _apiKey = _configuration["PicTechApi:Key"];
            _secretKey = _configuration["PicTechApi:Secret"];
        }
        
        // ... (从 SubmitTranslationTaskWithUrlAsync 到 QueryRemoveBackgroundTaskResultAsync 的所有公共方法保持不变) ...

        public async Task<Dictionary<string, object>> SubmitTranslationTaskWithUrlAsync(string imageUrl, string sourceLanguage, string targetLanguage)
        {
            var payload = new Dictionary<string, object>
            {
                { "ImageUrl", imageUrl },
                { "SourceLanguage", sourceLanguage },
                { "TargetLanguage", targetLanguage }
            };
            return await ExecutePostRequestAsync(TranslationSubmitEndpoint, payload);
        }
        
        public async Task<Dictionary<string, object>> SubmitTranslationTaskWithBase64Async(string imageBase64, string sourceLanguage, string targetLanguage)
        {
            var payload = new Dictionary<string, object>
            {
                { "ImageBase64", imageBase64 },
                { "SourceLanguage", sourceLanguage },
                { "TargetLanguage", targetLanguage }
            };
            return await ExecutePostRequestAsync(TranslationSubmitEndpoint, payload);
        }

        public async Task<Dictionary<string, object>> QueryTranslationTaskResultAsync(string requestId)
        {
            var payload = new Dictionary<string, object>
            {
                { "RequestId", requestId }
            };
            return await ExecutePostRequestAsync(TranslationQueryEndpoint, payload);
        }
        
        public async Task<bool> RemoveBackgroundAsync(string imagePath, string imageUrl, string outputDir, string outputFilename)
        {
            var startTime = DateTime.UtcNow;

            string imageBase64 = null;
            if (!string.IsNullOrEmpty(imagePath))
            {
                imageBase64 = await ReadImageAsBase64Async(imagePath);
                if (string.IsNullOrEmpty(imageBase64))
                {
                    _logger.LogError("从路径读取图片并转换为 Base64 失败: {ImagePath}", imagePath);
                    return false;
                }
            }
            
            var payload = new Dictionary<string, object> { { "BgColor", "white" } };
            if (!string.IsNullOrEmpty(imageBase64))
            {
                payload.Add("ImageBase64", imageBase64);
            }
            else if (!string.IsNullOrEmpty(imageUrl))
            {
                payload.Add("ImageUrl", imageUrl);
            }
            else
            {
                _logger.LogError("必须提供本地图片路径(imagePath)或图片URL(imageUrl)中的一个！");
                return false;
            }
            
            var submitResponse = await ExecutePostRequestAsync(BgRemovalSubmitEndpoint, payload);
            if (submitResponse == null || Convert.ToInt32(submitResponse.GetValueOrDefault("Code", -1)) != 200)
            {
                var errorMessage = submitResponse != null ? submitResponse["Message"].ToString() : "无响应";
                _logger.LogError("抠图任务提交失败: {ErrorMessage}", errorMessage);
                return false;
            }

            var requestId = submitResponse["RequestId"].ToString();
            _logger.LogInformation("任务提交成功, RequestId: {RequestId}", requestId);
            
            const int maxAttempts = 15;
            var interval = TimeSpan.FromMilliseconds(1500);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var result = await QueryRemoveBackgroundTaskResultAsync(requestId);
                if (result == null)
                {
                    _logger.LogError("查询任务 {RequestId} 失败: 无响应。", requestId);
                    return false;
                }
                
                int code = Convert.ToInt32(result.GetValueOrDefault("Code", -1));
                if (code == 200)
                {
                    var data = result["Data"] as Newtonsoft.Json.Linq.JObject;
                    if (data == null || data["OutputUrl"] == null)
                    {
                        _logger.LogError("任务成功，但响应中未找到有效的输出URL (OutputUrl)。");
                        return false;
                    }

                    var outputUrl = data["OutputUrl"].ToString();
                    _logger.LogInformation("任务处理成功，结果图片URL: {OutputUrl}", outputUrl);

                    try
                    {
                        var outputPath = Path.Combine(outputDir, outputFilename);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        var imageBytes = await HttpClient.GetByteArrayAsync(outputUrl);
                        await File.WriteAllBytesAsync(outputPath, imageBytes);
                        
                        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                        _logger.LogInformation("图片已成功保存到: {OutputPath}", outputPath);
                        _logger.LogInformation("任务总耗时: {Duration:F2} 秒", duration);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "下载或保存图片失败");
                        return false;
                    }
                }
                else if (code == 202)
                {
                    _logger.LogInformation("任务 {RequestId} 仍在处理中，{IntervalSeconds}秒后重试 (尝试 {Attempt}/{MaxAttempts})", 
                        requestId, interval.TotalSeconds, attempt, maxAttempts);
                    await Task.Delay(interval);
                }
                else
                {
                    var errorMessage = $"{result["Message"]}, ErrorCode: {result["ErrorCode"]}";
                    _logger.LogError("任务 {RequestId} 处理失败: {ErrorMessage}", requestId, errorMessage);
                    return false;
                }
            }
            
            _logger.LogError("任务 {RequestId} 在 {MaxAttempts} 次尝试后仍未完成，已超时。", requestId, maxAttempts);
            return false;
        }

        public async Task<Dictionary<string, object>> QueryRemoveBackgroundTaskResultAsync(string requestId)
        {
            var payload = new Dictionary<string, object>
            {
                { "RequestId", requestId }
            };
            return await ExecutePostRequestAsync(BgRemovalQueryEndpoint, payload);
        }

        // ===================================================================================
        // =                          私有辅助方法 (Private Helpers)                         =
        // ===================================================================================
      
        private async Task<string> ReadImageAsBase64Async(string filePath)
        {
            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(filePath);
                return Convert.ToBase64String(imageBytes);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "读取文件失败: {FilePath}", filePath);
                return null;
            }
        }

        private async Task<Dictionary<string, object>> ExecutePostRequestAsync(string endpoint, Dictionary<string, object> payload)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
            
            payload["AccountId"] = _apiKey;
            payload["Timestamp"] = timestamp;
            
            // 【修复#1】将调用处的参数名也修改掉
            var paramsForSignature = payload.ToDictionary(k => k.Key, v => v.Value.ToString());
            var signature = GenerateSignature(paramsForSignature); 
            payload["Signature"] = signature;
            
            var jsonBody = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("请求 URL: {Url}", _apiBaseUrl + endpoint);
            _logger.LogDebug("请求体: {Body}", jsonBody);
            
            try
            {
                var response = await HttpClient.PostAsync(_apiBaseUrl + endpoint, content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "调用 PicTech API 失败: {Endpoint}", endpoint);
                throw;
            }
        }
 
        // 【核心修复#2】将参数名从 `params` 修改为 `parameters` (或任何其他非关键字名称)
        private string GenerateSignature(Dictionary<string, string> parameters)
        {
            // 1. 过滤掉值为空或null的参数，并按参数名(key)的字母顺序排序
            var sortedParams = parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .OrderBy(p => p.Key);

            // 2. 将排序后的参数拼接成 `key1=value1&key2=value2` 的形式
            var canonicalQueryString = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}"));

            // 3. 在拼接后的字符串末尾加上 `&SecretKey=YOUR_SECRET_KEY`
            string stringToSign;
            if (string.IsNullOrEmpty(canonicalQueryString))
            {
                stringToSign = $"SecretKey={this._secretKey}";
            }
            else
            {
                stringToSign = $"{canonicalQueryString}&SecretKey={this._secretKey}";
            }
            
            _logger.LogDebug("用于签名的源字符串: {StringToSign}", stringToSign);

            // 4. 对最终的字符串进行 HMAC-SHA256 哈希计算，并进行 Base64 编码
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(this._secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}