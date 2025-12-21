namespace Warehouse.Core.Functional.Shared

open System
open System.Net

type Error(message: string, code: Nullable<HttpStatusCode>) =
    new(message: string) = Error(message, Nullable())
    member this.Message = message
    member this.Code = code
    override this.ToString() = if code.HasValue then $"[{code.Value}] %s{message}" else message

[<Struct>]
type Result<'T> =
    val private value: 'T
    val private error: Error
    val public IsSuccess: bool

    private new(value: 'T) = { value = value; error = null; IsSuccess = true }
    private new(error: Error) = { value = Unchecked.defaultof<'T>; error = error; IsSuccess = false }

    member this.Value =
        if this.IsSuccess then
            this.value
        else
            raise (InvalidOperationException $"Cannot access Value when Result is failure. Error: {this.error}")

    member this.Error =
        if not this.IsSuccess then
            this.error
        else
            raise (InvalidOperationException("Cannot access Error when Result is success."))

    static member Success(value: 'T) = Result<'T>(value)
    static member Failure(error: Error) = Result<'T>(error)
    static member Failure(message: string, code: HttpStatusCode) = Result<'T>(Error(message, Nullable(code)))
    static member Failure(message: string) = Result<'T>(Error(message))

    member this.Match<'TResult>(onSuccess: Func<'T, 'TResult>, onFailure: Func<Error, 'TResult>) =
        if this.IsSuccess then onSuccess.Invoke(this.value) else onFailure.Invoke(this.error)

    member this.OnFailure(action: Action<Error>) =
        if not this.IsSuccess then
            action.Invoke(this.error)

        this

    member this.OnSuccess(action: Action<'T>) =
        if this.IsSuccess then
            action.Invoke(this.value)

        this
