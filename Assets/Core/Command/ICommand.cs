using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// 命令接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommand<T>
    {
        public T Execute();
    }

    /// <summary>
    /// 可撤销命令接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUndoableCommand<T>
    {
        public T Undo();
    }

    /// <summary>
    /// 触发命令接口, 用于无返回值的命令, 只返回命令执行成功与否
    /// </summary>
    public interface ITriggerCommand : ICommand<bool> { }

    /// <summary>
    /// 异步命令接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncCommand<T> : ICommand<T>
    {
        public Task<T> ExecuteAsync();
    }

    /// <summary>
    /// UniTask 异步命令接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUniTaskCommand<T> : ICommand<T>
    {
        public UniTask<T> ExecuteAsync();
    }
}
