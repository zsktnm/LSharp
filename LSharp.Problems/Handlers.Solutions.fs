module LSharp.Problems.Handlers.Solutions

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ActionResults
open LSharp.Helpers.Handlers

open Giraffe
open Microsoft.AspNetCore.Http


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
            |> actionResultTaskToResponse next ctx
    }


let deleteTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        match maybeId with
        | None -> return! badRequest "Invalid id" next ctx
        | Some id -> return! deleteTask id |> actionResultTaskToResponse next ctx
    }


let likeSolutionHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let maybeId = ctx.TryGetQueryStringValue("id")
        let userId = getUserId ctx
        match maybeId with
        | None -> return! badRequest "Invalid id" next ctx
        | Some solutionId -> 
            return! like userId solutionId 
            |> actionResultTaskToResponse next ctx
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
            | Some s when s.published || s.user = userId ->
                return! ok (s |> toSolutionView userId) next ctx
            | _ -> return! notFound "Not found" next ctx
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
                    (solutions 
                    |> Seq.map (fun s -> toSolutionView userId s)
                    |> Seq.where (fun s -> s.published)
                    )
                    next ctx
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