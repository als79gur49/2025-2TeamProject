using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 기본 Result 패턴 구현
    /// 성공/실패를 명시적으로 처리하는 패턴
    /// </summary>
    [System.Serializable]
    public readonly struct Result
    {
        public readonly bool IsSuccess;
        public readonly bool IsFailure => !IsSuccess;
        public readonly string ErrorMessage;
        public readonly ResultErrorType ErrorType;
        public readonly Exception Exception;

        private Result(bool isSuccess, string errorMessage, ResultErrorType errorType, Exception exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? "";
            ErrorType = errorType;
            Exception = exception;
        }

        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public static Result Success() => new Result(true, "", ResultErrorType.None, null);

        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public static Result Failure(string errorMessage, ResultErrorType errorType = ResultErrorType.General) =>
            new Result(false, errorMessage, errorType, null);

        /// <summary>
        /// 예외를 포함한 실패 결과 생성
        /// </summary>
        public static Result Failure(Exception exception, ResultErrorType errorType = ResultErrorType.Exception) =>
            new Result(false, exception?.Message ?? "Unknown exception", errorType, exception);

        /// <summary>
        /// 결과가 성공인지 확인
        /// </summary>
        public void ThrowIfFailure()
        {
            if (IsFailure)
            {
                throw new InvalidOperationException($"Operation failed: {ErrorMessage}");
            }
        }

        /// <summary>
        /// 결과를 다른 타입으로 변환
        /// </summary>
        public Result<T> ToResult<T>(T value = default) =>
            IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(ErrorMessage, ErrorType);

        public override string ToString() =>
            IsSuccess ? "Success" : $"Failure: {ErrorMessage} (Type: {ErrorType})";

        public static implicit operator bool(Result result) => result.IsSuccess;
    }

    /// <summary>
    /// 값을 포함하는 Result 패턴 구현
    /// </summary>
    [System.Serializable]
    public readonly struct Result<T>
    {
        public readonly bool IsSuccess;
        public readonly bool IsFailure => !IsSuccess;
        public readonly T Value;
        public readonly string ErrorMessage;
        public readonly ResultErrorType ErrorType;
        public readonly Exception Exception;

        private Result(bool isSuccess, T value, string errorMessage, ResultErrorType errorType, Exception exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage ?? "";
            ErrorType = errorType;
            Exception = exception;
        }

        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public static Result<T> Success(T value) => new Result<T>(true, value, "", ResultErrorType.None, null);

        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public static Result<T> Failure(string errorMessage, ResultErrorType errorType = ResultErrorType.General) =>
            new Result<T>(false, default, errorMessage, errorType, null);

        /// <summary>
        /// 예외를 포함한 실패 결과 생성
        /// </summary>
        public static Result<T> Failure(Exception exception, ResultErrorType errorType = ResultErrorType.Exception) =>
            new Result<T>(false, default, exception?.Message ?? "Unknown exception", errorType, exception);

        /// <summary>
        /// 결과가 성공인지 확인하고 값 반환
        /// </summary>
        public T GetValueOrThrow()
        {
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot get value from failed result: {ErrorMessage}");
            }
            return Value;
        }

        /// <summary>
        /// 값 또는 기본값 반환
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default) => IsSuccess ? Value : defaultValue;

        /// <summary>
        /// 결과를 다른 타입으로 변환
        /// </summary>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (IsFailure)
                return Result<TNew>.Failure(ErrorMessage, ErrorType);
            
            try
            {
                return Result<TNew>.Success(mapper(Value));
            }
            catch (Exception ex)
            {
                return Result<TNew>.Failure(ex);
            }
        }

        /// <summary>
        /// 결과를 체이닝
        /// </summary>
        public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        {
            if (IsFailure)
                return Result<TNew>.Failure(ErrorMessage, ErrorType);

            try
            {
                return binder(Value);
            }
            catch (Exception ex)
            {
                return Result<TNew>.Failure(ex);
            }
        }

        /// <summary>
        /// 조건부 실행
        /// </summary>
        public Result<T> Do(Action<T> action)
        {
            if (IsSuccess)
            {
                try
                {
                    action(Value);
                }
                catch (Exception ex)
                {
                    return Result<T>.Failure(ex);
                }
            }
            return this;
        }

        /// <summary>
        /// 실패시 실행
        /// </summary>
        public Result<T> OnFailure(Action<string, ResultErrorType> onFailure)
        {
            if (IsFailure)
            {
                onFailure?.Invoke(ErrorMessage, ErrorType);
            }
            return this;
        }

        /// <summary>
        /// Result를 기본 Result로 변환
        /// </summary>
        public Result ToResult() =>
            IsSuccess ? Result.Success() : Result.Failure(ErrorMessage, ErrorType);

        public override string ToString() =>
            IsSuccess ? $"Success: {Value}" : $"Failure: {ErrorMessage} (Type: {ErrorType})";

        public static implicit operator bool(Result<T> result) => result.IsSuccess;
    }

    /// <summary>
    /// 에러 타입 열거형
    /// </summary>
    public enum ResultErrorType
    {
        None,               // 에러 없음
        General,            // 일반 에러
        Validation,         // 유효성 검증 에러
        NotFound,           // 대상을 찾을 수 없음
        Unauthorized,       // 권한 없음
        InvalidOperation,   // 잘못된 작업
        Timeout,            // 시간 초과
        Network,            // 네트워크 에러
        FileSystem,         // 파일 시스템 에러
        Database,           // 데이터베이스 에러
        Exception,          // 예외 발생
        Configuration,      // 설정 에러
        Resource,           // 리소스 에러
        Concurrency,        // 동시성 에러
        Business            // 비즈니스 로직 에러
    }

    /// <summary>
    /// Result 패턴 확장 메서드들
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// 여러 Result를 하나로 결합
        /// </summary>
        public static Result Combine(params Result[] results)
        {
            var errors = new List<string>();
            
            foreach (var result in results)
            {
                if (result.IsFailure)
                {
                    errors.Add(result.ErrorMessage);
                }
            }

            return errors.Count == 0 
                ? Result.Success() 
                : Result.Failure(string.Join("; ", errors), ResultErrorType.General);
        }

        /// <summary>
        /// 여러 Result<T>를 하나로 결합
        /// </summary>
        public static Result<T[]> Combine<T>(params Result<T>[] results)
        {
            var errors = new List<string>();
            var values = new List<T>();

            foreach (var result in results)
            {
                if (result.IsFailure)
                {
                    errors.Add(result.ErrorMessage);
                }
                else
                {
                    values.Add(result.Value);
                }
            }

            return errors.Count == 0
                ? Result<T[]>.Success(values.ToArray())
                : Result<T[]>.Failure(string.Join("; ", errors), ResultErrorType.General);
        }

        /// <summary>
        /// 조건부 Result 생성
        /// </summary>
        public static Result EnsureTrue(bool condition, string errorMessage, ResultErrorType errorType = ResultErrorType.Validation)
        {
            return condition ? Result.Success() : Result.Failure(errorMessage, errorType);
        }

        /// <summary>
        /// null 체크와 함께 Result 생성
        /// </summary>
        public static Result<T> EnsureNotNull<T>(T value, string errorMessage = null, ResultErrorType errorType = ResultErrorType.Validation)
            where T : class
        {
            return value != null 
                ? Result<T>.Success(value) 
                : Result<T>.Failure(errorMessage ?? "Value cannot be null", errorType);
        }

        /// <summary>
        /// try-catch를 Result로 래핑
        /// </summary>
        public static Result Try(Action action, ResultErrorType errorType = ResultErrorType.Exception)
        {
            try
            {
                action();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex, errorType);
            }
        }

        /// <summary>
        /// try-catch를 Result<T>로 래핑
        /// </summary>
        public static Result<T> Try<T>(Func<T> func, ResultErrorType errorType = ResultErrorType.Exception)
        {
            try
            {
                return Result<T>.Success(func());
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex, errorType);
            }
        }

        /// <summary>
        /// Unity의 null 체크 (UnityEngine.Object)
        /// </summary>
        public static Result<T> EnsureNotNullUnity<T>(T obj, string errorMessage = null) where T : UnityEngine.Object
        {
            return obj != null 
                ? Result<T>.Success(obj) 
                : Result<T>.Failure(errorMessage ?? $"{typeof(T).Name} is null or destroyed", ResultErrorType.Validation);
        }

        /// <summary>
        /// 컬렉션이 비어있지 않은지 확인
        /// </summary>
        public static Result<IEnumerable<T>> EnsureNotEmpty<T>(IEnumerable<T> collection, string errorMessage = null)
        {
            if (collection == null)
                return Result<IEnumerable<T>>.Failure("Collection is null", ResultErrorType.Validation);

            if (!collection.Any())
                return Result<IEnumerable<T>>.Failure(errorMessage ?? "Collection is empty", ResultErrorType.Validation);

            return Result<IEnumerable<T>>.Success(collection);
        }

        /// <summary>
        /// 범위 체크
        /// </summary>
        public static Result<T> EnsureInRange<T>(T value, T min, T max, string errorMessage = null) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                return Result<T>.Failure(
                    errorMessage ?? $"Value {value} is out of range [{min}, {max}]", 
                    ResultErrorType.Validation);
            }

            return Result<T>.Success(value);
        }

        /// <summary>
        /// Unity 전용 로그 출력
        /// </summary>
        public static Result LogOnFailure(this Result result, string prefix = "")
        {
            if (result.IsFailure)
            {
                var message = string.IsNullOrEmpty(prefix) 
                    ? result.ErrorMessage 
                    : $"{prefix}: {result.ErrorMessage}";
                
                switch (result.ErrorType)
                {
                    case ResultErrorType.Exception:
                        Debug.LogException(result.Exception);
                        break;
                    case ResultErrorType.Validation:
                    case ResultErrorType.InvalidOperation:
                        Debug.LogWarning(message);
                        break;
                    default:
                        Debug.LogError(message);
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Unity 전용 로그 출력 (값 포함)
        /// </summary>
        public static Result<T> LogOnFailure<T>(this Result<T> result, string prefix = "")
        {
            if (result.IsFailure)
            {
                var message = string.IsNullOrEmpty(prefix) 
                    ? result.ErrorMessage 
                    : $"{prefix}: {result.ErrorMessage}";
                
                switch (result.ErrorType)
                {
                    case ResultErrorType.Exception:
                        Debug.LogException(result.Exception);
                        break;
                    case ResultErrorType.Validation:
                    case ResultErrorType.InvalidOperation:
                        Debug.LogWarning(message);
                        break;
                    default:
                        Debug.LogError(message);
                        break;
                }
            }
            return result;
        }

        private static bool Any<T>(this IEnumerable<T> enumerable)
        {
            foreach (var _ in enumerable)
                return true;
            return false;
        }
    }

    /// <summary>
    /// Result 패턴을 사용하는 비동기 작업 헬퍼
    /// </summary>
    public static class AsyncResult
    {
        /// <summary>
        /// 비동기 작업을 Result로 래핑
        /// </summary>
        public static async System.Threading.Tasks.Task<Result> Try(System.Func<System.Threading.Tasks.Task> asyncAction)
        {
            try
            {
                await asyncAction();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }

        /// <summary>
        /// 값을 반환하는 비동기 작업을 Result로 래핑
        /// </summary>
        public static async System.Threading.Tasks.Task<Result<T>> Try<T>(System.Func<System.Threading.Tasks.Task<T>> asyncFunc)
        {
            try
            {
                var result = await asyncFunc();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex);
            }
        }
    }

    /// <summary>
    /// Unity Coroutine과 Result 패턴 통합
    /// </summary>
    public static class CoroutineResult
    {
        /// <summary>
        /// Coroutine 결과를 담는 컨테이너
        /// </summary>
        public class CoroutineResultContainer<T>
        {
            public Result<T> Result { get; set; }
            public bool IsCompleted { get; set; }
        }

        /// <summary>
        /// Coroutine을 Result 패턴으로 실행
        /// </summary>
        public static CoroutineResultContainer<T> Execute<T>(MonoBehaviour monoBehaviour, 
            System.Func<System.Collections.IEnumerator> coroutineFunc)
        {
            var container = new CoroutineResultContainer<T>();
            
            monoBehaviour.StartCoroutine(ExecuteCoroutine(coroutineFunc, container));
            
            return container;
        }

        private static System.Collections.IEnumerator ExecuteCoroutine<T>(
            System.Func<System.Collections.IEnumerator> coroutineFunc, 
            CoroutineResultContainer<T> container)
        {
            System.Collections.IEnumerator coroutine = null;
            Exception caughtException = null;
            
            // 코루틴 함수 실행을 안전하게 처리
            try
            {
                coroutine = coroutineFunc();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            
            if (caughtException != null)
            {
                container.Result = Result<T>.Failure(caughtException);
                container.IsCompleted = true;
                yield break;
            }
            
            if (coroutine == null)
            {
                container.Result = Result<T>.Failure("Coroutine function returned null", ResultErrorType.InvalidOperation);
                container.IsCompleted = true;
                yield break;
            }
            
            // try-catch 없이 코루틴 실행
            bool hasMore = true;
            while (hasMore)
            {
                bool moveNextSucceeded = false;
                Exception moveNextException = null;
                
                try
                {
                    hasMore = coroutine.MoveNext();
                    moveNextSucceeded = true;
                }
                catch (Exception ex)
                {
                    moveNextException = ex;
                }
                
                if (moveNextException != null)
                {
                    container.Result = Result<T>.Failure(moveNextException);
                    container.IsCompleted = true;
                    yield break;
                }
                
                if (moveNextSucceeded && hasMore)
                {
                    yield return coroutine.Current;
                }
            }
            
            container.Result = Result<T>.Success(default);
            container.IsCompleted = true;
        }
    }
}