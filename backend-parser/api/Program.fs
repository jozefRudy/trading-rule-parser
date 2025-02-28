open System
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

    let app = builder.Build()
    app.MapControllers()
    app.Run()

    0 // Exit code
