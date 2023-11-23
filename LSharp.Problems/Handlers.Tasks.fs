module LSharp.Problems.Handlers.Tasks

open System

open LSharp.Problems.Data
open LSharp.Problems.DataTransfer
open LSharp.Problems.Handlers.Common
open LSharp.Helpers.ImageSaving

open Giraffe
open Microsoft.AspNetCore.Http
open System.IO
open LSharp.Helpers.Handlers
open LSharp.Helpers.ActionResults
open System.Threading.Tasks


let getQueryValue str (ctx: HttpContext) = 
    ctx.TryGetQueryStringValue(str)

let getTasksHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> task { 
        let! tasks = 
            match getQueryValue "id" ctx with
            | None -> getAllTasks ()
            | Some id -> getTasksByCategory id
        return! ok tasks next ctx
    }


let getTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "invalid id"
        |> Task.FromResult
        |> ActionResult.bindTask getTaskById
        |> actionResultTaskToResponse next ctx



let addTaskHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        readDto<TaskDTO> ctx 
        |> ActionResult.fromResultTask BadRequest
        |> ActionResult.mapTask (fun dto -> toLsharpTask dto)
        |> ActionResult.bindTask insertTask
        |> actionResultTaskToResponse next ctx



let updateTaskHandler = 
    let taskResultMap func result = task {
        match! result with
        | Ok r -> return Ok (func r)
        | Error err -> return Error err
    } 

    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "invalid id"
        |> Task.FromResult
        |> ActionResult.bindTask (fun id -> 
            ctx
            |> readDto<TaskDTO>
            |> taskResultMap (fun dto -> (dto, id))
            |> ActionResult.fromResultTask BadRequest
            )
        |> ActionResult.bindTask (fun pair -> updateTask (fst pair) (snd pair))
        |> actionResultTaskToResponse next ctx




let updateTaskImageHandler = 
    let getFilename() = 
        Path.Combine("images", Path.GetRandomFileName() + ".png")

    let checkContentLength id (header: Nullable<int64>) = 
        match header with
        | header when not header.HasValue -> 
            BadRequest "No content-length header"
        | header when header.Value > maxImageSize -> 
            BadRequest "Invalid size" 
        | header -> 
            Success (id, header.Value)

    let copyFile body len filename id = 
        copyPngFile body len filename
        |> ActionResult.bindTask (fun filename -> updateTaskImage filename id)

    fun (next: HttpFunc) (ctx: HttpContext) -> 
        getQueryValue "id" ctx
        |> ActionResult.fromOption "invalid id"
        |> ActionResult.bind (fun id -> checkContentLength id (ctx.Request.ContentLength))
        |> Task.FromResult
        |> ActionResult.bindTask (fun (id, length) -> copyFile ctx.Request.Body length (getFilename ()) id)
        |> actionResultTaskToResponse next ctx
        

    