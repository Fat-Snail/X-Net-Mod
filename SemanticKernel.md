# SemanticKernel - .NET 10 语义内核框架

## 简介

SemanticKernel 是微软推出的一个轻量级开源 SDK，旨在将大型语言模型（LLM）与传统编程语言（如 C#）无缝集成。本示例基于 .NET 10 平台，演示如何使用 SemanticKernel 组件与本地 AI 模型（通过 Ollama）进行交互。

## 环境准备

### 必需组件

- **.NET 运行时**：.NET 10 或更高版本
- **SemanticKernel NuGet 包**：
  ```bash
  dotnet add package Microsoft.SemanticKernel
  ```

### 本地 AI 服务（Ollama）

1. **安装 Ollama**
   
   访问 [Ollama 官网](https://ollama.com/) 下载并安装适合您操作系统的版本。

2. **启动 Ollama 服务**
   
   ```bash
   ollama serve
   ```

3. **下载模型**
   
   ```bash
   ollama pull llama3.2:3b
   ```

4. **验证安装**
   
   ```bash
   ollama list
   ```

## 功能演示

### 演示一：基础示例（BasicExample）

展示 SemanticKernel 的基本用法，包括创建 Kernel 实例、添加插件、调用内置功能。

#### 核心代码

```csharp
public class BasicExample
{
    public static async Task RunAsync()
    {
#pragma warning disable SKEXP0050
        // 创建Semantic Kernel实例
        var builder = Kernel.CreateBuilder();
        
        // 配置日志服务
        builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // 添加内置插件
        builder.Plugins.AddFromType<TimePlugin>("Time");
        
        // 构建Kernel实例
        var kernel = builder.Build();
#pragma warning restore SKEXP0050

        // 输出欢迎信息
        Console.WriteLine("Semantic Kernel 基础示例");
        Console.WriteLine("========================\n");

        // 演示使用TimePlugin获取当前时间
        var timeResult = await kernel.InvokeAsync<string>("Time", "Date");
        Console.WriteLine($"当前日期: {timeResult}");

        // 提示：要使用Prompt功能，需要配置AI服务（如OpenAI、Azure OpenAI等）
        Console.WriteLine("提示：要使用Prompt功能，需要配置AI服务（如OpenAI、Azure OpenAI等）\n");

        Console.WriteLine("\n基础示例执行完成！");
    }
}
```

#### 功能说明

- **Kernel 创建**：使用 `Kernel.CreateBuilder()` 创建构建器
- **日志配置**：添加控制台日志输出
- **插件注册**：内置 `TimePlugin` 提供时间相关功能
- **函数调用**：使用 `InvokeAsync` 调用插件方法

### 演示二：高级功能示例（AdvancedExample）

展示 SemanticKernel 的高级功能，包括自定义 Prompt、函数管道、自定义函数注册等。

#### 核心代码

```csharp
public class AdvancedExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Semantic Kernel 高级功能示例");
        Console.WriteLine("==============================\n");

#pragma warning disable SKEXP0050
        // 创建Kernel实例
        var builder = Kernel.CreateBuilder();
        
        // 配置日志服务
        builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // 添加内置插件
        builder.Plugins.AddFromType<TimePlugin>("Time");
        builder.Plugins.AddFromType<MathPlugin>("Math");
        
        // 构建Kernel实例
        var kernel = builder.Build();
#pragma warning restore SKEXP0050

        // 示例1：自定义Prompt函数
        Console.WriteLine("示例1: 自定义Prompt函数");
        Console.WriteLine("------------------------");
        Console.WriteLine("提示：此功能需要配置AI服务（如OpenAI、Azure OpenAI等）\n");

        // 示例2：使用管道执行多个函数
        Console.WriteLine("示例2: 函数管道执行");
        Console.WriteLine("--------------------");
        
        // 执行数学运算
        var mathResult = await kernel.InvokeAsync<int>(
            "Math", 
            "Add", 
            new() 
            { 
                ["value"] = "5", 
                ["amount"] = "7" 
            }
        );
        Console.WriteLine($"数学运算结果 (5 + 7): {mathResult}\n");

        // 示例3：注册自定义函数
        Console.WriteLine("示例3: 自定义函数");
        Console.WriteLine("------------------");
        
        // 定义一个自定义函数
        var customFunction = kernel.CreateFunctionFromMethod(() => 
        {
            return $"这是一个由Semantic Kernel执行的自定义函数，当前时间: {DateTime.Now}";
        }, "GetCurrentInfo", "返回当前时间和自定义信息");
        
        var plugin = KernelPluginFactory.CreateFromFunctions("CustomFunctions", "自定义函数集合", new[] { customFunction });
        kernel.Plugins.Add(plugin);
        
        var customResult = await kernel.InvokeAsync("CustomFunctions", "GetCurrentInfo");
        Console.WriteLine($"自定义函数结果: {customResult}\n");

        // 示例4：使用变量和上下文
        Console.WriteLine("示例4: 变量和上下文管理");
        Console.WriteLine("--------------------------");
        Console.WriteLine("提示：此功能需要配置AI服务（如OpenAI、Azure OpenAI等）\n");

        // 示例5：使用内置插件组合
        Console.WriteLine("示例5: 插件组合使用");
        Console.WriteLine("---------------------");
        
        Console.WriteLine("提示：此功能需要配置AI服务（如OpenAI、Azure OpenAI等）\n");

        Console.WriteLine("高级功能示例执行完成！");
    }
}
```

#### 功能说明

1. **自定义 Prompt 函数**：支持自然语言提示词
2. **函数管道**：链式调用多个函数
3. **自定义函数注册**：将 C# 方法转换为 Kernel 函数
4. **变量和上下文管理**：在多个函数间传递数据
5. **插件组合**：结合多个插件实现复杂功能

### 演示三：本地 AI 模型使用（LocalAIUsageDemo）

展示如何实际连接和使用本地 AI 模型（Ollama + llama3.2:3b）。

#### 核心代码

```csharp
/// <summary>
/// 本地AI模型使用演示
/// 此类展示如何实际连接和使用本地AI模型
/// </summary>
public class LocalAIUsageDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("================================");
        Console.WriteLine("本地AI模型实际使用演示");
        Console.WriteLine("================================\n");

        Console.WriteLine("注意：要运行此演示，您需要：");
        Console.WriteLine("1. 启动本地AI服务器（如Ollama）");
        Console.WriteLine("2. 运行命令: ollama serve");
        Console.WriteLine("3. 下载模型: ollama pull llama3.2:3b");
        Console.WriteLine("4. 然后取消注释下面的相关代码\n");

        // 演示如何连接到本地AI服务
        await DemoConnectionToOllama();
        
        Console.WriteLine("本地AI模型使用演示完成！");
    }

    /// <summary>
    /// 演示连接到Ollama本地AI服务器
    /// </summary>
    private static async Task DemoConnectionToOllama()
    {
        try
        {
            // 检查本地服务器是否可用
            bool isServerAvailable = await IsLocalAIServerAvailable();
            
            if (!isServerAvailable)
            {
                Console.WriteLine("⚠️  本地AI服务器未运行");
                Console.WriteLine("   请先启动本地AI服务器（如Ollama）");
                Console.WriteLine("   示例命令: ollama serve\n");
                return;
            }

            Console.WriteLine("✅ 本地AI服务器可用，创建Kernel实例...\n");

            // 创建连接到本地AI服务的Kernel
            var builder = Kernel.CreateBuilder();
            
            // 添加日志服务
            builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

            // 添加核心插件
            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<MathPlugin>("Math");

            // 连接到本地Ollama服务器
            // 使用兼容OpenAI API的配置
            builder.AddOpenAIChatCompletion(
                modelId: "llama3.2:3b",  // 使用诊断工具确认的模型名称
                apiKey: "ollama",       // Ollama使用占位符密钥
                endpoint: new Uri("http://localhost:11434/v1")  // Ollama兼容OpenAI API的端点
            );

            var kernel = builder.Build();

            Console.WriteLine("1. 成功连接到本地AI模型！\n");

            // 演示使用本地AI处理简单请求
            Console.WriteLine("2. 演示使用本地AI处理请求：");
            
            // 示例1: 简单的问答
            var simpleQuestion = "什么是人工智能？请用100字以内回答。";
            Console.WriteLine($"   问题: {simpleQuestion}");
            
            try
            {
                var result = await kernel.InvokePromptAsync(simpleQuestion);
                Console.WriteLine($"   本地AI回答: {result.GetValue<string>()}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   本地AI响应失败: {ex.Message}\n");
            }

            // 示例2: 结合插件功能
            Console.WriteLine("3. 演示结合插件功能：");
            
            var complexPrompt = @"
今天的日期是 {{Time.Date}}。
请根据今天的日期，用一句话表达对未来的期望。
";
            Console.WriteLine($"   复杂提示: {complexPrompt.Trim()}");
            
            try
            {
                var complexResult = await kernel.InvokePromptAsync(complexPrompt);
                Console.WriteLine($"   本地AI响应: {complexResult.GetValue<string>()}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   本地AI响应失败: {ex.Message}\n");
            }

            // 示例3: 使用函数调用
            Console.WriteLine("4. 演示函数调用结合AI处理：");
            
            // 先使用插件计算
            var calculationResult = await kernel.InvokeAsync<int>("Math", "Add", new() 
            { 
                ["value"] = "10", 
                ["amount"] = "20" 
            });
            
            Console.WriteLine($"   插件计算结果: 10 + 20 = {calculationResult}");
            
            // 然后让AI解释结果
            var explanationPrompt = $"请解释数字 {calculationResult} 在日常生活中的意义。";
            try
            {
                var explanation = await kernel.InvokePromptAsync(explanationPrompt);
                Console.WriteLine($"   本地AI解释: {explanation.GetValue<string>()}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   本地AI解释失败: {ex.Message}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接本地AI服务失败: {ex.Message}");
            Console.WriteLine("请确保本地AI服务器正在运行并且配置正确。\n");
        }
    }

    /// <summary>
    /// 检查本地AI服务器是否可用
    /// </summary>
    private static async Task<bool> IsLocalAIServerAvailable()
    {
        try
        {
            // 这里可以添加实际的健康检查逻辑
            // 目前只是演示目的，实际应用中需要真正检查服务器连接
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5); // 设置超时
            
            // 尝试连接到本地Ollama服务器
            // var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
            // return response.IsSuccessStatusCode;
            
            // 由于这只是演示，返回true以展示代码结构
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

#### 功能说明

1. **连接本地 AI 服务**：通过 OpenAI 兼容的 API 连接 Ollama
2. **简单问答**：演示基本的自然语言交互
3. **插件结合**：将插件数据传递给 AI 进行处理
4. **函数调用**：结合传统函数调用和 AI 处理
5. **健康检查**：检测本地服务可用性

## 配置说明

### Ollama 连接配置

```csharp
builder.AddOpenAIChatCompletion(
    modelId: "llama3.2:3b",                              // 模型名称
    apiKey: "ollama",                                      // 占位符密钥
    endpoint: new Uri("http://localhost:11434/v1")        // Ollama API 端点
);
```

### 内置插件

SemanticKernel 提供了多个内置插件：

| 插件 | 功能 | 方法示例 |
|------|------|----------|
| `TimePlugin` | 时间相关 | `Date`, `UtcNow`, `TimeOfDay` |
| `MathPlugin` | 数学运算 | `Add`, `Subtract`, `Multiply`, `Divide` |
| `FileIOPlugin` | 文件操作 | `Read`, `Write`, `Append` |

## 运行示例

### 运行基础示例

```bash
dotnet run --project YourProject.csproj -- --example basic
```

### 运行高级示例

```bash
dotnet run --project YourProject.csproj -- --example advanced
```

### 运行本地 AI 示例

```bash
# 1. 先启动 Ollama 服务
ollama serve

# 2. 在另一个终端运行示例
dotnet run --project YourProject.csproj -- --example localai
```

## 常见问题

### Q: 为什么需要 `#pragma warning disable SKEXP0050`？

A: `SKEXP0050` 是 SemanticKernel 的实验性 API 警告代码。使用此指令可以抑制相关警告，表示您了解正在使用实验性功能。

### Q: Ollama 支持哪些模型？

A: Ollama 支持多种开源模型，包括：
- Llama 系列：llama3.2:3b, llama3.2:1b, llama3.1
- Mistral 系列：mistral, mixtral
- 其他：gemma, phi3, qwen 等

### Q: 如何更改使用的模型？

A: 修改 `AddOpenAIChatCompletion` 中的 `modelId` 参数：

```csharp
builder.AddOpenAIChatCompletion(
    modelId: "your-model-name",  // 更改为您想使用的模型
    apiKey: "ollama",
    endpoint: new Uri("http://localhost:11434/v1")
);
```

### Q: 本地 AI 模型性能如何？

A: 性能取决于：
- **硬件配置**：GPU 加速可显著提升速度
- **模型大小**：较小的模型（如 3B）响应更快
- **系统资源**：确保有足够的内存和 CPU 资源

### Q: 如何查看 Ollama 服务是否正常运行？

A: 使用以下命令：

```bash
# 查看服务状态
curl http://localhost:11434/api/tags

# 列出已下载的模型
ollama list

# 测试模型运行
ollama run llama3.2:3b "Hello, World!"
```

## 最佳实践

1. **资源管理**：及时释放 Kernel 实例和 HTTP 客户端
2. **错误处理**：添加适当的 try-catch 块处理网络和 AI 响应错误
3. **日志记录**：启用详细日志便于调试和问题追踪
4. **模型选择**：根据任务需求选择合适的模型大小
5. **超时设置**：为 HTTP 请求设置合理的超时时间
6. **缓存机制**：对于重复的查询结果考虑使用缓存

## 扩展功能

### 自定义插件

```csharp
public class MyCustomPlugin
{
    [KernelFunction("greet")]
    [Description("向用户打招呼")]
    public static string Greet(string name)
    {
        return $"Hello, {name}! Welcome to SemanticKernel.";
    }
}

// 注册自定义插件
builder.Plugins.AddFromType<MyCustomPlugin>("MyCustom");
```

### Prompt 模板

```csharp
var prompt = @"
用户输入: {{$userInput}}
请根据以下规则处理输入:
1. 识别用户意图
2. 提取关键信息
3. 提供有用的回应

回复:";
```

## 参考资料

- [SemanticKernel 官方文档](https://learn.microsoft.com/semantic-kernel/)
- [SemanticKernel GitHub 仓库](https://github.com/microsoft/semantic-kernel)
- [Ollama 官网](https://ollama.com/)
- [Llama 3.2 模型文档](https://llama.meta.com/)

## 作者信息

- **技术栈**：.NET 10 + SemanticKernel + Ollama
- **模型**：llama3.2:3b
- **适用场景**：本地 AI 开发、原型验证、离线应用

---

> **提示**：本示例展示了 SemanticKernel 的基本用法和本地 AI 集成能力。在实际生产环境中，建议：
> - 添加更完善的错误处理和重试机制
> - 实现模型缓存和预热
> - 配置监控和性能指标收集
> - 根据实际需求选择合适的云端或本地 AI 服务