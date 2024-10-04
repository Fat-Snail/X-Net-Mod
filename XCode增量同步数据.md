# 增量同步

在常见的业务逻辑中，数据库同步是一个重要的环节。数据库同步的方法多种多样，其中一些是数据库自带的功能，例如主从复制和集群等。然而，这些方法通常局限于同种数据库类型之间的同步，跨不同数据库类型时就显得力不从心。有人提到 Mycat，但使用过的人都知道，对于简单的数据存储还好，复杂的场景则需要投入大量的学习成本。此外，自从 Mycat 1.6 版本后，几乎没有革命性的更新，功能也停留在七八年前。虽然 2.0 版本看起来不错，但也已有两年未更新。因此，数据库集群的解决方案仍然需要依赖数据库厂商，想要依靠第三方工具而不学习几乎是不可能的。

那么，业务需要在不同数据库之间同步时，有什么解决方案呢？虽然有一些工具，比如 Navicat，但它并不支持自增键的同步。而且，我也不明白为什么要先进行结构同步再进行数据同步，为什么不能一键操作？所以，最终还是得自己动手。

借助 Newlife.XCode 组件，我们可以轻松编写一个符合需求的数据库同步程序。如果有小伙伴不会编写代码，也可以使用现成的项目，链接如下：
[GitHub - Fat-Snail/X-Mod](https://github.com/Fat-Snail/X-Mod)

如果想亲自动手，可以参考这篇教程：[Newlife.XCode 教程](https://newlifex.com/xcode/no_entity)。Newlife 几乎涵盖了主流的数据库系统，支持 MySQL、SQLite、SQL Server、Oracle、PostgreSQL、TDengine、达梦、金仓、瀚高等，因此无需担心数据库不兼容的问题。

解决了一对一同步的问题后，还有一些常见但偏门的场景，例如日志迁移、小批量内容传输、第三方数据库的一对多数据增量等。之前的解决方案通常是将数据转成实体，然后进行 JSON 序列化，再打包成压缩包，通过程序反序列化导入到数据库。这种增量同步方法几乎适用于所有小批量同步场景。强调小批量的原因在于，如果数据量达到 1000 条，内存和导入速度就会成为大问题，因为 JSON 反序列化会批量序列化成实体列表，内存可能会瞬间被挤爆。为了防止内存溢出，必须将数据分成多个 JSON 文件导入，这无形中增加了业务的复杂性和可靠性。

那么，有没有一种既简单又能处理几百万条数据增量同步的方法呢？SQLite 绝对是不二之选！使用 Newlife 的 ORM 操作 SQLite 简直是小菜一碟，可以预设好实体，也可以无实体操作，ORM 会自动下载 SQLite 驱动，操作简单到不行，您只需编写自己的增量同步逻辑即可。

如果要实现自动导出增量包，可能会遇到 ORM 的反向工程问题，具体可以参考这篇文章：[Newlife.XCode 反向工程](https://newlifex.com/xcode/negative)。为了避免频繁检查表结构，通常在程序启动时会对数据库表结构进行检查，如果表不存在则会自动创建。不同的自带会自动修改或增加表结构。然而，在进行增量同步时，自动打包会删除旧备份，否则就不算增量备份。Newlife.X.Code 组件并不知道您删除了数据库，因此在第二次导入新备份时可能会报错 `SQL logic error: no such table`。

因此，在打包好备份后，删除 SQLite 数据库时，必须重置 XCode 的反向工程。为了避免重复检查表结构，XCode 在底层做了很多拦截，具体可以查看源码，包括 DAL 和 `EntitySession<TEntity>`。因此，需要重置这些已保存的检查变量。

首先是基于原本的 `ConnName` 进行重置，即默认数据库连接的重置。这里需要使用 DAL 的 `Reset` 方法，原本底层的 `Reset` 方法是私有的，因此版本必须大于 11.15.2024.902。具体重置代码如下：

如何查看实体的默认 `ConnName`，如下图所示：

![image](/Assets/zl-1.png)

接下来，我们通过代码进行重置。假设我们使用 Newlife.XCode 组件新建一个 SQLite 数据库用于保存用户信息，然后手动删除新增的 `Membership.db`，接着点击新增用户时，看看是否会报 `no such table` 的错误。

![image](/Assets/zl-2.png)

```csharp
private void button1_Click(object sender, EventArgs e)
{
    var dal = DAL.Create("Membership"); // 使用默认的 ConnName
    dal.Reset();
    dal.CheckTables();
    try
    {
        var users = XCode.Membership.User.FindAll();

        var user = new XCode.Membership.User();
        user.Name = "Tom";
        user.InsertAsync();
    }
    catch (Exception ex)
    {
        // 处理异常
    }
}
```

如果没有执行以下代码：

```csharp
var dal = DAL.Create("Membership"); // 使用默认的 ConnName
dal.Reset();
dal.CheckTables();
```

那么下面的查询和插入数据将会报错，提示找不到 `User` 表。

`Newlife.XCode` 组件还有一个高级用法，即支持分表分库。那么，这与增量备份有什么关系呢？其实，在进行增量备份时，我不建议使用默认连接。如果换个思路，将其做成分库，操作性会大大增强。然而，核心问题在于 `CheckTables()` 方法中包含了检查各个 `TEntity` 是否已初始化表，而表结构的读取是基于默认连接的。因此，上述方法需要改良，代码如下：

```csharp
private void button1_Click(object sender, EventArgs e)
{
    var tables = XCode.EntityFactory.GetTables("Membership", true); // 使用默认数据库连接读取表结构

    var dir = "./Data/ms.db".EnsureDirectory(); // 定义分库的保存地址

    DAL.AddConnStr("Membership1", $"Data Source={dir};provider=sqlite", null, null); // Membership1 为分库数据库连接

    var dal = DAL.Create("Membership1");
    dal.SetTables(tables.ToArray());
    try
    {
        XCode.Membership.User.Meta.ConnName = "Membership1";
        var users = XCode.Membership.User.FindAll();

        var user = new XCode.Membership.User();
        user.Name = "Tom";
        user.InsertAsync();
    }
    catch (Exception ex)
    {
        // 处理异常
    }

    XCode.Membership.User.Meta.ConnName = "Membership"; // 记得把 ConnName 设回默认
}

```

当然，代码还可以进一步改良，使用 Newlife.XCode 最新的分表分库语法，代码如下：

```csharp
private void button1_Click(object sender, EventArgs e)
{
    var tables = XCode.EntityFactory.GetTables("Membership", true); // 使用默认数据库连接读取表结构

    var dir = "./Data/ms.db".EnsureDirectory(); // 定义分库的保存地址

    DAL.AddConnStr("Membership1", $"Data Source={dir};provider=sqlite", null, null); // Membership1 为分库数据库连接

    var dal = DAL.Create("Membership1");
    dal.SetTables(tables.ToArray());
    try
    {
        using var split = XCode.Membership.User.Meta.CreateSplit("Membership1", XCode.Membership.User.Meta.TableName);

        var users = XCode.Membership.User.FindAll();

        var user = new XCode.Membership.User();
        user.Name = "Tom";
        user.InsertAsync();
    }
    catch (Exception ex)
    {
        // 处理异常
    }
}

```

基本上，使用 SQLite 实现小批量的增量同步就完成了。如果数据量小于 `1000` 条，仍然建议使用 JSON 序列化，因为 SQLite 创建一个表的容量起步都在 60K，而且如果有索引，文件会更大。好处在于，您仍然可以使用熟悉的数据库操作，而不必担心内存突然被占满。
