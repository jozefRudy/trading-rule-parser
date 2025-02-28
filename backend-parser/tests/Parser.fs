module Parser

open System
open Ast
open Parser
open FParsec
open Xunit

let assertSuccess<'T> result (expected: 'T) =
    match result with
    | Success(formula, _, _) -> Assert.Equal(expected, formula)
    | Failure(errMsg, _, _) -> Assert.True(false, $"Parsing failed: {errMsg}")

let assertFailure result expectedErrMsg =
    match result with
    | Success _ -> Assert.False(true, "Parsing succeeded but was expected to fail.")
    | Failure(errMsg, _, _) ->
        match expectedErrMsg with
        | "" -> Assert.True(true)
        | _ -> Assert.Contains(expectedErrMsg, errMsg)


[<Fact>]
let ``Test parsing formula`` () =
    let indicatorMinute = Indicator(Price(Minute))
    let indicatorDay = Indicator(Price(Day))

    assertSuccess
        (run Formula.parse "main.priceMinute().trend.sma(3 ).arrayOp.add( main.priceDay().trend.sma(3))")
        (arithmeticOp (singleParamOp Sma 3 indicatorMinute) Add (singleParamOp Sma 3 indicatorDay))

    assertSuccess (run Formula.parse "main.priceMinute().trend.sma( 3    )") (singleParamOp Sma 3 indicatorMinute)

    assertSuccess (run Formula.parse "main.priceMinute().oscillator.rsi(14   )") (singleParamOp Rsi 14 indicatorMinute)

    assertSuccess (run Formula.parse "main.priceMinute().trend.lag(2   )") (singleParamOp Lag 2 indicatorMinute)

    assertSuccess (run Formula.parse "main.priceMinute()") indicatorMinute
    assertSuccess (run Formula.parse "main.priceMinute()  ") indicatorMinute

    assertSuccess (run Formula.parse "main.constant( 3.14 )") (Constant 3.14)
    assertSuccess (run Formula.parse "main.constant( 3 )") (Constant 3)

    assertSuccess
        (run Formula.parse "main.priceMinute().trend.sma(3).trend.sma(3)")
        (singleParamOp Sma 3 (singleParamOp Sma 3 indicatorMinute))

    assertSuccess
        (run Formula.parse "main.priceMinute().arrayOp.add(main.priceMinute() )")
        (arithmeticOp indicatorMinute Add indicatorMinute)

    assertSuccess
        (run Formula.parse "main.priceMinute().arrayOp.add(main.priceMinute()).trend.sma(3)")
        (singleParamOp Sma 3 (arithmeticOp indicatorMinute Add indicatorMinute))

[<Fact>]
let ``Test parsing logical expression`` () =
    let indicator = Indicator(Price(Minute))

    assertSuccess
        (run Logical.parse "main.priceMinute().oscillator.rsi(2) > main.constant(30.0)")
        (compoundFormula (singleParamOp Rsi 2 indicator) GT (Constant 30.0))

    assertSuccess
        (run
            Logical.parse
            "( (main.priceMinute().oscillator.rsi(2) >= main.constant(30.0)) || (main.priceMinute() == main.priceMinute()) )")
        (compoundLogical
            (compoundFormula (singleParamOp Rsi 2 indicator) GTE (Constant 30.0))
            Or
            (compoundFormula indicator EQ indicator))

    assertSuccess
        (run
            Logical.parse
            "( main.priceMinute().oscillator.rsi(2) >= main.constant(30.0) ) || ( main.priceMinute() == main.priceMinute() && main.priceMinute() == main.priceMinute() )")
        (compoundLogical
            (compoundFormula (singleParamOp Rsi 2 indicator) GTE (Constant 30.0))
            Or
            (compoundLogical (compoundFormula indicator EQ indicator) And (compoundFormula indicator EQ indicator)))


    assertSuccess
        (run Logical.parse "( ( main.never() || main.priceMinute() == main.priceMinute()) && main.always())")
        (compoundLogical (compoundLogical Never Or (compoundFormula indicator EQ indicator)) And Always)

[<Fact>]
let ``Test parsing universe`` () =
    assertSuccess
        (run Universe.parse "[Instrument.a, Instrument.b,Instrument.c, Instrument.d,Instrument.e]")
        (StringUniverse [ "a"; "b"; "c"; "d"; "e" ])

    assertSuccess
        (run Universe.parse "[Instrument.a, Instrument.b,Instrument.c, Instrument.d,]")
        (StringUniverse [ "a"; "b"; "c"; "d" ])

[<Fact>]
let ``Test parsing strategy name`` () =
    assertSuccess (run QuotedStringParser.parse "\"strategy1\"") "strategy1"
    assertSuccess (run QuotedStringParser.parse "\'strategy1\'") "strategy1"

[<Fact>]
let ``Test parsing date`` () =
    assertSuccess (run PeriodParser.parse "new Date( 2020, 1, 1 );") (Period(2020, 1, 1))


[<Fact>]
let ``Test parsing mode`` () =
    assertSuccess (run ModeParser.parse "Mode.Backtest") Backtest


[<Fact>]
let ``Test parsing file`` () =
    assertSuccess
        (run
            File.parse
            "  import { random_thing, another_one } from \"./universe\"; import {} from \"./helper-methods\"; export const entry = main.never(); export const u = [Instrument.BTCUSDT, Instrument.ETHUSDT];")
        ([ Declaration.Logical("entry", Never)
           Declaration.Universe("u", StringUniverse [ "BTCUSDT"; "ETHUSDT" ]) ])

    assertSuccess
        (run File.parse "export const entry = main.never(); export const strategyName = \"strategy\"; ")
        ([ Declaration.Logical("entry", Never)
           Declaration.StrategyName("strategyName", "strategy") ])

    assertSuccess
        (run File.parse "export const entry = main.never(); export const exit = main.never();")
        ([ Declaration.Logical("entry", Never); Declaration.Logical("exit", Never) ])



[<Fact>]
let ``Test backtest`` () =
    assertSuccess
        (run
            Backtest.parse
            " export const exit = main.never(); export  const  entry=   main.never(); export const universe = [Instrument.BTCUSDT]; export const name = \'strategy\'; export const start= new Date(2020, 1, 1); export const mode = Mode.Live;")
        { Mode = Live
          Entry = Never
          Exit = Never
          Universe = StringUniverse [ "BTCUSDT" ]
          Name = "strategy"
          Since = None }



[<Fact>]
let ``Test parsing failures`` () =
    assertFailure (run Formula.parse "main.constant(3.14") ""
    assertFailure (run Formula.parse "main.priceMinute().trend.sma(3") ""
    assertFailure (run Formula.parse "main.priceMinute().trend.sma(3.12)") ""
    assertFailure (run Formula.parse "main.priceMinute().trend.sma(3") ""

    assertFailure
        (run
            Backtest.parse
            "export const entry = main.never(); export  const  exit=   main.never(); export  const  entry=   main.never();")
        "variable names must be unique"

    assertFailure (run PeriodParser.parse "new Date(2021, 13, 1);") "Date error"

    assertFailure (run ModeParser.parse "Backtest")