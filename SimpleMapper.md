# SimpleMapper
- 在写程序时，经常会遇到对象映射的需求。譬如不想ORM底层暴露在接口的Json对象或者两个对象组成一个Json对象。而对象组件常用的有：

**[AutoMapper](https://github.com/AutoMapper/AutoMapper)**

非常简单实用映射组件

**[TinyMapper](https://github.com/TinyMapper)**

非常轻量快速的映射组件，借助Emit特性，性能碾压AutoMapper

但是想想又得引用对一个组件，其实对象映射的内核就是通过反射属性赋值，所以可以自行写一个SimpleMapper

如果项目引用了Newlife.Core,那可以使用Newlife自带的反射扩展实现高效的对象映射，如果不打算引用，可以使用反射自行修改一下^ _ ^


## 如何使用

```C#
        SimpleMapper.Config<UserDTO>(cfg =>
        {
            cfg.Ignore(x=>x.Phone);//配置跳过
            cfg.Bind<User>(d=>d.CreateTime,s=>s.RegTime);//配置不同名称映射
        });
        
        var dto = SimpleMapper.Map<UserDTO>(new User { UserName = "Lobster", Age = 18, Phone = "133333333333",RegTime = DateTime.Now});
        
        Console.WriteLine(dto.UserName);
```
[源码](SimpleMapper.cs)

Enjoy～  ^_^





