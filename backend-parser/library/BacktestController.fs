namespace Controllers

open Parser
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<ApiController>]
[<Route("[controller]/[action]")>]
type BacktestController(logger: ILogger<BacktestController>) =
    inherit ControllerBase()

    member private this.Parse(input: string) =
        input
        |> FParsec.CharParsers.run Backtest.parse
        |> ParserResult.toResult
        |> Result.teeError (fun e -> logger.LogWarning("Parsing failed {e}", e.Message))

    member this.toIActionResult<'T, 'L>(logger: ILogger<'L>, item: TaskResult<'T, ParserResult.ParserError>) =
        item
        |> TaskResult.foldResult (fun result -> result |> this.Ok :> IActionResult) (fun apiError ->
            apiError |> this.UnprocessableEntity :> IActionResult)

    [<HttpGet>]
    member this.Parse([<FromBody>] content: Ast.Content) =
        this.Parse(content.Content)
        |> TaskResult.ofResult
        |> fun x -> this.toIActionResult (logger, x)

    [<HttpGet>]
    member this.Hi() = this.Ok("hi")
