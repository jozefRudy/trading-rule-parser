module Parser

open System
open Ast
open FParsec


let private pFloat = pfloat .>> spaces
let private pInt = pint32 .>> spaces
let private pLiteral input = pstring input .>> spaces

let private pLiteralStrict input = pstring input
let private pComma = pLiteral ","

let private pDot = pchar '.'
let private pLeftBracket = pLiteral "("
let private pRightBracket = pLiteral ")"

let private parens (p: Parser<'a, unit>) = between pLeftBracket pRightBracket p
let private pSemicolon = opt (pLiteral ";") .>> spaces
let private pQuote = pLiteral "\""
let private pSingleQuote = pLiteral "\'"

let private pIdentifier =
    many1Satisfy2L isLetter (fun c -> isLetter c || isDigit c || c = '_') "Expected identifier"
    .>> spaces

let private pMain = pLiteralStrict "main."

module Formula =

    let pFormula, pFormulaRef = createParserForwardedToRef<Formula, unit> ()

    module Initial =
        let priceDay = pLiteral "priceDay" >>. parens spaces >>% Indicator(Price(Day))

        let priceMinute =
            pLiteral "priceMinute" >>. parens spaces >>% Indicator(Price(Minute))

        let pConstant = pLiteral "constant" >>. parens pFloat |>> Constant

        let pIndicator = pMain >>. choice [ priceMinute; priceDay; pConstant ]

    module Nullary =
        let pNullaryFunc funcName func =
            pLiteral funcName >>. parens spaces >>% nullaryParamOp func

        let pHtTrendline = pNullaryFunc "oscillator.htTrendline" HtTrendline
        let pMamaFast = pNullaryFunc "oscillator.mamaFast" MamaFast
        let pMamaSlow = pNullaryFunc "oscillator.mamaSlow" MamaSlow

    module Unary =
        let pUnaryFunc funcName func =
            pLiteral funcName >>. parens pInt |>> singleParamOp func

        let pSma = pUnaryFunc "trend.sma" Sma
        let pEma = pUnaryFunc "trend.ema" Ema
        let pWma = pUnaryFunc "trend.wma" Wma
        let pDema = pUnaryFunc "trend.dema" Dema
        let pKama = pUnaryFunc "trend.kama" Kama
        let pTema = pUnaryFunc "trend.tema" Tema
        let pT3 = pUnaryFunc "trend.t3" T3
        let pTrima = pUnaryFunc "trend.trima" Trima
        let pRsi = pUnaryFunc "oscillator.rsi" Rsi
        let pDelay = pUnaryFunc "trend.lag" Lag
        let pMidPointOverPeriod = pUnaryFunc "trend.midpointOverPeriod" MidPointOverPeriod
        let pChandeMomentumOsc = pUnaryFunc "oscillator.chandeMomentumOsc" ChandeMomentumOsc
        let pMomentum = pUnaryFunc "oscillator.absoluteMomentum" Momentum
        let pRateOfChange = pUnaryFunc "oscillator.relativeMomentum" RateOfChange
        let pTrix = pUnaryFunc "oscillator.trix" Trix
        let pBollingerBands = pUnaryFunc "oscillator.bollingerBands" BollingerBands

    module Binary =
        let pBinaryFunc funcName func =
            pLiteral funcName >>. parens (pipe2 pInt (pComma >>. pInt) (binaryParamOp func))

        let pAbsolutePriceOsc = pBinaryFunc "oscillator.absolutePriceOsc" AbsolutePriceOsc

        let pPercentagePriceOsc =
            pBinaryFunc "oscillator.percentagePriceOsc" PercentagePriceOsc

    module Ternary =
        let pTernaryFunc funcName func =
            pLiteral funcName
            >>. parens (pipe3 pInt (pComma >>. pInt) (pComma >>. pInt) (ternaryParamOp func))

        let pMacd = pTernaryFunc "oscillator.macd" Macd
        let pStochRsi = pTernaryFunc "oscillator.stochasticRsi" StochRsi

    module ArithmeticOp =
        let pArithmeticOp funcName func =
            pLiteral funcName >>. parens pFormula |>> fun r l -> ArithmeticOp(l, func, r)

        let pAdd = pArithmeticOp "arrayOp.add" Add
        let pSubtract = pArithmeticOp "arrayOp.subtract" Subtract
        let pDivide = pArithmeticOp "arrayOp.divide" Divide
        let pMultiply = pArithmeticOp "arrayOp.multiply" Multiply

    let pChained =
        pipe2
            Initial.pIndicator
            (many (
                pDot
                >>. choice
                        [ Nullary.pHtTrendline
                          Nullary.pMamaFast
                          Nullary.pMamaSlow

                          Unary.pSma
                          Unary.pEma
                          Unary.pWma
                          Unary.pDema
                          Unary.pKama
                          Unary.pTema
                          Unary.pT3
                          Unary.pTrima
                          Unary.pRsi
                          Unary.pDelay
                          Unary.pMidPointOverPeriod
                          Unary.pChandeMomentumOsc
                          Unary.pMomentum
                          Unary.pRateOfChange
                          Unary.pTrix
                          Unary.pBollingerBands

                          Binary.pPercentagePriceOsc
                          Binary.pAbsolutePriceOsc

                          Ternary.pMacd
                          Ternary.pStochRsi

                          ArithmeticOp.pAdd
                          ArithmeticOp.pSubtract
                          ArithmeticOp.pMultiply
                          ArithmeticOp.pDivide ]
            ))
            (List.fold (fun acc fn -> fn acc))

    do pFormulaRef.Value <- pChained
    let parse = pFormula


module Logical =
    let pGT: Parser<Comparison, unit> = pLiteral ">" |>> fun _ -> GT
    let pLT = pLiteral "<" |>> fun _ -> LT
    let pEQ = pLiteral "==" |>> fun _ -> EQ
    let pGTE = pLiteral ">=" |>> fun _ -> GTE
    let pLTE = pLiteral "<=" |>> fun _ -> LTE

    let pComparison = choice [ pGTE; pLTE; pGT; pLT; pEQ ]

    let pRule: Parser<Logical, unit> =
        pipe3 Formula.parse pComparison Formula.parse compoundFormula

    let pNever = pLiteral "never" >>. parens spaces |>> (fun _ -> Never)

    let pAlways = pLiteral "always" >>. parens spaces |>> (fun _ -> Always)

    module OperatorPrecedenceParser =
        let opp = OperatorPrecedenceParser<Logical, unit, unit>()
        let expr = opp.ExpressionParser

        opp.TermParser <- choice [ attempt pRule; pMain >>. choice [ pNever; pAlways ]; parens expr ]

        opp.AddOperator(InfixOperator("&&", spaces, 1, Associativity.Left, (fun l r -> compoundLogical l And r)))
        opp.AddOperator(InfixOperator("||", spaces, 1, Associativity.Left, (fun l r -> compoundLogical l Or r)))
        let parse = expr .>> pSemicolon

    let parse = OperatorPrecedenceParser.parse

module Universe =
    let pLeftSquareBracket = pLiteral "["
    let pRightSquareBracket = pLiteral "]"

    let validateItemCount items =
        if List.isEmpty items then
            fail "Universe must contain at least one instrument"
        elif List.length items > 20 then
            fail "Universe cannot contain more than 20 instruments"
        elif Set.count (Set.ofSeq items) <> List.length items then
            fail "Universe cannot contain duplicate instruments"
        else
            preturn items

    let parse: Parser<StringUniverse, unit> =
        let pItemList = sepEndBy (pLiteralStrict "Instrument." >>. pIdentifier) pComma

        between pLeftSquareBracket pRightSquareBracket pItemList .>> opt pSemicolon
        >>= validateItemCount
        |>> StringUniverse


module ModeParser =
    let parse =
        pLiteral "Mode." >>. pIdentifier .>> pSemicolon
        >>= function
            | "Backtest" -> preturn Backtest
            | "Live" -> preturn Live
            | _ -> fail "Invalid mode"


module QuotedStringParser =
    let parse =
        choice
            [ pQuote >>. pIdentifier .>> pQuote
              pSingleQuote >>. pIdentifier .>> pSingleQuote ]
        .>> opt pSemicolon

module PeriodParser =
    let pDate =
        pipe3
            (pLiteral "new Date" >>. pLeftBracket >>. pInt .>> pComma)
            (pInt .>> pComma)
            (pInt .>> pRightBracket .>> opt pSemicolon)
            (fun year month day -> (year, month, day))

    let tryCreateDateOnly year month day =
        if month < 1 || month > 12 then
            Result.Error $"Invalid month {month}"
        elif day < 1 || day > 31 then
            Result.Error $"Invalid day {day}"
        else
            let date = DateOnly(year, month, day)
            let minimumDate = DateOnly(1996, 1, 1)

            if date < minimumDate then
                Result.Error $"Start date cannot be earlier than {minimumDate}"
            else
                Result.Ok(date)

    let parse: Parser<Period, unit> =
        pDate
        >>= fun (y, m, d) ->
            match tryCreateDateOnly y m d with
            | Result.Ok start -> preturn start
            | Result.Error errMsg -> fail $"Date error: {errMsg}"

module Declaration =
    let pExport = pLiteral "export"
    let pConst = pLiteral "const"
    let pLet = pLiteral "let"
    let pEquals = pLiteral "="

    let parse: Parser<Declaration, unit> =
        (opt pExport >>. choice [ pConst; pLet ] >>. pIdentifier .>> pEquals)
        >>= fun identifier ->
            choice
                [ Logical.parse |>> (fun x -> Logical(identifier, x))
                  Universe.parse |>> (fun x -> Universe(identifier, x))
                  QuotedStringParser.parse |>> (fun x -> StrategyName(identifier, x))
                  PeriodParser.parse |>> (fun x -> StartPeriod(identifier, x))
                  ModeParser.parse |>> (fun x -> Mode(identifier, x)) ]
            .>> pSemicolon

module Import =
    let pImportKeyword = pLiteral "import"
    let pFrom = pLiteral "from"

    let private pImport: Parser<unit, unit> =
        pImportKeyword
        >>. pLiteral "{"
        >>. sepEndBy pIdentifier pComma
        >>. pLiteral "}"
        >>. pFrom
        >>. skipManyTill anyChar (pLiteral ";")
        >>% ()

    let parse = pImport >>% ()

module File =
    let parse: Parser<File, unit> =
        spaces >>. many Import.parse >>. many Declaration.parse .>> eof

module Backtest =
    let private getVarDefinition (declarations: Declaration list) (var: string) : Result<Declaration, string> =
        declarations
        |> List.tryFind (fun declaration ->
            declaration
            |> function
                | Logical(name, _) when name = var -> true
                | Universe(name, _) when name = var -> true
                | StrategyName(name, _) when name = var -> true
                | StartPeriod(name, _) when name = var -> true
                | Mode(name, _) when name = var -> true
                | _ -> false)
        |> function
            | Some(x) -> Result.Ok x
            | _ -> Result.Error $"variable {var} must be defined"

    let private getVarNames (declarations: Declaration list) : string list =
        declarations
        |> List.choose (function
            | Logical(name, _) -> Some name
            | Universe(name, _) -> Some name
            | StrategyName(name, _) -> Some name
            | StartPeriod(name, _) -> Some name
            | Mode(name, _) -> Some name)

    let private areUnique (vars: string list) : bool =
        vars |> Set.ofList |> Set.count = List.length vars

    let private varsUnique = getVarNames >> areUnique

    let private varsMustBeUnique x =
        if x |> getVarNames |> areUnique |> not then
            fail "variable names must be unique"
        else
            preturn ()

    let private (|Logical|) (declaration: Declaration) =
        match declaration with
        | Logical(_, x) -> Some x
        | _ -> None

    let private (|Universe|) (declaration: Declaration) =
        match declaration with
        | Universe(_, x) -> Some x
        | _ -> None

    let private (|StrategyName|) (declaration: Declaration) =
        match declaration with
        | StrategyName(_, x) -> Some x
        | _ -> None

    let private (|Period|) (declaration: Declaration) =
        match declaration with
        | StartPeriod(_, x) -> Some x
        | _ -> None

    let private (|Mode|) (declaration: Declaration) =
        match declaration with
        | Mode(_, x) -> Some x
        | _ -> None

    let parse =
        parse {
            let! file = File.parse

            do! varsMustBeUnique file

            let since = "start" |> getVarDefinition file

            let definitions =
                [ "entry"; "exit"; "universe"; "name"; "mode" ]
                |> List.map (getVarDefinition file)

            match definitions with
            | [ Result.Ok entry; Result.Ok exit; Result.Ok universe; Result.Ok name; Result.Ok mode ] ->
                match (entry, exit, universe, name, mode) with
                | Logical(Some entry),
                  Logical(Some exit),
                  Universe(Some universe),
                  StrategyName(Some name),
                  Mode(Some mode) ->
                    match mode with
                    | Live ->
                        return
                            { Mode = mode
                              Since = None
                              Entry = entry
                              Exit = exit
                              Universe = universe
                              Name = name }

                    | Backtest ->
                        match since with
                        | Result.Ok(Period(Some since)) ->
                            return
                                { Mode = mode
                                  Since = since |> Some
                                  Entry = entry
                                  Exit = exit
                                  Universe = universe
                                  Name = name }

                        | _ ->
                            return!
                                fail "start must be defined in backtest mode as valid date, e.g. new Date(2020, 1, 1)"
                | _ ->
                    return!
                        fail "entry and exit must be logical, universe must list of instruments, name must be a string"
            | errors ->
                return!
                    fail (
                        errors
                        |> List.choose (function
                            | Result.Error error -> Some error
                            | _ -> None)
                        |> String.concat "; "
                    )

        }

module ParserResult =
    type ParserError =
        { Message: string
          Line: int64
          Column: int64
          Index: int64 }

    let private convertParserError (parseError: FParsec.Error.ParserError) =
        { Message =
            parseError.ToString()
            |> fun x -> x.Split "\n" |> Array.tail |> String.concat "\n"
          Line = parseError.Position.Line
          Column = parseError.Position.Column
          Index = parseError.Position.Index }

    let toResult<'T> (result: FParsec.CharParsers.ParserResult<'T, unit>) : Result<'T, ParserError> =
        match result with
        | ParserResult.Success(x, _, _) -> Result.Ok x
        | ParserResult.Failure(_, error, _) -> Result.Error(convertParserError error)

module Errors =
    type ApiError = ParserError of ParserResult.ParserError
