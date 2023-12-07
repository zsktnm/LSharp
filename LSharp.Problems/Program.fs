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
open LSharp.Problems.Handlers.Common
open LSharp.Problems.Handlers.Solutions
open LSharp.Problems.Handlers.Tasks
open LSharp.Helpers.ActionResults

open FluentValidation
open System.Security.Claims
open LSharp.Rabbit
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Threading.Tasks

let [<Literal>] exitCode = 0


// TODO: centrilize / use settings
let factory = ConnectionFactory(HostName = "localhost")
let exchangeName = "lsharp"
let queueResults = "tasks_results"
let queueExp = "exp_gain"

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
    route "/task/image" >=> PUT >=> authorize >=> isAdmin >=> updateTaskImageHandler
    // TODO: use subroutes?
    route "/solutions/solve" >=> POST >=> authorize >=> solveHandler
    route "/solutions/find" >=> GET >=> authorize >=> getSolutionHandler
    route "/solutions/get" >=> GET >=> authorize >=> getSolutionsByTaskHandler
    route "/solutions/getByUser" >=> GET >=> authorize >=> getUserSolutionsHandler
    route "/solutions/like" >=> GET >=> authorize >=> likeSolutionHandler
    route "/solutions/publish" >=> PUT >=> authorize >=> publishSolutuonHandler
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

// TODO: what to do, if event handler has error?
let postLevelUp (result: TaskResult) = 
    let postMessage (solution: Solution) exp = 
        use connection = factory.CreateConnection()
        use channel = connection.CreateModel()

        if not solution.getExp then
            startQueueDeclare queueExp
            |> durable
            |> executeQueueDeclare channel
            |> ignore

            declareDirectExchange exchangeName channel
            bindQueue queueExp exchangeName queueExp channel

            toJsonBytes { UserId = solution.user; Exp = exp }
            |> startPublish
            |> withExchange exchangeName
            |> withRouting queueExp
            |> executePublish channel

            gotExp (solution._id.ToString()) 
            |> ignore       

    getSolution result.UserId result.TaskId
    |> ActionResult.bindTask (fun solution -> 
        getTaskById solution.task 
        |> ActionResult.mapTask (fun task -> (solution, task.exp)))
    |> fun t -> t.GetAwaiter().GetResult()
    |> function
        | Success values -> postMessage <|| values
        | err -> ignore () // TODO: handle errors

let subscribe () = 
    let connection = factory.CreateConnection()
    let channel = connection.CreateModel()

    let listener (args: BasicDeliverEventArgs) =
        printfn "receive message"
        let result = fromJsonBytes<TaskResult> (args.Body.ToArray())
        if result.IsValid then
            acceptValid result.UserId result.TaskId 
            |> fun task -> task.GetAwaiter().GetResult()
            |> function
                | Success _ -> postLevelUp result
                | err -> ignore () // TODO: handle errors
    
    startQueueDeclare queueResults
    |> durable
    |> executeQueueDeclare channel
    |> fun ok -> printfn "Start listening. msg count: %d" ok.MessageCount 
    
    declareDirectExchange exchangeName channel
    bindQueue queueResults exchangeName "" channel

    channel
    |> createEventConsumer
    ||> listen listener
    ||> startConsume queueResults
    


[<EntryPoint>]
let main args =
    subscribe() |> ignore
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

