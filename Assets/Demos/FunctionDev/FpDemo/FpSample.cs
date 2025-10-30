//==============================================
//               [AI GENERATED]
//==============================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;

public class FpDemo : MonoBehaviour
{
    [Header("Result 类使用示例")]
    [SerializeField]
    private bool runExamplesOnStart = true;

    private void Start()
    {
        if (runExamplesOnStart)
        {
            RunAllExamples();
        }
    }

    [ContextMenu("运行所有示例")]
    public void RunAllExamples()
    {
        Debug.Log("=== Result 类全面示例 ===");

        BasicUsageExample();
        MappingExample();
        BindingExample();
        PatternMatchingExample();
        ErrorHandlingExample();
        AsyncExample();
        ExtensionMethodsExample();
        UtilityMethodsExample();
    }

    #region 基本使用示例
    private void BasicUsageExample()
    {
        Debug.Log("\n--- 基本使用示例 ---");

        // 创建成功和失败的结果
        var successResult = Result<int, string>.Success(42);
        var failureResult = Result<int, string>.Failure("计算失败");

        Debug.Log($"成功结果: IsSuccess={successResult.IsSuccess}, Value={successResult.Value}");
        Debug.Log($"失败结果: IsSuccess={failureResult.IsSuccess}, Error={failureResult.Error}");

        // 使用静态工厂方法
        var staticSuccess = Result.Success("Hello World");
        var staticFailure = Result.Failure<int>("操作失败");

        Debug.Log($"静态成功: {staticSuccess.Value}");
        Debug.Log($"静态失败: {staticFailure.Error}");
    }
    #endregion

    #region 映射示例
    private void MappingExample()
    {
        Debug.Log("\n--- 映射示例 ---");

        var result = Result<int, string>.Success(10);

        // Map: 转换成功值
        var doubled = result.Map(x => x * 2);
        Debug.Log($"原始值: {result.Value}, 映射后: {doubled.Value}");

        // MapError: 转换错误类型
        var errorResult = Result<int, string>.Failure("网络错误");
        var mappedError = errorResult.MapError(error => $"严重错误: {error}");
        Debug.Log($"原始错误: {errorResult.Error}, 映射后: {mappedError.Error}");

        // 链式映射
        var chainResult = result.Map(x => x * 3).Map(x => x.ToString()).Map(s => $"结果: {s}");
        Debug.Log($"链式映射结果: {chainResult.Value}");
    }
    #endregion

    #region 绑定示例
    private void BindingExample()
    {
        Debug.Log("\n--- 绑定示例 ---");

        // 基本绑定
        var result1 = Result<int, string>.Success(5);
        var result2 = result1.Bind(x => Result<string, string>.Success($"数字: {x}"));
        Debug.Log($"绑定结果: {result2.Value}");

        // 绑定失败情况
        var failureResult = Result<int, string>.Failure("第一步失败");
        var bindFailure = failureResult.Bind(x => Result<string, string>.Success("不会执行"));
        Debug.Log($"绑定失败: {bindFailure.Error}");

        // 链式绑定
        var chainBind = result1
            .Bind(x => Result<int, string>.Success(x * 2))
            .Bind(x => Result<string, string>.Success($"最终结果: {x}"));
        Debug.Log($"链式绑定: {chainBind.Value}");
    }
    #endregion

    #region 模式匹配示例
    private void PatternMatchingExample()
    {
        Debug.Log("\n--- 模式匹配示例 ---");

        var successResult = Result<int, string>.Success(100);
        var failureResult = Result<int, string>.Failure("处理失败");

        // 使用 Match 进行副作用操作
        successResult.Match(
            onSuccess: value => Debug.Log($"处理成功，值: {value}"),
            onError: error => Debug.Log($"处理失败: {error}")
        );

        failureResult.Match(
            onSuccess: value => Debug.Log($"处理成功，值: {value}"),
            onError: error => Debug.Log($"处理失败: {error}")
        );

        // 使用 Match 返回值
        var successMessage = successResult.Match(
            onSuccess: value => $"成功获得值: {value}",
            onError: error => $"错误: {error}"
        );
        Debug.Log($"匹配结果: {successMessage}");
    }
    #endregion

    #region 错误处理示例
    private void ErrorHandlingExample()
    {
        Debug.Log("\n--- 错误处理示例 ---");

        // 安全执行
        var safeResult = Result.SafelyExecute(() => Divide(10, 2), "除法运算失败");
        Debug.Log($"安全执行成功: {safeResult.Value}");

        var safeFailure = Result.SafelyExecute(() => Divide(10, 0), "除零错误");
        Debug.Log($"安全执行失败: {safeFailure.Error}");

        // TryExecute
        var tryResult = Result.TryExecute(() => Divide(10, 2));
        Debug.Log($"Try执行成功: {tryResult.Value}");

        var tryFailure = Result.TryExecute(() => Divide(10, 0));
        Debug.Log($"Try执行失败: {tryFailure.Error.Message}");

        // GetValueOrDefault
        var successResult = Result<int, string>.Success(42);
        var failureResult = Result<int, string>.Failure("错误");

        Debug.Log($"成功默认值: {successResult.GetValueOrDefault(0)}");
        Debug.Log($"失败默认值: {failureResult.GetValueOrDefault(0)}");

        // GetValueOrThrow
        try
        {
            var value = successResult.GetValueOrThrow();
            Debug.Log($"获取值成功: {value}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"获取值异常: {ex.Message}");
        }
    }

    private int Divide(int a, int b)
    {
        return a / b;
    }
    #endregion

    #region 异步示例
    private async void AsyncExample()
    {
        Debug.Log("\n--- 异步示例 ---");

        // 异步安全执行
        var asyncResult = await Result.SafelyExecuteAsync(
            async () => await SimulateAsyncOperation(1000, "成功"),
            "异步操作失败"
        );
        Debug.Log($"异步执行结果: {asyncResult.Value}");

        // 异步Try执行
        var asyncTryResult = await Result.TryExecuteAsync(async () =>
            await SimulateAsyncOperation(500, "Try成功")
        );
        Debug.Log($"异步Try结果: {asyncTryResult.Value}");

        // 异步绑定
        var asyncBindResult = await Result<int, string>
            .Success(5)
            .BindAsync(async x =>
            {
                var result = await SimulateAsyncOperation(x * 100, $"处理结果: {x}");
                return Result<string, string>.Success(result);
            });
        Debug.Log($"异步绑定结果: {asyncBindResult.Value}");

        // 异步绑定处理错误
        var asyncBindWithError = await Result<int, string>
            .Success(-1)
            .BindAsync(async x =>
            {
                if (x < 0)
                    return Result<string, string>.Failure("输入值不能为负数");

                var result = await SimulateAsyncOperation(x * 100, $"处理结果: {x}");
                return Result<string, string>.Success(result);
            });
        Debug.Log($"异步绑定错误处理: {asyncBindWithError.Error}");

        // 使用辅助方法的简洁异步绑定
        var simpleAsyncBind = await Result<int, string>
            .Success(3)
            .BindAsync(x => SimulateAsyncOperationWithResult(x * 200, $"简洁处理: {x}"));
        Debug.Log($"简洁异步绑定: {simpleAsyncBind.Value}");
    }

    private async Task<string> SimulateAsyncOperation(int delay, string result)
    {
        await Task.Delay(delay);
        return result;
    }

    // 辅助方法：将异步操作包装为 Result
    private async Task<Result<string, string>> SimulateAsyncOperationWithResult(
        int delay,
        string result
    )
    {
        try
        {
            await Task.Delay(delay);
            return Result<string, string>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<string, string>.Failure($"异步操作失败: {ex.Message}");
        }
    }
    #endregion

    #region 扩展方法示例
    private void ExtensionMethodsExample()
    {
        Debug.Log("\n--- 扩展方法示例 ---");

        var result = Result<int, string>.Success(10);

        // Tap: 执行副作用但不改变结果
        var tappedResult = result.Tap(value => Debug.Log($"Tap: 当前值 {value}"));
        Debug.Log($"Tap后结果: {tappedResult.Value}");

        // TapError: 错误时执行副作用
        var errorResult = Result<int, string>.Failure("测试错误");
        var tappedError = errorResult.TapError(error => Debug.Log($"TapError: {error}"));
        Debug.Log($"TapError后错误: {tappedError.Error}");

        // Pipe: 管道操作
        var pipedResult = 5.Pipe(x => x * 2).Pipe(x => x + 3);
        Debug.Log($"管道操作结果: {pipedResult}");

        // 如果需要转换为字符串，需要分步进行
        var pipedString = 5.Pipe(x => x * 2).Pipe(x => x + 3).ToString();
        Debug.Log($"管道字符串结果: {pipedString}");

        // 扩展方法绑定
        var bindResult = Result<int, string>
            .Success(5)
            .Bind(x => Result<string, string>.Success($"扩展绑定: {x}"));
        Debug.Log($"扩展绑定: {bindResult.Value}");
    }
    #endregion

    #region 实用方法示例
    private void UtilityMethodsExample()
    {
        Debug.Log("\n--- 实用方法示例 ---");

        // 模拟实际业务场景
        var userResult = ValidateUser("admin", "password123");
        var processedResult = userResult
            .Bind(user => AuthenticateUser(user))
            .Bind(user => LoadUserProfile(user))
            .Tap(user => Debug.Log($"用户登录成功: {user.Name}"))
            .TapError(error => Debug.LogError($"登录失败: {error}"));

        // 模拟文件操作
        var fileResult = ReadFile("config.txt")
            .Bind(content => ParseConfig(content))
            .Map(config => $"配置加载成功: {config.Version}");

        Debug.Log($"文件操作结果: {fileResult.Value}");
    }

    // 模拟业务方法
    private Result<User, string> ValidateUser(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return Result<User, string>.Failure("用户名或密码不能为空");

        if (username.Length < 3)
            return Result<User, string>.Failure("用户名长度不足");

        return Result<User, string>.Success(new User { Username = username });
    }

    private Result<User, string> AuthenticateUser(User user)
    {
        if (user.Username == "admin")
            return Result<User, string>.Success(user);

        return Result<User, string>.Failure("认证失败");
    }

    private Result<User, string> LoadUserProfile(User user)
    {
        user.Name = "管理员";
        user.Email = "admin@example.com";
        return Result<User, string>.Success(user);
    }

    private Result<string, string> ReadFile(string filename)
    {
        if (filename == "config.txt")
            return Result<string, string>.Success("version=1.0\nname=test");

        return Result<string, string>.Failure("文件不存在");
    }

    private Result<Config, string> ParseConfig(string content)
    {
        if (content.Contains("version="))
            return Result<Config, string>.Success(new Config { Version = "1.0" });

        return Result<Config, string>.Failure("配置格式错误");
    }
    #endregion
}

// 辅助类
public class User
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class Config
{
    public string Version { get; set; }
}
