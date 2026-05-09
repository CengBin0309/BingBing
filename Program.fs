open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    match Environment.GetEnvironmentVariable("PORT") with
    | null | "" -> ()
    | port -> builder.WebHost.UseUrls(sprintf "http://0.0.0.0:%s" port) |> ignore

    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    CourseWorkbench.App.mapRoutes app
    app.Run()
    0
