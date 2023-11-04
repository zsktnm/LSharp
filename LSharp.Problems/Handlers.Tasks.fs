module LSharp.Problems.Handlers.Tasks

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ImageSaving

open Giraffe
open Microsoft.AspNetCore.Http
open System.IO


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