module LSharp.Problems.Handlers.Solutions

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ActionResults
open LSharp.Helpers.Handlers
open System

open Giraffe
open Microsoft.AspNetCore.Http
open System.Threading.Tasks


let solveHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        readDto<CodeDTO> ctx
        |> ActionResult.fromResultTask BadRequest
        |> ActionResult.mapTask (fun code -> (getUserId ctx, code.taskId, code.code))
        |> ActionResult.bindTask (fun values -> values |||> solve)
        |> actionResultTaskToResponse next ctx
    


let deleteTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        ctx.TryGetQueryStringValue("id")
        |> ActionResult.fromOption "invalid id"
        |> Task.FromResult
        |> ActionResult.bindTask deleteTask
        |> actionResultTaskToResponse next ctx


let likeSolutionHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let userId = getUserId ctx
        ctx.TryGetQueryStringValue("id")
        |> ActionResult.fromOption "invalid id"
        |> Task.FromResult
        |> ActionResult.bindTask (fun id -> like userId id)
        |> actionResultTaskToResponse next ctx
    


let getSolutionHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let userId = getUserId ctx
        ctx.TryGetQueryStringValue("id")
        |> ActionResult.fromOption "invalid id"
        |> Task.FromResult
        |> ActionResult.bindTask getSolutionById
        |> ActionResult.mapTask (fun solution -> toSolutionView userId solution)
        |> actionResultTaskToResponse next ctx


let getPage (ctx: HttpContext) = 
    let tryParse (input: string) =
        let (isValid, result) = Int32.TryParse(input)
        match isValid with
        | false -> None
        | true -> Some result

    ctx.TryGetQueryStringValue("page") 
    |> Option.bind tryParse
    |> function
        | None -> 1
        | Some p when p <= 0 -> 1
        | Some p -> p
    

let getSolutionsByTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let userId = getUserId ctx
        let page = getPage ctx
        let! solutions = 
            ctx.TryGetQueryStringValue("id")
            |> getSolutionsByTask page
        return! Req.ok (solutions |> Array.map (fun s -> toSolutionView userId s)) next ctx
    }


let getUserSolutionsHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! solutions = getUserId ctx |> getSolutionsByUser
        return! ok solutions next ctx
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