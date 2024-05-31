#  动态编译Razor页面

## 需求

 以前写网站很喜欢WebForm来搞，因为aspx支持实时编辑，以至于Asp.net MVC发布多年，我依然对aspx念念不忘。终于在一个偶然的机会得知，原来DotnetCore 在2.1的版本就已经支持Razor模版动态编译，而且是官方支持的，现在已更新到8.0.6，几乎跟所有官方组件同步更新，我以为只有我这个从asp开始写网站，一直写到Net8 才有这种怀旧的需求


## 如何使用

首先添加 Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation 组件

```
dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
```

然后修改项目文件 譬如：xxx.csproj
```xml
   <ItemGroup>
        <PageFiles Include="$(ProjectDir)\Pages\**\*.cshtml" />
    </ItemGroup>

    <!-- Copy .cshtml files from Pages folder after publishing -->
    <Target Name="CopyPageFilesAfterPublish" AfterTargets="Publish">
        <Copy SourceFiles="@(PageFiles)" DestinationFolder="$(PublishDir)\Pages\%(RecursiveDir)" />
    </Target>
```

 这是 WebPages项目，如果是MVC，则：

```xml
  <ItemGroup>
        <PageFiles Include="$(ProjectDir)\Views\**\*.cshtml" />
    </ItemGroup>

    <!-- Copy .cshtml files from Views folder after publishing -->
    <Target Name="CopyPageFilesAfterPublish" AfterTargets="Publish">
        <Copy SourceFiles="@(PageFiles)" DestinationFolder="$(PublishDir)\Views\%(RecursiveDir)" />
    </Target>

```

配置文件也很好理解，就是把后缀为cshtml的文件拷贝到发布或者debug目录

这样，就可以实时修改Razor文件而不用重新发布重新编译，也不用重启IIS服务器或是Nginx。就是那么丝滑，就是那么无缝衔接。当然，因为是实时编译的，理论上会比发布版本慢上一丢丢，但是在项目初期，还是很方便的，不然改一个标题都得喊程序员上线。

当然，还有引用RuntimeCompilation有个缺陷，就是会产生大量的文件夹和编译文件，如果你本身项目就是有这些依赖文件那可以忽略，如果是对文件数量有要求的强迫症患者，譬如我，那就必须解决这个天大的问题，所以可以使用NetBeauty2美化文件目录


## NetBeauty2（可选）

首先感谢 liesauer 大神修复了对RuntimeCompilation组件的支持

### Add Nuget reference to your .NET Core project.
```
dotnet add package nulastudio.NetBeauty
```

然后根据说明，编辑项目文件，譬如：xxx.csproj

```xml

  <PropertyGroup>
    <BeautySharedRuntimeMode>False</BeautySharedRuntimeMode>
    <!-- beauty into sub-directory, default is libs, quote with "" if contains space  -->
    <BeautyLibsDir Condition="$(BeautySharedRuntimeMode) == 'True'">../libraries</BeautyLibsDir>
    <BeautyLibsDir Condition="$(BeautySharedRuntimeMode) != 'True'">./libraries</BeautyLibsDir>
    <!-- dlls that you don't want to be moved or can not be moved -->
    <!-- <BeautyExcludes>dll1.dll;lib*;...</BeautyExcludes> -->
    <!-- dlls that end users never needed, so hide them -->
    <!-- <BeautyHiddens>hostfxr;hostpolicy;*.deps.json;*.runtimeconfig*.json</BeautyHiddens> -->
    <!-- set to True if you want to disable -->
    <DisableBeauty>False</DisableBeauty>
    <!-- set to False if you want to beauty on build -->
    <BeautyOnPublishOnly>False</BeautyOnPublishOnly>
    <!-- DO NOT TOUCH THIS OPTION -->
    <BeautyNoRuntimeInfo>False</BeautyNoRuntimeInfo>
    <!-- set to True if you want to allow 3rd debuggers(like dnSpy) debugs the app -->
    <BeautyEnableDebugging>False</BeautyEnableDebugging>
    <!-- the patch can reduce the file count -->
    <!-- set to False if you want to disable -->
    <!-- SCD Mode Feature Only -->
    <BeautyUsePatch>True</BeautyUsePatch>
    <!-- App Entry Dll = BeautyDir + BeautyAppHostDir + BeautyAppHostEntry -->
    <!-- see https://github.com/nulastudio/NetBeauty2#customize-apphost for more details -->
    <!-- relative path based on AppHostDir -->
    <!-- .NET Core Non Single-File Only -->
    <!-- <BeautyAppHostEntry>bin/MyApp.dll</BeautyAppHostEntry> -->
    <!-- relative path based on BeautyDir -->
    <!-- .NET Core Non Single-File Only -->
    <!-- <BeautyAppHostDir>..</BeautyAppHostDir> -->
    <!-- <BeautyAfterTasks></BeautyAfterTasks> -->
    <!-- valid values: Error|Detail|Info -->
    <BeautyLogLevel>Info</BeautyLogLevel>
    <!-- set to a repo mirror if you have troble in connecting github -->
    <!-- <BeautyGitCDN>https://gitee.com/liesauer/HostFXRPatcher</BeautyGitCDN> -->
    <!-- <BeautyGitTree>master</BeautyGitTree> -->
  </PropertyGroup>


```

然后编译发布的时候，目录变干净了，只有寥寥几个核心的dll文件，也方便对项目进行更新


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