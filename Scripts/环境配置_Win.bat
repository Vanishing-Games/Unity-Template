@echo off
chcp 65001 >nul
echo ========================================
echo Unity Template 项目环境配置脚本 (Windows)
echo ========================================
echo.

:: 设置错误处理
setlocal enabledelayedexpansion

:: 检查管理员权限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [WARNING] 建议以管理员身份运行此脚本以获得最佳体验
    echo.
)

:: 1. 检查并安装 .NET SDK
echo [1/3] 检查 .NET SDK...
dotnet --version >nul 2>&1
if %errorLevel% neq 0 (
    echo .NET SDK 未安装，正在下载安装...
    echo 请访问 https://dotnet.microsoft.com/download 下载最新版本的 .NET SDK
    echo 或者使用 winget 安装: winget install Microsoft.DotNet.SDK.8
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%a in ('dotnet --version') do set dotnet_version=%%a
    echo ✓ .NET SDK 已安装，版本: !dotnet_version!
)
echo.

:: 2. 安装 Roslynator
echo [2/3] 安装 Roslynator...
dotnet tool list -g | findstr "roslynator" >nul 2>&1
if %errorLevel% neq 0 (
    echo 正在安装 Roslynator 全局工具...
    dotnet tool install -g roslynator.dotnet.cli
    if %errorLevel% equ 0 (
        echo ✓ Roslynator 安装成功
    ) else (
        echo ✗ Roslynator 安装失败
        exit /b 1
    )
) else (
    echo ✓ Roslynator 已安装
    echo 正在更新到最新版本...
    dotnet tool update -g roslynator.dotnet.cli
)
echo.

:: 3. 安装 CSharpier
echo [3/3] 安装 CSharpier...
dotnet tool list -g | findstr "csharpier" >nul 2>&1
if %errorLevel% neq 0 (
    echo 正在安装 CSharpier 全局工具...
    dotnet tool install -g csharpier
    if %errorLevel% equ 0 (
        echo ✓ CSharpier 安装成功
    ) else (
        echo ✗ CSharpier 安装失败
        exit /b 1
    )
) else (
    echo ✓ CSharpier 已安装
    echo 正在更新到最新版本...
    dotnet tool update -g csharpier
)
echo.

:: 验证安装
echo ========================================
echo 验证安装结果:
echo ========================================
echo .NET SDK 版本:
dotnet --version
echo.
echo Roslynator 版本:
roslynator --version 2>nul || echo "Roslynator 命令不可用"
echo.
echo CSharpier 版本:
dotnet csharpier --version 2>nul || echo "CSharpier 命令不可用"
echo.

:: 环境变量提示
echo ========================================
echo 环境变量配置:
echo ========================================
echo 如果工具命令无法使用，请确保以下路径在 PATH 环境变量中:
echo - %%USERPROFILE%%\.dotnet\tools
echo.
echo 可以通过以下命令添加到当前会话:
echo set PATH=%%PATH%%;%%USERPROFILE%%\.dotnet\tools
echo.

echo ========================================
echo 环境配置完成！
echo ========================================
echo 现在可以使用以下命令:
echo - dotnet build       : 构建 .NET 项目
echo - roslynator analyze : 代码分析
echo - dotnet csharpier   : 代码格式化
echo.
pause
