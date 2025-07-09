using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class Result<T, E>
    {
        private readonly T m_Value;
        private readonly E m_Error;
        public bool IsSuccess { get; }

        private Result(T value, E error, bool isSuccess)
        {
            m_Value = value;
            m_Error = error;
            IsSuccess = isSuccess;
        }

        public T Value
        {
            get
            {
                if (!IsSuccess)
                    throw new InvalidOperationException("无法从失败的结果中获取 Value。");
                return m_Value;
            }
        }

        public E Error
        {
            get
            {
                if (IsSuccess)
                    throw new InvalidOperationException("无法从成功的结果中获取 Error。");
                return m_Error;
            }
        }

        public static Result<T, E> Success(T value) => new Result<T, E>(value, default, true);

        public static Result<T, E> Failure(E error) => new Result<T, E>(default, error, false);

        public Result<TNew, E> Map<TNew>(Func<T, TNew> mapper)
        {
            return IsSuccess
                ? Result<TNew, E>.Success(mapper(m_Value))
                : Result<TNew, E>.Failure(m_Error);
        }

        public Result<T, ENew> MapError<ENew>(Func<E, ENew> errorMapper)
        {
            return IsSuccess
                ? Result<T, ENew>.Success(m_Value)
                : Result<T, ENew>.Failure(errorMapper(m_Error));
        }

        public Result<TResult, E> Bind<TResult>(Func<T, Result<TResult, E>> func)
        {
            return IsSuccess ? func(m_Value) : Result<TResult, E>.Failure(m_Error);
        }

        public T GetValueOrDefault(T defaultValue = default)
        {
            return IsSuccess ? m_Value : defaultValue;
        }

        public T GetValueOrThrow()
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Result is not successful. Error: {m_Error}");
            return m_Value;
        }

        public void Match(Action<T> onSuccess, Action<E> onError)
        {
            if (IsSuccess)
                onSuccess(m_Value);
            else
                onError(m_Error);
        }

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onError)
        {
            return IsSuccess ? onSuccess(m_Value) : onError(m_Error);
        }
    }

    public static class Result
    {
        public static Result<T, string> Success<T>(T value) => Result<T, string>.Success(value);

        public static Result<T, string> Failure<T>(string error) =>
            Result<T, string>.Failure(error);

        public static Result<T, Exception> TryExecute<T>(Func<T> action)
        {
            try
            {
                return Result<T, Exception>.Success(action());
            }
            catch (Exception ex)
            {
                return Result<T, Exception>.Failure(ex);
            }
        }

        public static Result<T, E> SafelyExecute<T, E>(Func<T> function, E error)
        {
            try
            {
                return Result<T, E>.Success(function());
            }
            catch
            {
                return Result<T, E>.Failure(error);
            }
        }

        public static async Task<Result<T, Exception>> TryExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return Result<T, Exception>.Success(await action());
            }
            catch (Exception ex)
            {
                return Result<T, Exception>.Failure(ex);
            }
        }

        public static async Task<Result<T, E>> SafelyExecuteAsync<T, E>(
            Func<Task<T>> function,
            E error
        )
        {
            try
            {
                return Result<T, E>.Success(await function());
            }
            catch
            {
                return Result<T, E>.Failure(error);
            }
        }
    }

    public static class ResultExtensions
    {
        public static Result<Tout, E> Bind<Tin, Tout, E>(
            this Result<Tin, E> input,
            Func<Tin, Result<Tout, E>> bindFunc
        )
        {
            return input.IsSuccess ? bindFunc(input.Value) : Result<Tout, E>.Failure(input.Error);
        }

        public static async Task<Result<TOut, E>> BindAsync<TIn, TOut, E>(
            this Result<TIn, E> input,
            Func<TIn, Task<Result<TOut, E>>> bindFuncAsync
        )
        {
            return input.IsSuccess
                ? await bindFuncAsync(input.Value)
                : Result<TOut, E>.Failure(input.Error);
        }

        public static Result<T, E> Tap<T, E>(this Result<T, E> result, Action<T> action)
        {
            if (result.IsSuccess)
                action(result.Value);
            return result;
        }

        public static Result<T, E> TapError<T, E>(this Result<T, E> result, Action<E> action)
        {
            if (!result.IsSuccess)
                action(result.Error);
            return result;
        }

        public static async Task<Result<T, E>> TapAsync<T, E>(
            this Result<T, E> result,
            Func<T, Task> action
        )
        {
            if (result.IsSuccess)
                await action(result.Value);
            return result;
        }

        public static async Task<Result<T, E>> TapErrorAsync<T, E>(
            this Result<T, E> result,
            Func<E, Task> action
        )
        {
            if (!result.IsSuccess)
                await action(result.Error);
            return result;
        }

        public static T Pipe<T>(this T source, params Func<T, T>[] funcs)
        {
            return funcs.Aggregate(source, (current, func) => func(current));
        }
    }
}
