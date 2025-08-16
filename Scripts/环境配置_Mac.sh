#!/bin/bash

# 设置颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "========================================"
echo "Unity Template 项目环境配置脚本 (macOS)"
echo "========================================"
echo

# 错误处理函数
check_command() {
    if command -v "$1" &> /dev/null; then
        echo -e "${GREEN}✓${NC} $1 已安装"
        return 0
    else
        echo -e "${RED}✗${NC} $1 未安装"
        return 1
    fi
}

# 检查是否有sudo权限
check_sudo() {
    if sudo -n true 2>/dev/null; then
        echo -e "${GREEN}✓${NC} 已有管理员权限"
    else
        echo -e "${YELLOW}⚠${NC} 可能需要输入密码来获取管理员权限"
    fi
    echo
}

# 1. 检查并安装 Homebrew（如果需要的话）
echo -e "${BLUE}[0/4]${NC} 检查 Homebrew..."
if ! command -v brew &> /dev/null; then
    echo "Homebrew 未安装，正在安装..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    
    # 添加到PATH（针对Apple Silicon Mac）
    if [[ $(uname -m) == "arm64" ]]; then
        echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zprofile
        eval "$(/opt/homebrew/bin/brew shellenv)"
    else
        echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zprofile
        eval "$(/usr/local/bin/brew shellenv)"
    fi
    
    if command -v brew &> /dev/null; then
        echo -e "${GREEN}✓${NC} Homebrew 安装成功"
    else
        echo -e "${RED}✗${NC} Homebrew 安装失败"
        exit 1
    fi
else
    echo -e "${GREEN}✓${NC} Homebrew 已安装"
    # 更新 Homebrew
    echo "正在更新 Homebrew..."
    brew update
fi
echo

# 2. 检查并安装 .NET SDK
echo -e "${BLUE}[1/4]${NC} 检查 .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "正在安装 .NET SDK..."
    brew install --cask dotnet
    
    if command -v dotnet &> /dev/null; then
        echo -e "${GREEN}✓${NC} .NET SDK 安装成功"
    else
        echo -e "${RED}✗${NC} .NET SDK 安装失败"
        exit 1
    fi
else
    dotnet_version=$(dotnet --version)
    echo -e "${GREEN}✓${NC} .NET SDK 已安装，版本: $dotnet_version"
fi
echo

# 3. 安装 Roslynator
echo -e "${BLUE}[2/4]${NC} 安装 Roslynator..."
if ! dotnet tool list -g | grep -q "roslynator"; then
    echo "正在安装 Roslynator 全局工具..."
    dotnet tool install -g roslynator.dotnet.cli
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} Roslynator 安装成功"
    else
        echo -e "${RED}✗${NC} Roslynator 安装失败"
        exit 1
    fi
else
    echo -e "${GREEN}✓${NC} Roslynator 已安装"
    echo "正在更新到最新版本..."
    dotnet tool update -g roslynator.dotnet.cli
fi
echo

# 4. 安装 CSharpier
echo -e "${BLUE}[3/4]${NC} 安装 CSharpier..."
if ! dotnet tool list -g | grep -q "csharpier"; then
    echo "正在安装 CSharpier 全局工具..."
    dotnet tool install -g csharpier
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} CSharpier 安装成功"
    else
        echo -e "${RED}✗${NC} CSharpier 安装失败"
        exit 1
    fi
else
    echo -e "${GREEN}✓${NC} CSharpier 已安装"
    echo "正在更新到最新版本..."
    dotnet tool update -g csharpier
fi
echo

# 5. 配置环境变量
echo -e "${BLUE}[4/4]${NC} 配置环境变量..."

# 获取当前shell
current_shell=$(basename "$SHELL")
case $current_shell in
    "bash")
        profile_file="$HOME/.bash_profile"
        ;;
    "zsh")
        profile_file="$HOME/.zshrc"
        ;;
    *)
        profile_file="$HOME/.profile"
        ;;
esac

# 检查.NET工具路径是否在PATH中
dotnet_tools_path="$HOME/.dotnet/tools"
if [[ ":$PATH:" != *":$dotnet_tools_path:"* ]]; then
    echo "添加 .NET 工具路径到 PATH..."
    echo "export PATH=\"\$PATH:$dotnet_tools_path\"" >> "$profile_file"
    export PATH="$PATH:$dotnet_tools_path"
    echo -e "${GREEN}✓${NC} 已添加 .NET 工具路径到 $profile_file"
else
    echo -e "${GREEN}✓${NC} .NET 工具路径已在 PATH 中"
fi
echo

# 验证安装
echo "========================================"
echo "验证安装结果:"
echo "========================================"
echo ".NET SDK 版本:"
dotnet --version
echo

echo "Roslynator 版本:"
if command -v roslynator &> /dev/null; then
    roslynator --version
else
    echo "Roslynator 命令不可用，请重新启动终端或运行: source $profile_file"
fi
echo

echo "CSharpier 版本:"
if command -v dotnet &> /dev/null && dotnet tool list -g | grep -q "csharpier"; then
    dotnet csharpier --version 2>/dev/null || echo "CSharpier 已安装但版本命令不可用"
else
    echo "CSharpier 命令不可用，请重新启动终端或运行: source $profile_file"
fi
echo

# 环境变量提示
echo "========================================"
echo "环境变量配置:"
echo "========================================"
echo "如果工具命令无法使用，请运行以下命令或重新启动终端:"
echo -e "${YELLOW}source $profile_file${NC}"
echo
echo "或者手动添加以下路径到 PATH 环境变量:"
echo -e "${YELLOW}export PATH=\"\$PATH:$HOME/.dotnet/tools\"${NC}"
echo

echo "========================================"
echo -e "${GREEN}环境配置完成！${NC}"
echo "========================================"
echo "现在可以使用以下命令:"
echo "- dotnet build       : 构建 .NET 项目"
echo "- roslynator analyze : 代码分析"
echo "- dotnet csharpier   : 代码格式化"
echo
echo -e "${YELLOW}注意: 如果这是首次安装，请重新启动终端以确保所有环境变量生效。${NC}" 