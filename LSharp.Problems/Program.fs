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

open LSharp.Helpers.ImageSaving
open LSharp.Helpers

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


let badRequest = RequestErrors.BAD_REQUEST
let notFound = RequestErrors.NOT_FOUND
let ok = Successful.OK
let noContent = Successful.NO_CONTENT


let responseFromResult next ctx result = task {
    match! result with
    | Ok msg -> 
        return! Successful.OK msg next ctx
    | Error msg -> 
        return! RequestErrors.BAD_REQUEST msg next ctx
}


let getUserId (ctx: HttpContext) = 
    ctx.User.FindFirst(ClaimTypes.NameIdentifier).Value


let findCategoryResult id next ctx = task {
    match! getCategoryById id with
    | None -> 
        return! notFound "not found" next ctx
    | Some category -> 
        return! json category next ctx
}


let findCategoryHandler = 
   fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let id = ctx.TryGetQueryStringValue("id")
        match id with
        | None -> 
            return! notFound "not found" next ctx
        | Some id -> 
            return! findCategoryResult id next ctx
    } 


let getCategoriesHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let! categories = getAllCategories()
        return! json categories next ctx
    } 


let addCategoryHandler =  
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match! readDto<CategoryDTO> ctx with
        | Error errors -> 
            return! badRequest errors next ctx
        | Ok dto -> 
            do! addCategory (dto |> toCategory)
            return! Successful.NO_CONTENT next ctx
    }


let updateCategoryResult id dto next ctx = task {
    match! (updateCategory id dto) with
    | Ok _ -> return! Successful.NO_CONTENT next ctx
    | Error err -> return! badRequest err next ctx
}


let updateCategoryHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let id = ctx.TryGetQueryStringValue("id")
        let! dto = readDto<CategoryDTO> ctx
        return! 
            match (id, dto) with
            | (None, _) -> 
                badRequest "id is undefined" next ctx
            | (_, Error validationErrors) -> 
                badRequest validationErrors next ctx
            | (Some id, Ok dto) -> 
                updateCategoryResult id dto next ctx
    }


let updateCategoryImageAsync maybeId filenameTask = task {
    let! filename = filenameTask
    match (maybeId, filename) with
    | (Some id, Ok filename) -> return! updateCategoryImage filename id
    | (_, Error msg) -> return Error msg
    | (None, _) -> return Error "Invalid Id"
}


let updateCategoryImageHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")

    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let maybeId = ctx.TryGetQueryStringValue("id")
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! RequestErrors.BAD_REQUEST "No content-length header" next ctx
        | header when header.Value > maxImageSize -> 
            return! RequestErrors.BAD_REQUEST "Invalid size" next ctx
        | header -> 
            return! copyPngFile ctx.Request.Body header.Value (getFilename())
            |> updateCategoryImageAsync maybeId
            |> responseFromResult next ctx
    }


let getTasksHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let maybeId = ctx.TryGetQueryStringValue("id")
        let! tasks = 
            match maybeId with
            | None -> getAllTasks ()
            | Some id -> getTasksByCategory id
        return! ok tasks next ctx
    }


let getTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        match ctx.TryGetQueryStringValue("id") with
        | None -> return! badRequest "Id is empty" next ctx
        | Some id -> 
            match! getTaskById id with
            | None -> return! notFound "Not found" next ctx
            | Some t -> return! ok t next ctx
    } 


let addTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        match! (readDto<TaskDTO> ctx) with
        | Error validationErrors -> 
            return! badRequest validationErrors next ctx
        | Ok dto -> 
            match! (dto |> toLsharpTask |> insertTask) with
            | Error err -> return! badRequest err next ctx
            | Ok _ -> return! noContent next ctx
    }


let updateTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        let! dto = readDto<TaskDTO> ctx

        match (maybeId, dto) with 
        | (Some id, Ok dto) -> 
            return! updateTask dto id 
            |> responseFromResult next ctx
        | (None, _) -> 
            return! badRequest "invalid id" next ctx
        | (_, Error validationErrors) ->
            return! badRequest validationErrors next ctx
    }


let updateTaskFileAsync maybeId filenameTask = task {
    let! filename = filenameTask
    match (maybeId, filename) with
    | (Some id, Ok filename) -> return! updateTaskFile filename id
    | (_, Error msg) -> return Error msg
    | (None, _) -> return Error "Invalid Id"
}


let updateTaskFileHandler = 
    let getFilename() = 
        Path.Combine("tasks", Path.GetRandomFileName() + ".zip")

    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! badRequest "No content-length header" next ctx
        | header when header.Value > maxZipSize -> 
            return! badRequest "Invalid size" next ctx
        | header -> 
            return! copyFile ctx.Request.Body header.Value (getFilename())
            |> updateTaskFileAsync maybeId
            |> responseFromResult next ctx
    }


let updateTaskImageAsync maybeId filenameTask = task {
    let! filename = filenameTask
    match (maybeId, filename) with
    | (Some id, Ok filename) -> return! updateTaskImage filename id
    | (_, Error msg) -> return Error msg
    | (None, _) -> return Error "Invalid Id"
}


let updateTaskImageHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")

    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        match ctx.Request.ContentLength with
        | header when not header.HasValue -> 
            return! badRequest "No content-length header" next ctx
        | header when header.Value > maxZipSize -> 
            return! badRequest "Invalid size" next ctx
        | header -> 
            return! copyPngFile ctx.Request.Body header.Value (getFilename())
            |> updateTaskImageAsync maybeId
            |> responseFromResult next ctx
    }


let solveHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let code = readDto<CodeDTO> ctx
        match! code with
        | Error validationErrors -> 
            return! badRequest validationErrors next ctx
        | Ok code -> 
            return! (userId, code.taskId, code.code)
            |||> solve 
            |> responseFromResult next ctx
    }


let deleteTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        match maybeId with
        | None -> return! badRequest "Invalid id" next ctx
        | Some id -> return! deleteTask id |> responseFromResult next ctx
    }


let likeSolutionHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        let userId = getUserId ctx
        match maybeId with
        | None -> return! badRequest "Invalid id" next ctx
        | Some solutionId -> 
            return! like userId solutionId 
            |> responseFromResult next ctx
    }


let getSolutionHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let maybeId = ctx.TryGetQueryStringValue("id")
        match maybeId with
        | None -> 
            return! badRequest "Invalid id" next ctx
        | Some solutionId -> 
            match! getSolutionById solutionId with
            | None -> return! notFound "Not found" next ctx
            | Some s -> return! ok (s |> toSolutionView userId) next ctx
    }


let getSolutionsByTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let maybeId = ctx.TryGetQueryStringValue("id")
        match maybeId with
        | None -> 
            return! badRequest "Invalid id" next ctx
        | Some taskId -> 
            let! solutions = getSolutionsByTask taskId 
            return! ok 
                    (solutions |> Seq.map (fun s -> toSolutionView userId s))
                    next ctx
    }


let postCommentHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let comment = readDto<PostCommentDTO> ctx
        match! comment with
        | Error msgs -> 
            return! badRequest msgs next ctx
        | Ok comment -> 
            return! commentSolution userId comment.solution comment.text
            |> responseFromResult next ctx
    }


let deleteCommentHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let commentData = readDto<RemoveDTO> ctx
        match! commentData with
        | Error msgs -> 
            return! badRequest msgs next ctx
        | Ok data -> 
            return! deleteComment userId data.solution data.comment
            |> responseFromResult next ctx
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

