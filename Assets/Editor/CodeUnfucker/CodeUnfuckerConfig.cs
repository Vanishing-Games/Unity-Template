using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CodeUnfuckerConfig
{
    public DotnetPathConfig dotnetPaths = new();
}

[System.Serializable]
public class DotnetPathConfig
{
    public List<string> environmentVariables = new() { "DOTNET_ROOT", "DOTNET_CLI_HOME" };
    public List<string> defaultSearchPaths = new()
    {
        "/opt/homebrew/bin/dotnet", // Homebrew on Apple Silicon
        "/usr/local/bin/dotnet", // Homebrew on Intel Mac
        "/usr/local/share/dotnet/dotnet", // Microsoft installer
        "/usr/bin/dotnet", // System installation
        "dotnet", // 如果在 PATH 中
    };
    public List<string> customPaths = new();
}
