# 全站静态化

## 需求

- 抵御无序访问攻击，增加访问速度，减轻知服务器负担

- 搜索引擎优化SEO

- 使用CDN加速，加快页面制打开速度


## 案例

使用 [Razor.Templating.Core](https://github.com/soundaranbu/Razor.Templating.Core) 组件 不用修改代码，无缝的生成你要生成的静态页面

例子放在本目录 HtmlGanerate.zip 文件

## 如何使用

只需要新建一个控制台或服务，然后引用你的网站，其实就是需要里面的View模版生成，对模版支持如下：

| MVC Razor View Features           |               |
|---------------------------------- |---------------|
| ViewModel                         | &check;       |
| ViewBag                           | &check;       |
| ViewData                          | &check;       |
| Layouts                           | &check;       |
| ViewStarts                        | &check;       |
| ViewImports                       | &check;       |
| Partial Views                     | &check;       |
| Tag Helpers                       | &check;       |
| View Components (.NET 5 +)        | &check;       |
| Dependency Injection into Views   | &check;       |
| @Url.ContentUrl**                 | &cross;       |
| @Url.RouteUrl**                   | &cross;       |


市面上的Razor引擎基本都比较老旧，或者不怎么支持ViewData和ViewBag，这个组建做到无缝衔接，还是值得表扬的，如果需要编辑模版，可以使用进阶功能，把模版放在目录，具体可参考：

We can make use of ASP.NET Core's inbuilt [RazorRuntimeCompilation](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/view-compilation?view=aspnetcore-6.0&tabs=visual-studio) to render any .cshtml inside or outside of the project.

As of `v1.7.0+`, we can achieve this as below:
```csharp
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Razor.Templating.Core;

var services = new ServiceCollection();
services.AddMvcCore().AddRazorRuntimeCompilation();
services.Configure<MvcRazorRuntimeCompilationOptions>(opts =>
{
    opts.FileProviders.Add(new PhysicalFileProvider(@"D:\PathToRazorViews")); // This will be the root path
});
services.AddRazorTemplating();

var html = await RazorTemplateEngine.RenderAsync("/Views/Home/Rcl.cshtml"); // relative path to the root
```

Enjoy～  ^_^





## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的70多个开源项目已被广泛应用于各行业，Nuget累计下载量高达100余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  