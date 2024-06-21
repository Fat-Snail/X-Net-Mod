# MVVM
- MVVM是一种用于构建用户界面的软件架构模式，它的名称代表着三个组成部分：Model（模型）、View（视图）和ViewModel（视图模型）。MVVM的主要目标是将应用程序的UI与其底层数据模型分离，通过数据绑定实现数据和UI的自动同步，从而降低代码的耦合度，提高应用程序的可维护性和可测试性。

![image](/Assets/mvvm.png)

## Minmvvm 

- 兼容Winform、Wpf、AvaloniaUI，理论上MAUI也支持，没测试

| .NET Desktop UI |         |
|-----------------|---------|
| WinForm         | &check; |
| WPF             | &check; |
| AvaloniaUI      | &check; |
| MAUI            | unkonw  |

- 优点，不用引用ReactiveUI这么重的组件，精简清爽
- 缺点，一些细小的功能还在完善。常用的绑定、命令都已适配ReactiveUI，可以无缝切换

**[代码](MiniMvvm.cs)**   
**[Demo](Mvvm.zip)**

## 如何使用

```C#
//继承ViewModelBase
public class MainViewModel : ViewModelBase
```
属性
```C#
    private string _text="默认值";
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }
```
命令
```C#
    public ICommand TestNormalCommand { get; }
    private void TestNormal()
    {
        //实现逻辑
    }
```

WPF、AvaloniaUI的界面是支持命令和属性绑定的。而WinForm基于事件，所以只能通过代码绑定，WinForm的Demo里面包含了ReactiveExtensions.cs可以参考一下

Enjoy～  ^_^





