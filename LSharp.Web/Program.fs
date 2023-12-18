open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Giraffe
open Microsoft.AspNetCore.Http

open Lsharp

let indexHandler = 
    fun next (ctx: HttpContext) -> htmlView (Views.Index()) next ctx

let registrationHandler =
    fun next (ctx: HttpContext) -> htmlView (Views.Registration()) next ctx

let webApp = choose [
    route "/" >=> GET >=> indexHandler;
    route "/registration" >=> GET >=> registrationHandler;
]


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp) |> ignore

    app.Run()

    0 // Exit code

