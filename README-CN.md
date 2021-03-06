# SQLib

**SQLib** 是一个自动参数化的 **SQL** 创作工具。

- [English Readme](https://github.com/zmjack/SQLib/blob/master/README.md)
- [中文自述](https://github.com/zmjack/SQLib/blob/master/README-CN.md)

<br/>

支持的数据库：

| Database      | Library         | Scope          | .NET Version                                                | Provider                                          |
| ------------- | --------------- | -------------- | ----------------------------------------------------------- | ------------------------------------------------- |
| **MySql**     | SQLib.MySql     | MySqlScope     | NET 4.5 / 4.5.1 / 4.6<br />**NET Standard 2.0**             | MySqlConnector                                    |
| **Sqlite**    | SQLib.Sqlite    | SqliteScope    | NET 3.5 / 4.0 / 4.5 / 4.5.1 / 4.6<br />**NET Standard 2.0** | System.Data.SQLite<br />**Microsoft.Data.Sqlite** |
| **SqlServer** | SQLib.SqlServer | SqlServerScope | NET 3.5 / 4.0 / 4.5 / 4.5.1 / 4.6<br />**NET Standard 2.0** | .NET Framework<br />**Microsoft.Data.SqlClient**  |
| **Others**    | SQLib           | SqlScope<>     | NET 3.5 / 4.0 / 4.5 / 4.5.1 / 4.6<br />**NET Standard 2.0** | -                                                 |

<br/>

## .NET 兼容性

### **.NET 3.5 / 4.0 / 4.5 / 4.5.1**

如果 **Visual Studio** 不支持字符串内插 ( **$""** )，可使用 **FormattableStringFactory.Create** 创建 **FormattableString**。

例如：

```csharp
sqlite.SqlQuery($"SELECT * FROM main WHERE Text={"Hello"};");
```

等同于：

```csharp
sqlite.SqlQuery(
    FormattableStringFactory.Create(
        "SELECT * FROM main WHERE Text={0};",
        "Hello"));
```

<br/>

## 用法说明

### 0. 引用

本文中的所有示例都使用 **Sqlite** 进行描述。

从 **NuGet** 安装库 **SQLib.Sqlite**：

```powershell
dotnet add package SQLib.Sqlite
```

示例数据库 **sqlib.db** 表 **main** 定义如下：

| 列名             | 类型    | C# 类型  |
| ---------------- | ------- | -------- |
| **CreationTime** | text    | DateTime |
| **Integer**      | integer | int      |
| **Real**         | real    | double   |
| **Text**         | text    | string   |
| **Blob**         | blob    | byte[]   |

<br/>

### 1. 构建访问器

从 **SqlScope** 构建数据访问器（SQLite）：

```c#
public class ApplicationDbScope : SqliteScope<ApplicationDbScope>
{
    public const string CONNECT_STRING = "filename=sqlib.db";
    public static ApplicationDbScope UseDefault() => new ApplicationDbScope(CONNECT_STRING);

    public ApplicationDbScope(string connectionString) : base(connectionString) { }
}
```
<br/>

### 2. 无返回记录查询

使用 **Sql** 方法进行无返回记录的自动参数化查询。

例如，使用如下语句进行数据插入：

```c#
using (var sqlite = ApplicationDbScope.UseDefault())
{
    sqlite.Sql($"INSERT INTO main (CreationTime, Integer, Real, Text, Blob) VALUES ({creationTime}, {416L}, {5.21d}, {"Hello"}, {"Hello".Bytes()});");
}
```

将生成以下带有参数的 **SQL** 语句用于查询：

```sqlite
INSERT INTO main (CreationTime, Integer, Real, Text, Blob) VALUES (@p0, @p1, @p2, @p3, @p4);
```

<br/>

**IN** 语句参数化：

```csharp
sqlite.SqlQuery($"SELECT * FROM main WHERE Integer in {new[] { 415, 416, 417 }};");
```

生成参数化 SQL：

```sql
SELECT * FROM main WHERE Integer in (@p0_0, @p0_1, @p0_2);
```

但是 **字节数组** 是特殊的，它通常用于表示文件，而不被翻译为 **new[] { ... }** 语法。

<br/>

### 3. 有返回记录查询

使用 **SqlQuery** 进行有返回记录的自动参数化查询。

例如，在 **main** 表中查询记录，找到 **Text** 是 ***Hello*** （参数化）的首条记录，返回它的 **Real** 值:

```c#
var record = sqlite.SqlQuery($"SELECT * FROM main WHERE Text={"Hello"};").First();
Assert.Equal(5.21d, record["Real"]);
```

#### 使用实体接收查询

为了便于使用强类型，我们提供了实体接收查询的方法：

```c#
public class Main
{
    public DateTime CreationTime { get; set; }
    public int Integer { get; set; }
    public double Real { get; set; }
    public string Text { get; set; }
    public byte[] Blob { get; set; }
}
```
```c#
var record = sqlite.SqlQuery<Main>($"SELECT * FROM main WHERE Text={"Hello"};").First();
Assert.Equal(5.21d, record.Real);
```

<br/>

### 4. SQL 监视

如果需要监视 **SQL** 的执行，可以注册 **OnExcuted** 事件：

```c#
using (var sqlite = ApplicationDbScope.UseDefault())
{
    var output = new StringBuilder();
    sqlite.OnExecuted += command => output.AppendLine(command.CommandText);

    sqlite.Sql($"INSERT INTO main (CreationTime, Integer, Real, Text, Blob) VALUES ({creationTime}, {416L}, {5.21d}, {"Hello"}, {"Hello".Bytes()});");

    Assert.Equal(@"INSERT INTO main (CreationTime, Integer, Real, Text, Blob) VALUES (@p0, @p1, @p2, @p3, @p4);
", output.ToString());
}
```

<br/>

## 其他提示

应该使用自动参数化来转换所有不可靠的输入，以<font color=red>防止 **SQL 注入**</font>。

例如，查询 **main** 表中 **Text** 为 ***Hello***（用户输入）的第一条记录：

```c#
var text = "Hello";
sqlite.SqlQuery($"SELECT * FROM main WHERE Text={text};");
```

```sqlite
SELECT * FROM main WHERE Text=@p0;
/*
    @p0 = "Hello";
*/
```

在传统方法中，等同于

```c#
var text = "Hello";
var cmd = new SqliteCommand("SELECT * FROM main WHERE Text=@p0;", conn);
cmd.Parameters.Add(new SqliteParameter
{
    ParameterName = "@p0",
    Value = text,
    DbType = DbType.String,
});
cmd.ExecuteNonQuery();
```
<br/>

### SQL 注入警示

<font color=red>永远不应该使用拼接方式来组合 SQL 语句</font>：

```c#
var text = "Hello";
sqlite.UnsafeSqlQuery("SELECT * FROM main WHERE Text='" + text +"';");
```

```sqlite
SELECT * FROM main WHERE Text='Hello';
```

如果这样做，当用户输入特定值时，**SQL** 语义可能会改变：

```c#
var text = "'or 1 or '";
sqlite.UnsafeSqlQuery("SELECT * FROM main WHERE Text='" + text +"';");
```

```sqlite
SELECT * FROM main WHERE Text='' or 1 or '';
```

这将导致一系列安全问题。

<br/>

