# 使用魔方快速搭建OAuth 2.0服务

> 在看本教程之前，如果你不太了解SSO登录以及OAuth 2.0 可以看两篇文章，连接如下：
> [OAuth2.0授权登录](https://www.cnblogs.com/wwcom123/p/11600463.html)
> [OAuth 2.0 的四种方式](http://www.ruanyifeng.com/blog/2019/04/oauth-grant-types.html)
>

为了方便同一环境搭建，建议使用win11系统，本教程只是告诉你魔方搭建OAuth服务的原理，如果融会贯通，部署到Win Server和Linux自然没问题，毕竟Newlife组件已经历十几年岁月的沉淀。

目录会包含本次实验的测试包，测试包基于最新的.NET 9,适合小白搭建，如果想自行编译可下载最新的Cube源码进行编译运行

**第一步** ，搭建OAuth 2.0服务授权站点，因为是测试，所以便于区分，服务授权站点我端口定义为6080，子站点我定义为6090 **

修改 CubeDemoNC_master/appsettings.json 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://*:6080;https://*:6081",
  //"StarServer": "http://star.newlifex.com:6600",
  //"RedisCache": "server=127.0.0.1;password=;db=2",
  "ConnectionStrings": {
    "Membership": "Data Source=\\Data\\Membership.db;provider=sqlite",
    "Log": "Data Source=\\Data\\Log.db;provider=sqlite",
    "MembershipMySql": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    "LogMySql": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql"
  }
}
```

主要是修改了Urls，让其监听6080端口。注意Membership和Log我已经改到本目录Data下面，不然会跟子站共享数据库文件

然后点击cube.exe启动，dotcore的启动方法很多，mac和linux启动只能自行百度，这里不再赘述

如果点击启动报缺失Plugins驱动的错误的话，可以使用我打包的Plugins-win11.zip，可以解压到Plugins目录即可，然后再启动，如无意外就可以访问http://localhost:6080/可以进入登录界面

使用默认的超管账号登陆admin/admin 线上环境建议禁用超管

配置子站登陆SSO的凭证及回调接口

> 魔方管理 -> 应用系统 -> 添加
>

![图1](/pics/01.png)

添加测试用户（用户全部存放在服务授权站点，子站点只有授权登陆，没有用户注册）

> 系统管理 -> 用户 -> 添加

![图2](/pics/02.png)
![图3](/pics/03.png)

**第二步**，启动子站点，依然拿魔方作为子站点程序，如果是其他自研系统，只要实现NewLife.CubeNC源码下面的SsoController 所有 单点登录客户端 的代码即可

那话不多说，先修改配置文件CubeDemoNC_site1/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://*:6090;https://*:6091",
  //"StarServer": "http://star.newlifex.com:6600",
  //"RedisCache": "server=127.0.0.1;password=;db=2",
  "ConnectionStrings": {
    "Membership": "Data Source=\\Data\\Membership.db;provider=sqlite",
    "Log": "Data Source=\\Data\\Log.db;provider=sqlite",
    "MembershipMySql": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    "LogMySql": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql"
  }
}
```

修改了Urls，把子站端口设为 6090，以便于区分

为了隔离与服务授权站点的关联，我们使用Chrome的无痕模式访问 http://localhost:6090/

需登陆后台填写魔方的登陆信息，如果是自研平台可以保存在appsettings.json或者保存在数据库

魔方默认自带一个 newlifex.com 的授权配置，可以参考然后变成自己的配置就好

![图4](/pics/04.png)

以下是填写参数

```txt
名称：sso1     （到时地址需要填写这个参数）
应用标识：Site1         （必填项，必须跟之前填写的AppId一致）
应用密钥：AAAABBBB     （与授权登陆服务器一致）
昵称：商城sso       （随意填写）
服务地址：http://localhost:6080/sso
令牌地址：access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}             （标准的OAuth2.0授权地址格式）

授权类型：AuthorizationCode         （其他三种可以参考OAuth 2.0 的四种方式）
验证地址：authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}

```

![图6](/pics/06.png)

新增好后退出登陆

然后地址栏输入：http://localhost:6090/Sso/Login?name=sso1  ，注意：确保http://localhost:6080是正在运行的

这时会跳转到服务授权站点 然后输入刚才录入的测试账号便可实现授权登陆，这时子站点是没有tom这个用户的

输入tom的登陆信息便可跳转回 http://localhost:6090 到这时，登陆用户回显示一个随机生成的用户名

![图7](/pics/07.png)

魔方搭建OAuth 2.0服务搭建便完成了，如果全套都是使用魔方平台，那完全可以借助魔方平台快速搭建授权中心，那如果是自研的话就得了解魔方OAuth 2.0校验具体细节了

