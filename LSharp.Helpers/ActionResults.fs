module LSharp.Helpers.ActionResults

type ActionResult<'a, 'b> = 
| Success of 'a
| InternalError of 'b
| NotFound of 'b
| BadRequest of 'b


module ActionResult = 
    let matchErrors =
        function
        | NotFound msg -> NotFound msg
        | BadRequest msg -> BadRequest msg
        | InternalError msg -> InternalError msg
        | Success _ -> failwith "matching Success as error"
        

    let fromOption errorMessage option = 
        match option with
        | None -> NotFound errorMessage
        | Some x -> Success x

    let fromOptionTask errorMessage option = task {
        match! option with
        | None -> return NotFound errorMessage
        | Some x -> return Success x
    }

    let fromResult errFunc result = 
        match result with
        | Ok x -> Success x
        | Error err -> errFunc err

    let fromResultTask errFunc result = task {
        match! result with
        | Ok x -> return Success x
        | Error err -> return errFunc err
    }

    let bind func result = 
        match result with
        | Success x -> func x
        | NotFound msg -> NotFound msg
        | BadRequest msg -> BadRequest msg
        | InternalError msg -> InternalError msg

    let bindTask func result = task {
        match! result with
        | Success x -> return! func x
        | NotFound msg -> return NotFound msg
        | BadRequest msg -> return BadRequest msg
        | InternalError msg -> return InternalError msg
    }

    let map func result = 
        match result with
        | Success x -> Success (func x)
        | NotFound msg -> NotFound msg
        | BadRequest msg -> BadRequest msg
        | InternalError msg -> InternalError msg

    let mapTask func result = task {
        match! result with
        | Success x -> return Success (func x)
        | NotFound msg -> return NotFound msg
        | BadRequest msg -> return BadRequest msg
        | InternalError msg -> return InternalError msg
    }