// TranslationController.cs (与Java对齐版)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // 确保引入 Newtonsoft.Json
using Newtonsoft.Json.Linq; // 【新增】引入 JObject, JArray 等
using PicTechApi.CSharp.Services;
using System.Text.Json.Serialization;

namespace PicTechApi.CSharp.Controllers
{
    // ... (所有 DTOs 保持不变) ...
    #region DTOs
    public class UrlTranslationRequest{ [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; } [JsonPropertyName("sourceLanguage")] public string? SourceLanguage { get; set; } [JsonPropertyName("targetLanguage")] public string? TargetLanguage { get; set; } }
    public class Base64TranslationRequest{ [JsonPropertyName("imageBase64")] public string? ImageBase64 { get; set; } [JsonPropertyName("sourceLanguage")] public string? SourceLanguage { get; set; } [JsonPropertyName("targetLanguage")] public string? TargetLanguage { get; set; } }
    public class UploadedImageRequest{ [JsonPropertyName("requestId")] public string? RequestId { get; set; } [JsonPropertyName("filename")] public string? Filename { get; set; } [JsonPropertyName("imageBase64")] public string? ImageBase64 { get; set; } }
    public class TranslationData{ [JsonPropertyName("FinalImageUrl")] public string? FinalImageUrl { get; set; } [JsonPropertyName("InPaintingUrl")] public string? InPaintingUrl { get; set; } [JsonPropertyName("SourceUrl")] public string? SourceUrl { get; set; } [JsonPropertyName("TemplateJson")] public string? TemplateJson { get; set; } }
    public class SaveStateRequest{ [JsonPropertyName("RequestId")] public string? RequestId { get; set; } [JsonPropertyName("Code")] public int Code { get; set; } [JsonPropertyName("Message")] public string? Message { get; set; } [JsonPropertyName("Data")] public TranslationData? Data { get; set; } }
    public class IopaintRequest { [JsonPropertyName("image")] public string Image { get; set; } [JsonPropertyName("mask")] public string Mask { get; set; } }
    public class UploadIoInpaintImageRequest { [JsonPropertyName("imageData")] public string ImageData { get; set; } }
    #endregion

    /// <summary>
    /// 提供图片翻译的 RESTful API 接口 (与 Java 版本行为对齐)
    /// </summary>
    [ApiController]
    [Route("api/translate")]
    public class TranslationController : ControllerBase
    {
        private readonly TranslationService _translationService;
        private readonly string? _uploadDir;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(TranslationService translationService, IConfiguration configuration, ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _uploadDir = configuration["FileUpload:UploadDir"];
            _logger = logger;
        }

        /// <summary>
        /// 接口1: 通过图片 URL 提交翻译任务
        /// </summary>
        [HttpPost("url")]
        public async Task<IActionResult> SubmitFromUrl([FromBody] UrlTranslationRequest request)
        {
            try
            {
                // 参数校验
                if (string.IsNullOrEmpty(request.ImageUrl) || string.IsNullOrEmpty(request.SourceLanguage) || string.IsNullOrEmpty(request.TargetLanguage))
                {
                    return BadRequest(new Dictionary<string, string> { { "error", "ImageUrl, sourceLanguage, 和 targetLanguage 均不能为空。" } });
                }
                var result = await _translationService.SubmitTaskFromUrlAsync(request.ImageUrl, request.SourceLanguage, request.TargetLanguage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 SubmitFromUrl 接口中发生错误。");
                // 返回与Java版本一致的错误结构
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", ex.Message } });
            }
        }

        /// <summary>
        /// 接口2: 通过 Base64 字符串提交翻译任务
        /// </summary>
        [HttpPost("base64")]
        public async Task<IActionResult> SubmitFromBase64([FromBody] Base64TranslationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageBase64) || string.IsNullOrEmpty(request.SourceLanguage) || string.IsNullOrEmpty(request.TargetLanguage))
                {
                    return BadRequest(new Dictionary<string, string> { { "error", "ImageBase64, sourceLanguage, 和 targetLanguage 均不能为空。" } });
                }
                var result = await _translationService.SubmitTaskFromBase64Async(request.ImageBase64, request.SourceLanguage, request.TargetLanguage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 SubmitFromBase64 接口中发生错误。");
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", ex.Message } });
            }
        }

        /// <summary>
        /// 接口3: 通过文件上传方式提交翻译任务
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> SubmitFromFileUpload([FromForm] IFormFile file, [FromForm] string sourceLanguage, [FromForm] string targetLanguage)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new Dictionary<string, string> { { "error", "上传文件不能为空" } });
            }
            try
            {
                var result = await _translationService.SubmitTaskFromFileAsync(file, sourceLanguage, targetLanguage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 SubmitFromFileUpload 接口中发生错误。");
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", ex.Message } });
            }
        }

        /// <summary>
        /// 接口: 保存编辑器画布状态
        /// </summary>
        [HttpPost("save")]
        public IActionResult SaveState([FromBody] SaveStateRequest request)
        {
            try
            {
                // 【核心改造】返回与Java版本完全一致的模拟响应
                if (request?.Data?.FinalImageUrl != null)
                {
                    _logger.LogInformation("接收到保存状态请求，FinalImageUrl: {FinalImageUrl}", request.Data.FinalImageUrl);
                }
                var mockResponse = new Dictionary<string, object>
                {
                    { "Code", 200 },
                    { "Message", "状态保存成功" },
                    { "RequestId", Guid.NewGuid().ToString() }
                };
                return Ok(mockResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 SaveState 接口中发生错误。");
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", $"服务器内部错误: {ex.Message}" } });
            }
        }

       /// <summary>
        /// 接口4: 查询翻译任务的结果 (已增加数据清洗和修复逻辑)
        /// </summary>
        [HttpGet("result/{requestId}")]
        public async Task<IActionResult> QueryResult(string requestId)
        {
            _logger.LogInformation("收到查询任务结果的请求，RequestId: {RequestId}", requestId);

            try
            {
                var originalResult = await _translationService.QueryTaskResultAsync(requestId);

                if (originalResult == null)
                {
                    return NotFound(new { error = "未找到指定任务的结果。" });
                }

                // LogResponseData($"[原始] 外部API响应 for {requestId}", originalResult);

                // --- 【核心修复】数据重塑与清洗 ---
                
                var finalResponse = new Dictionary<string, object?>();

                // 1. 提取顶层元数据
                finalResponse["Code"] = originalResult.GetValueOrDefault("Code");
                finalResponse["Message"] = originalResult.GetValueOrDefault("Message");
                finalResponse["RequestId"] = originalResult.GetValueOrDefault("RequestId");

                // 2. 清洗并设置 'Data' 对象
                if (originalResult.TryGetValue("Data", out var dataObject) && dataObject != null)
                {
                    // 将 object 转换为 JObject 以便灵活处理
                    var originalData = JObject.FromObject(dataObject);
                    var cleanDataObject = new Dictionary<string, object?>();

                    // 使用辅助方法清洗每一个URL字段
                    cleanDataObject["FinalImageUrl"] = ExtractAndCleanUrlValue(originalData, "FinalImageUrl");
                    cleanDataObject["InPaintingUrl"] = ExtractAndCleanUrlValue(originalData, "InPaintingUrl");
                    cleanDataObject["SourceUrl"] = ExtractAndCleanUrlValue(originalData, "SourceUrl");
                    cleanDataObject["TemplateJson"] = ExtractAndCleanUrlValue(originalData, "TemplateJson");

                    finalResponse["Data"] = cleanDataObject;
                }
                else
                {
                    finalResponse["Data"] = new Dictionary<string, object>(); // 默认空Data对象
                }
                
                // LogResponseData($"[修复后] 发送给前端的响应 for {requestId}", finalResponse);

                return Ok(finalResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 QueryResult 接口中发生错误。RequestId: {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", ex.Message } });
            }
        }


        /// <summary>
        /// 接口: 接收前端导出的 Base64 图片并保存到服务器
        /// </summary>
        [HttpPost("uploadExportedImage")]
        public async Task<IActionResult> UploadExportedImage([FromBody] UploadedImageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return BadRequest(new Dictionary<string, string> { { "error", "图片数据不能为空" } });
            }
            if (string.IsNullOrEmpty(_uploadDir))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "服务器未配置上传目录 (FileUpload:UploadDir)。" });
            }
            
            try
            {
                // 【核心改造】Java 版本中，Base64 字符串是不带前缀的，这里确保行为一致
                var base64Data = request.ImageBase64;
                var match = System.Text.RegularExpressions.Regex.Match(base64Data, @"^data:image\/[a-zA-Z]+;base64,");
                if (match.Success)
                {
                    base64Data = base64Data.Substring(match.Length);
                }

                var imageBytes = Convert.FromBase64String(base64Data);
                
                var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                var directoryPath = Path.Combine(_uploadDir, dateFolder);
                Directory.CreateDirectory(directoryPath);
                
                var originalFilename = string.IsNullOrWhiteSpace(request.Filename) ? "exported.png" : request.Filename;
                var fileExtension = Path.GetExtension(originalFilename);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                
                var filePath = Path.Combine(directoryPath, uniqueFileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                
                _logger.LogInformation("成功保存导出图片，路径: {FilePath}", filePath);
                
                var accessiblePath = $"/{dateFolder}/{uniqueFileName}".Replace('\\', '/');
                
                // 【核心改造】返回与Java版本一致的响应结构
                var responseBody = new Dictionary<string, object>
                {
                    { "message", "文件上传成功" },
                    { "filePath", accessiblePath }
                };
                return Ok(responseBody);
            }
            catch (FormatException)
            {
                return BadRequest(new Dictionary<string, string> { { "error", "无效的Base64数据" } });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "在 UploadExportedImage 中发生文件写入错误。");
                return StatusCode(StatusCodes.Status500InternalServerError, new Dictionary<string, string> { { "error", "文件保存失败，服务器IO错误" } });
            }
        }
        
        

         /// <summary>
        /// 辅助方法：从 JObject 中提取值，并将其从可能的数组格式清洗为字符串。
        /// </summary>
        private string? ExtractAndCleanUrlValue(JObject data, string key)
        {
            if (!data.TryGetValue(key, out var token))
            {
                // 键不存在，返回 null
                return null;
            }

            // 情况1: 值是一个数组 (例如 [])
            if (token is JArray array)
            {
                // 如果数组不为空，取第一个元素作为URL；否则返回 null
                return array.FirstOrDefault()?.ToString();
            }

            // 情况2: 值本身就是一个字符串
            if (token.Type == JTokenType.String)
            {
                return token.ToString();
            }
            
            // 其他所有情况 (null, object, etc.) 都视为无效，返回 null
            return null;
        }

        /// <summary>
        /// 辅助方法：记录响应日志 (保持不变)
        /// </summary>
        private void LogResponseData(string context, object? data)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
                _logger.LogInformation("--- 响应数据 Context: {Context} ---\n{JsonData}", context, jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "序列化响应数据以进行日志记录时失败。");
            }
        }

  #region 【新增接口】

        /// <summary>
        /// 【新增接口】代理图像擦除 (Inpainting) 请求
        /// </summary>
        [HttpPost("iopaint")]
        public async Task<IActionResult> PerformInpainting([FromBody] IopaintRequest request)
        {
            try
            {
                var newImageBase64 = await _translationService.IopaintAsync(request.Image, request.Mask);

                // 5. 将新的 Base64 字符串返回给前端
                return Ok(new { newImageBase64 });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Inpainting process failed");
                // 返回与Java版本一致的错误结构
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"图像擦除处理失败: {e.Message}" });
            }
        }

        /// <summary>
        /// 【重构接口】接收 Inpaint 后的 Base64 图片，保存到项目静态资源目录并返回可访问 URL
        /// </summary>
        [HttpPost("uploadIoInpaintImage")]
        public async Task<IActionResult> UploadIoInpaintImage([FromBody] UploadIoInpaintImageRequest request)
        {
            // 1. 参数校验
            if (string.IsNullOrWhiteSpace(request.ImageData))
            {
                return BadRequest(CreateErrorResponse("图片数据(imageData)不能为空", StatusCodes.Status400BadRequest));
            }

            try
            {
                var finalUrl = await _translationService.UploadIoInpaintImageAsync(request.ImageData);

             // 6. 返回标准格式的成功响应
                var successResponse = CreateSuccessResponse(finalUrl);

                // ===================== 【新增的日志记录】 =====================
                // 在返回之前，将要发送的对象序列化为JSON字符串并打印到日志
                _logger.LogInformation(
                    "准备返回成功响应给客户端，内容: {Response}",
                    JsonConvert.SerializeObject(successResponse, Formatting.Indented) // 使用 Indented 格式化输出，更易读
                );
                // ===================== 【日志记录结束】 =====================

                return Ok(successResponse);
            }
            catch (ArgumentException e) // 由服务层抛出的 Base64 格式错误
            {
                _logger.LogError(e, "Base64 解码失败。");
                return BadRequest(CreateErrorResponse("无效的Base64数据", StatusCodes.Status400BadRequest));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "文件保存时发生未知异常。");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateErrorResponse("文件保存失败，服务器IO错误"));
            }
        }

        #endregion

        // ... (私有辅助方法 ExtractAndCleanUrlValue, LogResponseData 保持不变) ...

        #region 辅助方法 (用于创建标准响应体)

        /// <summary>
        /// 创建一个标准的成功响应体
        /// </summary>
        private object CreateSuccessResponse(string url)
		{
		    // 【修改】使用 Dictionary 来精确控制键名
		    var data = new Dictionary<string, string>
		    {
		        { "Url", url }
		    };
            var responseBody = new Dictionary<string, object>
            {
                { "Code", 200 },
                { "Message", "文件上传成功" },
                { "Data", data }
            };
		    return responseBody;
		}

        /// <summary>
        /// 创建一个标准的错误响应体
        /// </summary>
        private object CreateErrorResponse(string errorMessage, int statusCode = StatusCodes.Status500InternalServerError)
        {
            var responseBody = new Dictionary<string, object>
            {
                { "Code", statusCode },
                { "Message", errorMessage },
                { "Data", (object)null }
            };
		    return responseBody;
        }

        #endregion

    }
}
