module LSharp.CodeRunner.Data

open MongoDB.Bson
open MongoDB.Driver
open LSharp.Mongodb.Mongo

let [<Literal>] connectionString = "mongodb://localhost:27017"
let [<Literal>] databaseName = "LsharpData"
let [<Literal>] tasksName = "tasks" 

[<CLIMutable>]
type LsharpTask = { //LSharp task
    _id: ObjectId;
    name: string;
    category: string;
    image: string;
    require: int;
    exp: int;
    description: string;
    code: string;
    test: string;
}

let tasks = 
    connectionString 
    |> client 
    |> database databaseName
    |> collection<LsharpTask> tasksName

let loadTest id = task {
    let! found = tasks |> findByIdAsync id
    match found with
    | None -> return Error "Invalid Id"
    | Some t -> return Ok t.test
}

let loadTask id = task {
    let! found = tasks |> findByIdAsync id
    match found with
    | None -> return Error "Invalid Id"
    | Some t -> return Ok t
}