#!/usr/bin/env dotnet-script

/*
 * mini-dll.cs - 基于.NET 10最新运行单个C#源文件的功能
 * 作者：Fatty Coder
 * 版权所有 © 2024-2025 Fatty Coder
 * 官网：https://github.com/Fat-Snail
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ================ 主程序入口 ================
try
{
    if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
    {
        PrintHelp();
        return 0;
    }

    Console.WriteLine("🔧 .NET 程序集清理工具");
    Console.WriteLine(new string('═', 50));
    
    // 交互式配置
    var options = InteractivelyConfigureOptions(args);
    if (options == null) return 0;
    
    Console.Clear();
    PrintConfiguration(options);
    
    // 分析目录
    var result = AnalyzeDirectory(options);
    DisplayAnalysisResult(result, options);
    
    // 交互式操作
    return InteractivelyProcessResult(result, options);
}
catch (Exception ex)
{
    // 这里需要处理options可能为null的情况
    Console.WriteLine($"\n❌ 错误: {ex.Message}");
    Console.WriteLine($"堆栈: {ex.StackTrace}");
    return 1;
}

// ================ 交互式配置 ================
CleanOptions? InteractivelyConfigureOptions(string[] args)
{
    var options = new CleanOptions();
    
    // 尝试从命令行参数解析
    ParseCommandLineArgs(args, options);
    
    // 如果命令行已经指定了操作，直接返回
    if (options.Mode != OperationMode.Interactive)
        return options;
    
    Console.WriteLine("\n🎯 请选择操作模式:");
    Console.WriteLine("  1. 🔍 仅分析（查看可清理的内容）");
    Console.WriteLine("  2. ✂️  预精简（模拟清理效果）");
    Console.WriteLine("  3. 🗑️  直接清理（执行删除操作）");
    Console.WriteLine("  4. 📊 详细报告（生成分析报告）");
    Console.WriteLine("  5. ❌ 退出");
    
    Console.Write("\n请选择 (1-5): ");
    var modeChoice = Console.ReadLine();
    
    switch (modeChoice)
    {
        case "1":
            options.Mode = OperationMode.AnalyzeOnly;
            break;
        case "2":
            options.Mode = OperationMode.PreviewCleanup;
            break;
        case "3":
            options.Mode = OperationMode.DeleteFiles;
            break;
        case "4":
            options.Mode = OperationMode.GenerateReport;
            break;
        default:
            return null;
    }
    
    // 配置目录
    if (string.IsNullOrEmpty(options.Directory))
    {
        Console.Write($"\n📁 请输入要分析的目录 [当前目录: {Directory.GetCurrentDirectory()}]: ");
        var dir = Console.ReadLine();
        options.Directory = string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir;
    }
    
    // 配置入口文件
    if (string.IsNullOrEmpty(options.EntryPoint))
    {
        Console.Write($"\n🎯 请输入入口程序集名称（或按回车自动检测）: ");
        var entry = Console.ReadLine();
        options.EntryPoint = string.IsNullOrWhiteSpace(entry) ? "*" : entry;
    }
    
    // 配置白名单
    Console.Write("\n📝 请输入白名单（逗号分隔，按回车跳过）: ");
    var whitelistInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(whitelistInput))
    {
        options.Whitelist = whitelistInput.Split(',', ';')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
    
    // 配置其他选项
    Console.Write("\n🔍 是否递归扫描子目录？(y/N): ");
    options.Recursive = Console.ReadKey().Key == ConsoleKey.Y;
    Console.WriteLine();
    
    if (options.Mode != OperationMode.AnalyzeOnly)
    {
        Console.Write("🌍 是否清理语言包文件？(y/N): ");
        options.CleanLocale = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();
        
        Console.Write("📄 是否清理其他文件(.pdb/.xml等)？(y/N): ");
        options.CleanOther = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();
    }
    
    return options;
}

void ParseCommandLineArgs(string[] args, CleanOptions options)
{
    if (args.Length == 0)
    {
        options.Mode = OperationMode.Interactive;
        return;
    }
    
    options.Mode = OperationMode.DeleteFiles; // 命令行默认直接执行
    
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--analyze":
            case "-a":
                options.Mode = OperationMode.AnalyzeOnly;
                break;
            case "--preview":
            case "-p":
                options.Mode = OperationMode.PreviewCleanup;
                break;
            case "--report":
                options.Mode = OperationMode.GenerateReport;
                break;
            case "--dir":
            case "-d":
                if (i + 1 < args.Length) options.Directory = args[++i];
                break;
            case "--entry":
            case "-e":
                if (i + 1 < args.Length) options.EntryPoint = args[++i];
                break;
            case "--whitelist":
            case "-w":
                if (i + 1 < args.Length)
                {
                    options.Whitelist = args[++i].Split(',', ';')
                        .Select(s => s.Trim())
                        .ToList();
                }
                break;
            case "--recursive":
            case "-r":
                options.Recursive = true;
                break;
            case "--no-locale":
                options.CleanLocale = true;
                break;
            case "--clean-other":
                options.CleanOther = true;
                break;
            case "--verbose":
            case "-v":
                options.Verbose = true;
                break;
        }
    }
    
    if (string.IsNullOrEmpty(options.Directory))
        options.Directory = Directory.GetCurrentDirectory();
    
    if (string.IsNullOrEmpty(options.EntryPoint))
        options.EntryPoint = "*";
}

void PrintConfiguration(CleanOptions options)
{
    Console.WriteLine("════════════════════ 配置信息 ════════════════════");
    Console.WriteLine($"📁 分析目录: {Path.GetFullPath(options.Directory)}");
    Console.WriteLine($"🎯 入口程序: {options.EntryPoint}");
    Console.WriteLine($"🔍 扫描模式: {(options.Recursive ? "递归扫描" : "当前目录")}");
    Console.WriteLine($"🗑️  操作模式: {GetModeDescription(options.Mode)}");
    Console.WriteLine($"📝 白名单: {(options.Whitelist.Any() ? string.Join(", ", options.Whitelist) : "无")}");
    if (options.Mode != OperationMode.AnalyzeOnly)
    {
        Console.WriteLine($"🌍 清理语言包: {(options.CleanLocale ? "是" : "否")}");
        Console.WriteLine($"📄 清理其他文件: {(options.CleanOther ? "是" : "否")}");
    }
    Console.WriteLine(new string('═', 50));
}

string GetModeDescription(OperationMode mode) => mode switch
{
    OperationMode.AnalyzeOnly => "仅分析",
    OperationMode.PreviewCleanup => "预精简",
    OperationMode.DeleteFiles => "直接清理",
    OperationMode.GenerateReport => "生成报告",
    _ => "交互式"
};

// ================ 核心分析逻辑 ================
AnalysisResult AnalyzeDirectory(CleanOptions options)
{
    var result = new AnalysisResult();
    var dir = Path.GetFullPath(options.Directory);
    
    if (!Directory.Exists(dir))
        throw new DirectoryNotFoundException($"目录不存在: {dir}");
    
    // 查找入口文件
    var entryFile = FindEntryFile(dir, options.EntryPoint);
    if (entryFile == null)
        throw new FileNotFoundException($"找不到入口文件: {options.EntryPoint}");
    
    result.EntryFile = entryFile;
    
    // 获取所有文件
    var allFiles = Directory.GetFiles(dir, "*.*", 
            options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
        .ToList();
    
    // 统计原始状态
    result.TotalFiles = allFiles.Count;
    result.TotalSize = allFiles.Sum(f => new FileInfo(f).Length);
    
    // 分析程序集依赖
    var usedAssemblies = FindUsedAssemblies(entryFile, dir, options.Recursive);
    result.UsedAssembliesCount = usedAssemblies.Count;
    
    // 分类文件
    var assemblies = new List<FileInfo>();
    var localeFiles = new List<FileInfo>();
    var otherFiles = new List<FileInfo>();
    
    foreach (var file in allFiles)
    {
        var info = new FileInfo(file);
        var ext = info.Extension.ToLower();
        var name = Path.GetFileNameWithoutExtension(file);
        
        if (ext == ".dll" || ext == ".exe")
        {
            if (IsLocaleFile(file))
            {
                localeFiles.Add(info);
                result.OriginalLocaleSize += info.Length;
                result.OriginalLocaleCount++;
            }
            else
            {
                assemblies.Add(info);
                if (!usedAssemblies.Contains(name) &&
                    !IsWhitelisted(name, options.Whitelist) &&
                    !IsSystemAssembly(name))
                {
                    result.UnusedAssemblies.Add(info);
                    result.UnusedSize += info.Length;
                }
            }
        }
        else if (options.CleanOther && ShouldCleanOtherFile(file))
        {
            otherFiles.Add(info);
            result.OtherSize += info.Length;
        }
    }
    
    result.OriginalAssemblyCount = assemblies.Count;
    result.OriginalAssemblySize = assemblies.Sum(f => f.Length);
    
    // 计算可清理的语言包
    if (options.CleanLocale)
    {
        result.LocaleFiles = localeFiles
            .Where(f => !IsWhitelisted(Path.GetFileNameWithoutExtension(f.Name), options.Whitelist))
            .ToList();
        result.LocaleSize = result.LocaleFiles.Sum(f => f.Length);
    }
    
    // 计算总可清理大小
    result.TotalSavableSize = result.UnusedSize + result.LocaleSize + result.OtherSize;
    result.TotalSavableCount = result.UnusedAssemblies.Count + result.LocaleFiles.Count + otherFiles.Count;
    
    return result;
}

// ================ 显示分析结果 ================
void DisplayAnalysisResult(AnalysisResult result, CleanOptions options)
{
    Console.WriteLine("📊 分析结果摘要");
    Console.WriteLine(new string('─', 50));
    
    // 显示原始状态
    Console.WriteLine($"📦 原始状态:");
    Console.WriteLine($"   • 文件总数: {result.TotalFiles:N0} 个");
    Console.WriteLine($"   • 总大小: {FormatSize(result.TotalSize)}");
    Console.WriteLine($"   • 程序集文件: {result.OriginalAssemblyCount:N0} 个 ({FormatSize(result.OriginalAssemblySize)})");
    Console.WriteLine($"   • 语言包文件: {result.OriginalLocaleCount:N0} 个 ({FormatSize(result.OriginalLocaleSize)})");
    Console.WriteLine($"   • 被引用程序集: {result.UsedAssembliesCount:N0} 个");
    
    // 显示可清理的内容
    if (result.TotalSavableCount > 0)
    {
        Console.WriteLine($"\n🗑️  可清理内容:");
        
        if (result.UnusedAssemblies.Any())
        {
            Console.WriteLine($"   • 未使用程序集: {result.UnusedAssemblies.Count:N0} 个 ({FormatSize(result.UnusedSize)})");
            if (options.Mode == OperationMode.PreviewCleanup || options.Mode == OperationMode.AnalyzeOnly)
            {
                Console.WriteLine("     包括:");
                foreach (var file in result.UnusedAssemblies.Take(5).OrderBy(f => f.Name))
                {
                    Console.WriteLine($"       - {file.Name} ({FormatSize(file.Length)})");
                }
                if (result.UnusedAssemblies.Count > 5)
                    Console.WriteLine($"       ... 还有 {result.UnusedAssemblies.Count - 5} 个文件");
            }
        }
        
        if (result.LocaleFiles.Any() && options.CleanLocale)
        {
            Console.WriteLine($"   • 语言包文件: {result.LocaleFiles.Count:N0} 个 ({FormatSize(result.LocaleSize)})");
        }
        
        if (result.OtherSize > 0 && options.CleanOther)
        {
            Console.WriteLine($"   • 其他文件: {FormatSize(result.OtherSize)}");
        }
        
        // 显示精简后状态
        var remainingSize = result.TotalSize - result.TotalSavableSize;
        var remainingCount = result.TotalFiles - result.TotalSavableCount;
        
        Console.WriteLine($"\n✨ 预精简后状态:");
        Console.WriteLine($"   • 剩余文件数: {remainingCount:N0} 个");
        Console.WriteLine($"   • 剩余大小: {FormatSize(remainingSize)}");
        Console.WriteLine($"   • 精简比例: {((double)result.TotalSavableSize / result.TotalSize * 100):F1}%");
        
        Console.WriteLine($"\n💾 可释放空间: {FormatSize(result.TotalSavableSize)}");
    }
    else
    {
        Console.WriteLine($"\n✅ 未发现可清理的文件");
    }
    
    Console.WriteLine(new string('═', 50));
}

// ================ 交互式处理结果 ================
int InteractivelyProcessResult(AnalysisResult result, CleanOptions options)
{
    if (result.TotalSavableCount == 0)
        return 0;
    
    switch (options.Mode)
    {
        case OperationMode.AnalyzeOnly:
            Console.WriteLine("🔍 分析完成，未执行任何清理操作。");
            Console.WriteLine("   使用 --preview 查看详细清理效果，或 --delete 执行清理。");
            break;
            
        case OperationMode.PreviewCleanup:
            return PreviewCleanup(result, options);
            
        case OperationMode.DeleteFiles:
            return ExecuteCleanup(result, options);
            
        case OperationMode.GenerateReport:
            GenerateReport(result, options);
            break;
    }
    
    return 0;
}

int PreviewCleanup(AnalysisResult result, CleanOptions options)
{
    Console.WriteLine("🔍 预精简模式 - 显示将要清理的文件");
    Console.WriteLine(new string('─', 50));
    
    if (result.UnusedAssemblies.Any())
    {
        Console.WriteLine($"\n🗑️  将要删除的未使用程序集 ({result.UnusedAssemblies.Count}个):");
        foreach (var file in result.UnusedAssemblies.OrderBy(f => f.Name))
        {
            var relPath = GetRelativePath(file.FullName, options.Directory);
            Console.WriteLine($"   • {relPath} ({FormatSize(file.Length)})");
        }
    }
    
    if (result.LocaleFiles.Any() && options.CleanLocale)
    {
        Console.WriteLine($"\n🌍 将要删除的语言包文件 ({result.LocaleFiles.Count}个):");
        foreach (var file in result.LocaleFiles.Take(10).OrderBy(f => f.Name))
        {
            var relPath = GetRelativePath(file.FullName, options.Directory);
            Console.WriteLine($"   • {relPath} ({FormatSize(file.Length)})");
        }
        if (result.LocaleFiles.Count > 10)
            Console.WriteLine($"   ... 还有 {result.LocaleFiles.Count - 10} 个文件");
    }
    
    Console.WriteLine($"\n📋 总计: {result.TotalSavableCount} 个文件，{FormatSize(result.TotalSavableSize)}");
    Console.WriteLine($"\n💡 提示: 使用 --delete 参数来执行实际清理操作");
    
    return 0;
}

int ExecuteCleanup(AnalysisResult result, CleanOptions options)
{
    if (result.TotalSavableCount == 0)
    {
        Console.WriteLine("✅ 没有需要清理的文件");
        return 0;
    }
    
    Console.WriteLine($"\n⚠️  即将删除 {result.TotalSavableCount} 个文件，释放 {FormatSize(result.TotalSavableSize)} 空间");
    Console.Write("是否继续？(y/N): ");
    
    if (Console.ReadKey().Key != ConsoleKey.Y)
    {
        Console.WriteLine("\n❌ 操作已取消");
        return 0;
    }
    
    Console.WriteLine("\n");
    
    int deleted = 0, failed = 0;
    
    // 删除未使用的程序集
    foreach (var file in result.UnusedAssemblies)
    {
        if (DeleteFileSafe(file.FullName))
        {
            deleted++;
            Console.WriteLine($"✅ 删除程序集: {file.Name}");
        }
        else
        {
            failed++;
        }
    }
    
    // 删除语言包
    if (options.CleanLocale)
    {
        foreach (var file in result.LocaleFiles)
        {
            if (DeleteFileSafe(file.FullName))
            {
                deleted++;
                Console.WriteLine($"✅ 删除语言包: {GetRelativePath(file.FullName, options.Directory)}");
            }
            else
            {
                failed++;
            }
        }
    }
    
    // 清理空目录
    if (options.Recursive)
    {
        CleanEmptyDirectories(options.Directory);
    }
    
    Console.WriteLine(new string('─', 50));
    Console.WriteLine($"✨ 清理完成!");
    Console.WriteLine($"   • 成功删除: {deleted} 个文件");
    Console.WriteLine($"   • 删除失败: {failed} 个文件");
    Console.WriteLine($"   • 释放空间: {FormatSize(deleted == 0 ? 0 : result.TotalSavableSize)}");
    
    return failed == 0 ? 0 : 1;
}

void GenerateReport(AnalysisResult result, CleanOptions options)
{
    var reportDir = Path.Combine(options.Directory, "CleanupReports");
    Directory.CreateDirectory(reportDir);
    
    var reportPath = Path.Combine(reportDir, $"cleanup-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
    
    using var writer = new StreamWriter(reportPath);
    
    writer.WriteLine("════════════════════ .NET 程序集清理报告 ════════════════════");
    writer.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    writer.WriteLine($"分析目录: {Path.GetFullPath(options.Directory)}");
    writer.WriteLine($"入口程序: {Path.GetFileName(result.EntryFile)}");
    writer.WriteLine();
    
    writer.WriteLine("📊 分析结果摘要");
    writer.WriteLine(new string('─', 50));
    writer.WriteLine($"原始文件总数: {result.TotalFiles:N0} 个");
    writer.WriteLine($"原始总大小: {FormatSize(result.TotalSize)}");
    writer.WriteLine($"程序集文件: {result.OriginalAssemblyCount:N0} 个");
    writer.WriteLine($"语言包文件: {result.OriginalLocaleCount:N0} 个");
    writer.WriteLine($"被引用程序集: {result.UsedAssembliesCount:N0} 个");
    writer.WriteLine();
    
    writer.WriteLine("🗑️  可清理内容");
    writer.WriteLine(new string('─', 50));
    
    if (result.UnusedAssemblies.Any())
    {
        writer.WriteLine($"未使用程序集 ({result.UnusedAssemblies.Count}个, {FormatSize(result.UnusedSize)}):");
        foreach (var file in result.UnusedAssemblies.OrderBy(f => f.Name))
        {
            writer.WriteLine($"  • {GetRelativePath(file.FullName, options.Directory)} ({FormatSize(file.Length)})");
        }
        writer.WriteLine();
    }
    
    if (result.LocaleFiles.Any())
    {
        writer.WriteLine($"语言包文件 ({result.LocaleFiles.Count}个, {FormatSize(result.LocaleSize)}):");
        foreach (var file in result.LocaleFiles.OrderBy(f => f.Name))
        {
            writer.WriteLine($"  • {GetRelativePath(file.FullName, options.Directory)} ({FormatSize(file.Length)})");
        }
        writer.WriteLine();
    }
    
    writer.WriteLine("📈 精简效果预测");
    writer.WriteLine(new string('─', 50));
    writer.WriteLine($"可清理文件数: {result.TotalSavableCount:N0} 个");
    writer.WriteLine($"可释放空间: {FormatSize(result.TotalSavableSize)}");
    writer.WriteLine($"精简比例: {((double)result.TotalSavableSize / result.TotalSize * 100):F1}%");
    
    var remainingSize = result.TotalSize - result.TotalSavableSize;
    writer.WriteLine($"预计剩余大小: {FormatSize(remainingSize)}");
    
    writer.WriteLine(new string('═', 60));
    
    Console.WriteLine($"\n📄 报告已生成: {reportPath}");
}

// ================ 工具函数 ================
HashSet<string> FindUsedAssemblies(string entryPath, string baseDir, bool recursive)
{
    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var queue = new Queue<string>();
    
    var entryName = Path.GetFileNameWithoutExtension(entryPath);
    used.Add(entryName);
    queue.Enqueue(entryPath);
    
    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        if (!visited.Add(current)) continue;
        
        try
        {
            var assembly = LoadAssemblySafe(current);
            if (assembly == null) continue;
            
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name != null && used.Add(reference.Name))
                {
                    var depFile = FindAssemblyFile(reference.Name, baseDir, recursive);
                    if (depFile != null)
                    {
                        queue.Enqueue(depFile);
                    }
                }
            }
        }
        catch
        {
            // 忽略无法加载的程序集
        }
    }
    
    return used;
}

Assembly? LoadAssemblySafe(string path)
{
    try
    {
        return Assembly.LoadFrom(path);
    }
    catch
    {
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }
}

string? FindAssemblyFile(string assemblyName, string baseDir, bool recursive)
{
    var patterns = new[] { $"{assemblyName}.dll", $"{assemblyName}.exe" };
    
    foreach (var pattern in patterns)
    {
        try
        {
            var files = Directory.GetFiles(baseDir, pattern, 
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            
            if (files.Length > 0)
                return files[0];
        }
        catch { }
    }
    
    return null;
}

string? FindEntryFile(string directory, string entryHint)
{
    if (entryHint == "*")
    {
        // 自动检测
        var exeFiles = Directory.GetFiles(directory, "*.exe");
        if (exeFiles.Length == 1)
            return exeFiles[0];
        
        var dllFiles = Directory.GetFiles(directory, "*.dll")
            .Where(f => !IsLocaleFile(f))
            .Where(f => !Path.GetFileName(f).Contains("Test", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        return exeFiles.FirstOrDefault() ?? dllFiles.FirstOrDefault();
    }
    
    // 查找指定文件
    var fullPath = Path.Combine(directory, entryHint);
    if (File.Exists(fullPath))
        return fullPath;
    
    // 尝试添加扩展名
    if (!Path.HasExtension(entryHint))
    {
        foreach (var ext in new[] { ".exe", ".dll" })
        {
            var path = Path.Combine(directory, entryHint + ext);
            if (File.Exists(path))
                return path;
        }
    }
    
    return null;
}

bool IsLocaleFile(string filePath)
{
    var dir = Path.GetDirectoryName(filePath);
    if (dir != null)
    {
        var dirName = Path.GetFileName(dir);
        if (Regex.IsMatch(dirName, @"^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.IgnoreCase))
        {
            return true;
        }
    }
    
    var fileName = Path.GetFileName(filePath);
    return fileName.Contains(".resources.", StringComparison.OrdinalIgnoreCase) ||
           fileName.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase);
}

bool IsWhitelisted(string fileName, List<string> whitelist)
{
    if (whitelist == null || whitelist.Count == 0) return false;
    
    var comparer = StringComparer.OrdinalIgnoreCase;
    return whitelist.Any(w => comparer.Equals(w, fileName)) ||
           whitelist.Any(w => fileName.StartsWith(w + ".", StringComparison.OrdinalIgnoreCase));
}

bool IsSystemAssembly(string assemblyName)
{
    var systemPrefixes = new[] 
    {
        "System.", "Microsoft.", "Windows.", "netstandard", "mscorlib",
        "Accessibility", "Presentation", "WindowsBase"
    };
    
    return systemPrefixes.Any(prefix => 
        assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}

bool ShouldCleanOtherFile(string filePath)
{
    var ext = Path.GetExtension(filePath).ToLower();
    var cleanableExts = new[] { ".pdb", ".xml", ".config", ".bak", ".tmp", ".log" };
    
    return cleanableExts.Contains(ext) ||
           Path.GetFileName(filePath).StartsWith("~$", StringComparison.Ordinal);
}

bool DeleteFileSafe(string filePath)
{
    try
    {
        File.Delete(filePath);
        return true;
    }
    catch
    {
        return false;
    }
}

void CleanEmptyDirectories(string directory)
{
    foreach (var dir in Directory.GetDirectories(directory))
    {
        CleanEmptyDirectories(dir);
        
        try
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
        catch { }
    }
}

string GetRelativePath(string fullPath, string basePath)
{
    try
    {
        return Path.GetRelativePath(basePath, fullPath);
    }
    catch
    {
        return fullPath;
    }
}

string FormatSize(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }
    return $"{len:0.##} {sizes[order]}";
}

// ================ 帮助信息 ================
void PrintHelp()
{
    Console.WriteLine("""
        🔧 .NET 程序集清理工具 - 交互式版本
        
        用法:
          交互模式: dotnet run clear.cs
          命令行模式: dotnet run clear.cs [选项]
        
        选项:
          -a, --analyze        仅分析，不执行清理
          -p, --preview        预精简模式（显示将要清理的文件）
          --report             生成详细报告
          -d, --dir <目录>     要分析的目录
          -e, --entry <文件>   入口程序集
          -w, --whitelist <列表> 白名单（逗号分隔）
          -r, --recursive      递归扫描子目录
          --no-locale          清理语言包文件
          --clean-other        清理其他文件
          -v, --verbose        显示详细日志
          -h, --help           显示帮助
        
        交互模式功能:
          1. 仅分析: 查看当前目录的可清理内容
          2. 预精简: 显示精简前后的容量对比
          3. 直接清理: 执行删除操作
          4. 生成报告: 创建详细的清理报告
        
        示例:
          # 交互式操作
          dotnet run clear.cs
          
          # 分析指定目录
          dotnet run clear.cs --analyze --dir ./publish
          
          # 预精简模式查看效果
          dotnet run clear.cs --preview --dir ./bin/Release
          
          # 直接清理（小心使用）
          dotnet run clear.cs --dir ./output --no-locale
        
        提示: 建议先使用 --analyze 或 --preview 查看效果，再决定是否清理。
        """);
}

// ================ 数据模型 ================
enum OperationMode
{
    Interactive,
    AnalyzeOnly,
    PreviewCleanup,
    DeleteFiles,
    GenerateReport
}

class CleanOptions
{
    public string Directory { get; set; } = "";
    public string EntryPoint { get; set; } = "";
    public List<string> Whitelist { get; set; } = new();
    public OperationMode Mode { get; set; } = OperationMode.Interactive;
    public bool Recursive { get; set; }
    public bool CleanLocale { get; set; }
    public bool CleanOther { get; set; }
    public bool Verbose { get; set; }
}

class AnalysisResult
{
    public string EntryFile { get; set; } = "";
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public int OriginalAssemblyCount { get; set; }
    public long OriginalAssemblySize { get; set; }
    public int OriginalLocaleCount { get; set; }
    public long OriginalLocaleSize { get; set; }
    public int UsedAssembliesCount { get; set; }
    public List<FileInfo> UnusedAssemblies { get; set; } = new();
    public long UnusedSize { get; set; }
    public List<FileInfo> LocaleFiles { get; set; } = new();
    public long LocaleSize { get; set; }
    public long OtherSize { get; set; }
    public long TotalSavableSize { get; set; }
    public int TotalSavableCount { get; set; }
}