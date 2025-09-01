# PicTechApi.CSharp 项目说明

## 项目简介

`PicTechApi.CSharp` 是一个基于 ASP.NET Core 的后端服务项目，结合 Vue.js 前端，提供图片翻译功能。它与 Java 版本的行为对齐，支持通过图片 URL、Base64 字符串或文件上传提交翻译任务，并提供任务结果查询和画布状态保存功能。前端使用 `vue-pic-tech-editor` 插件实现图片编辑功能。

本项目适合需要将图片中的文字从一种语言翻译为另一种语言的场景，支持 RESTful API 接口，易于集成和扩展。

---

## 目录结构

```plaintext
PicTechApi.CSharp/
├── bin/                           # 编译输出目录
├── Clients/                       # API 客户端相关代码
├── Controllers/                   # 控制器目录
│   └── TranslationController.cs    # 图片翻译控制器
├── frontened/                     # 前端代码目录
│   ├── node_modules/              # Node.js 依赖
│   ├── public/                    # 前端静态资源
│   ├── src/                       # 前端源代码
│   │   ├── App.vue                # Vue 主组件
│   │   └── main.js                # Vue 入口文件
│   ├── package.json               # 前端依赖配置文件
│   ├── package-lock.json          # 依赖锁文件
│   └── vue.config.js              # Vue 构建配置
├── obj/                           # 编译临时文件
├── Properties/                    # 项目属性
│   └── launchSettings.json        # 启动配置
├── Services/                      # 服务层代码
├── wwwroot/                       # 前端构建输出目录
├── .gitignore                     # Git 忽略文件
├── appsettings.Development.json   # 开发环境配置
├── appsettings.json               # 应用配置文件
├── PicTechApi.CSharp.csproj       # 项目文件
├── PicTechApi.CSharp.sln         # 解决方案文件
├── Program.cs                     # 程序入口
└── Startup.cs                     # ASP.NET Core 启动配置
```

---

## 环境要求

- **后端**：
  - .NET Core SDK 3.1 或更高版本
  - Visual Studio 或 Visual Studio Code
- **前端**：
  - Node.js 14.x 或更高版本
  - Vue CLI 5.x
- **其他**：
  - 确保配置了上传目录（如 `/Users/liuhongjing/Downloads/uploads`）
  - 确保有权限访问 `appsettings.json` 中配置的 `PicTechApi:BaseUrl`

---

## 安装与配置

### 1. 克隆项目

```bash
git clone <repository-url>
cd PicTechApi.CSharp
```

### 2. 配置后端

1. **安装 .NET 依赖**：
   在项目根目录运行：
   ```bash
   dotnet restore
   ```

2. **配置 `appsettings.json`**：
   编辑 `appsettings.json` 文件，确保以下配置正确：
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning",
         "Microsoft.Hosting.Lifetime": "Information"
       }
     },
     "AllowedHosts": "*",
     "PicTechApi": {
       "BaseUrl": "http://your-api-base-url", // 替换为实际 API 地址
       "Key": "pic_EBD6CCBC",
       "Secret": "your-api-secret" // 替换为实际 Secret
     },
     "FileUpload": {
       "UploadDir": "/path/to/your/upload/directory" // 替换为实际上传目录
     }
   }
   ```

   - `PicTechApi:BaseUrl`：翻译服务 API 的基础地址。
   - `PicTechApi:Key` 和 `PicTechApi:Secret`：API 认证所需的密钥。
   - `FileUpload:UploadDir`：本地文件上传保存目录，确保该目录存在且有写入权限。

### 3. 配置前端

1. **进入前端目录**：
   ```bash
   cd frontened
   ```

2. **安装前端依赖**：
   ```bash
   npm install
   ```

3. **配置 API 地址**：
   编辑 `src/main.js`，确保 `userApiConfig` 中的 API 地址与后端一致：
   ```javascript
   const userApiConfig = {
     UPLOAD_API: '/api/translate/upload',处理文件上传的翻译任务。
     URL_API: '/api/translate/url',处理基于 URL 的翻译任务。
     RESULT_API: '/api/translate/result',查询翻译任务的处理结果。
     UPLOAD_EXPORT_IMG_API: '/api/translate/uploadExportedImage',**接收并保存前端导出的最终图片，建议定期清理**。
     SAVE_API: '/api/translate/save',保存编辑器当前画布状态。
     IO_IN_PAINT_API: '/api/translate/iopaint', 请求擦除服务，每5次请求消耗一个积分。
     UPLOAD_IO_IN_PAINT_IMG_API: '/api/translate/uploadIoInpaintImage', 保存图片中间结果，建议定期清理
   };
   ```

4. **构建前端**：
   ```bash
   npm run build
   ```
   构建后的文件将输出到 `wwwroot` 目录。

### 4. 运行项目

1. **启动后端**：
   在项目根目录运行：
   ```bash
   dotnet run
   ```
   默认情况下，服务将运行在 `http://localhost:5256`。

2. **访问应用**：
   打开浏览器，访问 `http://localhost:5256`（或配置的端口），即可看到前端界面。

---

## API 使用说明

以下是 `TranslationController` 提供的 RESTful API 接口：

### 1. 提交翻译任务（URL）
- **URL**: `/api/translate/url`
- **方法**: POST
- **请求体**:
  ```json
  {
    "imageUrl": "http://example.com/image.jpg",
    "sourceLanguage": "en",
    "targetLanguage": "zh"
  }
  ```
- **响应**:
  - 成功：返回任务 ID 和状态
  - 失败：返回错误信息

### 2. 提交翻译任务（Base64）
- **URL**: `/api/translate/base64`
- **方法**: POST
- **请求体**:
  ```json
  {
    "imageBase64": "data:image/png;base64,...",
    "sourceLanguage": "en",
    "targetLanguage": "zh"
  }
  ```
- **响应**:
  - 成功：返回任务 ID 和状态
  - 失败：返回错误信息

### 3. 提交翻译任务（文件上传）
- **URL**: `/api/translate/upload`
- **方法**: POST
- **请求体**: FormData 格式，包含：
  - `file`: 图片文件
  - `sourceLanguage`: 源语言代码
  - `targetLanguage`: 目标语言代码
- **响应**:
  - 成功：返回任务 ID 和状态
  - 失败：返回错误信息

### 4. 查询翻译结果
- **URL**: `/api/translate/result/{requestId}`
- **方法**: GET
- **参数**: `requestId`（任务 ID）
- **响应**:
  ```json
  {
    "Code": 200,
    "Message": "Success",
    "RequestId": "xxx",
    "Data": {
      "FinalImageUrl": "http://example.com/final.jpg",
      "InPaintingUrl": "http://example.com/inpainting.jpg",
      "SourceUrl": "http://example.com/source.jpg",
      "TemplateJson": "{}"
    }
  }
  ```

### 5. 保存画布状态
- **URL**: `/api/translate/save`
- **方法**: POST
- **请求体**:
  ```json
  {
    "RequestId": "xxx",
    "Code": 200,
    "Message": "Success",
    "Data": {
      "FinalImageUrl": "http://example.com/final.jpg",
      "InPaintingUrl": "http://example.com/inpainting.jpg",
      "SourceUrl": "http://example.com/source.jpg",
      "TemplateJson": "{}"
    }
  }
  ```
- **响应**:
  ```json
  {
    "Code": 200,
    "Message": "状态保存成功",
    "RequestId": "xxx"
  }
  ```

### 6. 上传导出的图片
- **URL**: `/api/translate/uploadExportedImage`
- **方法**: POST
- **请求体**:
  ```json
  {
    "requestId": "xxx",
    "filename": "exported.png",
    "imageBase64": "data:image/png;base64,..."
  }
  ```
- **响应**:
  ```json
  {
    "message": "文件上传成功",
    "filePath": "/yyyy-MM-dd/exported.png"
  }
  ```

---

## 常见问题

1. **CORS 错误怎么办？**
   项目已配置允许所有来源的 CORS 策略。如果仍遇到问题，检查 `Startup.cs` 中的 `UseCors()` 是否正确启用。

2. **前端插件未加载怎么办？**
   确保 `vue-pic-tech-editor` 已正确安装，并且 `main.js` 中导入了 CSS 文件：
   ```javascript
   import 'vue-pic-tech-editor/dist/vue-pic-tech-editor.css';
   ```

3. **API 请求失败怎么办？**
   - 检查 `appsettings.json` 中的 `PicTechApi:BaseUrl` 是否正确。
   - 确保网络连接正常，且 API 密钥有效。

---

## 开发与调试

- **调试后端**：
  使用 Visual Studio 或 VS Code 附加到 `dotnet run` 进程，设置断点调试 `TranslationController.cs`。

- **调试前端**：
  在 `frontened` 目录运行：
  ```bash
  npm run serve
  ```
  前端将运行在 `http://localhost:8080`，支持热更新。

- **日志**：
  后端日志通过 `ILogger` 记录，查看控制台或配置日志文件（如 Serilog）以获取详细信息。

---

## 贡献

欢迎提交 Issue 或 Pull Request！请确保代码风格与现有代码一致，并附上必要的测试。