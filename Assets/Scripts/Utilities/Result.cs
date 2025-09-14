using System;

/// <summary>
/// A generic result type that can represent either success with a value or failure with an error message.
/// Helps avoid exception handling for expected error cases in the terminal emulator.
/// </summary>
/// <typeparam name="T">The type of the result value on success</typeparam>
public class Result<T>
{
  /// <summary>
  /// The success value (if IsSuccess is true)
  /// </summary>
  public T Data { get; private set; }

  /// <summary>
  /// The error message (if IsSuccess is false)
  /// </summary>
  public string ErrorMessage { get; private set; }

  /// <summary>
  /// Whether the operation was successful
  /// </summary>
  public bool IsSuccess { get; private set; }

  /// <summary>
  /// Whether the operation failed
  /// </summary>
  public bool IsFailure => !IsSuccess;

  private Result(T data, string errorMessage, bool isSuccess)
  {
    Data = data;
    ErrorMessage = errorMessage;
    IsSuccess = isSuccess;
  }

  /// <summary>
  /// Creates a successful result with the given data
  /// </summary>
  public static Result<T> Success(T data)
  {
    return new Result<T>(data, null, true);
  }

  /// <summary>
  /// Creates a failure result with the given error message
  /// </summary>
  public static Result<T> Failure(string errorMessage)
  {
    return new Result<T>(default, errorMessage, false);
  }

  /// <summary>
  /// Executes an action based on whether the result was successful or not
  /// </summary>
  public void Match(Action<T> onSuccess, Action<string> onFailure)
  {
    if (IsSuccess)
      onSuccess?.Invoke(Data);
    else
      onFailure?.Invoke(ErrorMessage);
  }

  /// <summary>
  /// Maps a successful result to a new result with a different type
  /// </summary>
  public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
  {
    if (IsSuccess)
      return Result<TNew>.Success(mapper(Data));
    else
      return Result<TNew>.Failure(ErrorMessage);
  }

  /// <summary>
  /// Returns the data if successful or the default value if not
  /// </summary>
  public T GetValueOrDefault(T defaultValue = default)
  {
    return IsSuccess ? Data : defaultValue;
  }
}
