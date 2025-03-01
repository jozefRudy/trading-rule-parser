open System
open System.Text.Json
open System.Text.Json.Serialization
open Controllers
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

#nowarn "20"

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services
        .AddControllers()
        .AddApplicationPart(typeof<BacktestController>.Assembly)
        .AddJsonOptions(fun options ->
            options.JsonSerializerOptions.DictionaryKeyPolicy <- JsonNamingPolicy.CamelCase
            options.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
            options.JsonSerializerOptions.NumberHandling <- JsonNumberHandling.AllowNamedFloatingPointLiterals

            JsonFSharpOptions
                .Default()
                .WithUnionTagNamingPolicy(JsonNamingPolicy.CamelCase)
                .WithUnionFieldNamingPolicy(JsonNamingPolicy.CamelCase)
                .WithUnionNamedFields()
                .WithUnionTagName("case")
                .WithUnionFieldsName("fields")
                .WithIncludeRecordProperties()
                .WithUnionUnwrapRecordCases()
                .WithUnionUnwrapFieldlessTags()
                .WithUnionUnwrapSingleCaseUnions(false)
                .WithUnionUnwrapSingleFieldCases()
                .WithSkippableOptionFields()
                .AddToJsonSerializerOptions(options.JsonSerializerOptions))

    let app = builder.Build()
    app.UseCors(fun b -> b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore)

    app.MapControllers()
    app.Run()

    0 // Exit code
