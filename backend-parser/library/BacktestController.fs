namespace Controllers

open Ast
open Parser
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Parser.Errors
open Parser.ParserResult

type Instrument =
    { Id: int
      Ticker: string
      Name: string }

[<ApiController>]
[<Route("[controller]/[action]")>]
type BacktestController(logger: ILogger<BacktestController>) =
    inherit ControllerBase()

    member private this.Parse(input: string) : Result<Backtest, ApiError> =
        input
        |> FParsec.CharParsers.run Backtest.parse
        |> ParserResult.toResult
        |> Result.teeError (fun e -> logger.LogWarning("Parsing failed {e}", e.Message))
        |> Result.mapError ApiError.ParserError

    member this.toIActionResult<'T, 'L>(logger: ILogger<'L>, item: TaskResult<'T, ApiError>) =
        item
        |> TaskResult.foldResult (fun result -> result |> this.Ok :> IActionResult) (fun apiError ->
            apiError |> this.UnprocessableEntity :> IActionResult)

    [<HttpPost>]
    member this.Parse([<FromBody>] content: Ast.Content) =
        this.Parse(content.Content)
        |> TaskResult.ofResult
        |> fun x -> this.toIActionResult (logger, x)

    [<HttpGet>]
    member this.Universe() =
        [ { Id = 1
            Ticker = "BTC"
            Name = "Bitcoin" }
          { Id = 2
            Ticker = "ETH"
            Name = "Ethereum" } ]
        |> this.Ok
