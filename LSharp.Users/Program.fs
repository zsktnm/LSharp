open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication
open Microsoft.IdentityModel.Tokens
open System.Security.Cryptography
open System.IO
open System.Security.Claims
open Microsoft.AspNetCore.Http

open LSharp.Users.Data
open LSharp.Helpers.ImageSaving
open LSharp.Helpers.Handlers
open LSharp.Helpers.ActionResults
open System.Threading.Tasks


let [<Literal>] exitCode = 0
let [<Literal>] maxFileSize = 524288L // 512 kb
    

let rsaPublic = RSA.Create()


let helloHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        ctx 
        |> getUserId 
        |> findUserByIdAsync
        |> actionResultTaskToResponse next ctx
        
    

let getLevelHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        ctx 
        |> getUserId 
        |> findUserByIdAsync
        |> ActionResult.mapTask (fun u -> u.level)
        |> actionResultTaskToResponse next ctx


let replaceUserNameHandler = 
    let getName (ctx: HttpContext) =
        ctx.TryGetQueryStringValue "name"

    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match getName ctx with
        | None -> 
            return! Req.badRequest "Invalid name" next ctx
        | Some name -> 
            return! ctx 
            |> getUserId 
            |> updateUserNameAsync name
            |> actionResultTaskToResponse next ctx
    }

let changeAvatarAsync id filename = task {
    match! filename with
    | Success filename -> return! updateUserAvatarAsync filename id
    | err -> return err
}

let setPhotoHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")
    
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! RequestErrors.BAD_REQUEST "No content-length header" next ctx
        | header when header.Value > maxFileSize -> 
            return! RequestErrors.BAD_REQUEST "Invalid size" next ctx
        | header -> 
            return! copyPngFile ctx.Request.Body header.Value (getFilename())
            |> changeAvatarAsync (getUserId ctx) 
            |> actionResultTaskToResponse next ctx
    }


let webApp = choose [
    route "/getname" >=> GET >=> authorize >=> helloHandler
    route "/setname" >=> GET >=> authorize >=> replaceUserNameHandler
    route "/getlevel" >=> GET >=> authorize >=> getLevelHandler
    route "/setphoto" >=> POST >=> authorize >=> setPhotoHandler
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
    |> ignore


    app.Run()

    exitCode

