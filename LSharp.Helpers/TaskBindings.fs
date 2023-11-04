namespace LSharp.Helpers

module TaskBindings =

    let resTaskToResTask func taskValue = task {
        match! taskValue with 
        | Error e -> return Error e
        | Ok v -> return! func v
    }


    let optTaskToOptTask func taskValue = task {
        match! taskValue with 
        | None -> return None
        | Some v -> return! func v
    } 


    let optTaskToResTask func taskValue message = task {
        match! taskValue with 
        | None -> return Error message
        | Some v -> return! func v
    } 


    let resTaskToOptTask func taskValue = task {
        match! taskValue with 
        | Error _ -> return None
        | Ok v -> return! func v
    }


    let taskBind func value = task {
        let! v = value
        return func v
    }