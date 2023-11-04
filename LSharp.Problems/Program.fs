module LSharp.Problems.Main

open System
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

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer

open LSharp.Problems.Handlers.Categories
open LSharp.Problems.Handlers.Solutions
open LSharp.Problems.Handlers.Tasks

open FluentValidation
open System.Security.Claims


let [<Literal>] exitCode = 0
let [<Literal>] maxImageSize = 1000000
let [<Literal>] maxZipSize = 5000000


let rsaPublic = RSA.Create()


let authorize = 
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)


let isAdmin = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match ctx
            .User
            .FindFirst(ClaimTypes.Role)
            .Value
            .Contains("Admin") with
        | true -> return! next ctx
        | _ -> return! RequestErrors.FORBIDDEN "Forbidden" next ctx
    }




let webApp = choose [
    route "/categories" >=> GET >=> authorize >=> getCategoriesHandler
    route "/category" >=> choose [
        GET >=> authorize >=> findCategoryHandler
        POST >=> authorize >=> isAdmin >=> addCategoryHandler
        PUT >=> authorize >=> isAdmin >=> updateCategoryHandler
    ]
    route "/category/image" >=> PUT >=> authorize >=> isAdmin >=> updateCategoryImageHandler
    route "/tasks" >=> GET >=> authorize >=> getTasksHandler
    route "/task" >=> choose [
        POST >=> authorize >=> isAdmin >=> addTaskHandler
        GET >=> authorize >=> getTaskHandler
        PUT >=> authorize >=> isAdmin >=> updateTaskHandler
        DELETE >=> authorize >=> isAdmin >=> deleteTaskHandler
    ]
    route "/task/file" >=> PUT >=> authorize >=> isAdmin >=> updateTaskFileHandler
    route "/task/image" >=> PUT >=> authorize >=> isAdmin >=> updateTaskImageHandler
    route "/solutions/solve" >=> POST >=> authorize >=> solveHandler
    route "/solutions/find" >=> GET >=> authorize >=> getSolutionHandler
    route "/solutions/get" >=> GET >=> authorize >=> getSolutionsByTaskHandler
    route "/solutions/getByUser" >=> GET >=> authorize >=> getUserSolutionsHandler
    route "/solutions/like" >=> GET >=> authorize >=> likeSolutionHandler
    route "/solutions/comment" >=> choose [
        POST >=> authorize >=> postCommentHandler
        DELETE >=> authorize >=> deleteCommentHandler
    ]
    RequestErrors.NOT_FOUND "Not found"
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

    builder.Services
        .AddScoped<AbstractValidator<CategoryDTO>, CategoryValidator>()
        .AddScoped<AbstractValidator<TaskDTO>, TaskValidator>()
        .AddScoped<AbstractValidator<CodeDTO>, CodeValidator>()
        .AddScoped<AbstractValidator<PostCommentDTO>, PostCommentValidator>()
        .AddScoped<AbstractValidator<RemoveDTO>, RemoveCommentValidator>()
        |> ignore
        
    let app = builder.Build()
    rsaPublic.FromXmlString(File.ReadAllText("public.xml"))

    app
        .UseAuthentication()
        .UseGiraffe(webApp) 
    |> ignore

    app.Run()

    exitCode

