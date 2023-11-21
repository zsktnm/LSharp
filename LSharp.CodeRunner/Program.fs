open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open System.Security.Cryptography
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.DependencyInjection
open Giraffe
open System.IO
open Microsoft.AspNetCore.Http

open Lsharp.CodeRunner.DataTransfer
open LSharp.CodeRunner.Data
open LSharp.CodeRunner.Containers


let [<Literal>] exitCode = 0
let rsaPublic = RSA.Create()


let badRequest = RequestErrors.BAD_REQUEST
let notFound = RequestErrors.NOT_FOUND
let ok = Successful.OK
let noContent = Successful.NO_CONTENT

let authorize = 
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let responseFromResult next ctx result = task {
    match! result with
    | Ok msg -> 
        return! Successful.OK msg next ctx
    | Error msg -> 
        return! RequestErrors.BAD_REQUEST msg next ctx
}

    

let startTest taskId code next ctx = task {
    let! test = loadTest taskId
    match test with
    | Error err -> return! notFound err next ctx
    | Ok test ->
        return! 
            executeTests code test 
            |> responseFromResult next ctx
}

let checkHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let taskId = ctx.TryGetQueryStringValue("id")
        let! dto = ctx.BindModelAsync<CodeDTO> () 
        match taskId with
        | None -> return! notFound "invalid id" next ctx
        | Some id -> return! startTest id dto.code next ctx
    }

let webApp = choose [
    route "/check" >=> POST >=> authorize >=> checkHandler
]

let getJwtOptions (options: JwtBearerOptions) =
    // TODO: explore options
    options.TokenValidationParameters <- TokenValidationParameters(
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = RsaSecurityKey(rsaPublic),
        ValidateAudience = false,
        ValidateIssuer = true,
        ValidIssuer = "LSharp",
        ValidateLifetime = true
    )

let getAuthOptions (options: AuthenticationOptions) = 
    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddGiraffe() |> ignore
    builder.Services
        .AddAuthentication(Action<AuthenticationOptions> getAuthOptions)
        .AddJwtBearer(Action<JwtBearerOptions> getJwtOptions) 
        |> ignore

    let app = builder.Build()

    rsaPublic.FromXmlString(File.ReadAllText("public.xml"))

    app
        .UseAuthentication()
        .UseGiraffe(webApp) 

    app.Run()

    exitCode

