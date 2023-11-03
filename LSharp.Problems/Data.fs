namespace LSharp.Problems

open LSharp.Mongodb
open LSharp.Mongodb.BuildHelpers
open LSharp.Mongodb.Mongo
open MongoDB.Bson
open FluentValidation

module Data = 
    let [<Literal>] connectionString = "mongodb://localhost:27017"
    let [<Literal>] databaseName = "LsharpData"
    let [<Literal>] categoriesName = "categories" 
    let [<Literal>] tasksName = "tasks" 

    [<CLIMutable>]
    type Category = {
        _id: ObjectId;
        name: string;
        image: string;
        level: int;
    }

    [<CLIMutable>]
    type LsharpTask = { //LSharp task
        _id: ObjectId;
        name: string;
        category: string;
        image: string;
        require: int;
        exp: int;
        description: string;
        file: string;
    }

    let categories = 
        connectionString 
        |> client 
        |> database databaseName
        |> collection<Category> categoriesName

    let tasks = 
        connectionString 
         |> client 
         |> database databaseName
         |> collection<LsharpTask> tasksName

    let tryParseOid id = 
        let result = ObjectId.TryParse(id)
        match result with
        | (false, _) -> None
        | (true, objId) -> Some objId 
    

    // CATEGORIES

    let getCategoryById (id: string) = task {
        match tryParseOid id with
        | None -> return None
        | Some id -> 
            return! categories
                |> find {| _id = oid id |}
                |> oneAsync
    }

    let getAllCategories () = task {
        return! categories 
        |> getAll
        |> toListAsync
    }

    let addCategory category = task {
        return! categories 
        |> insertOneAsync category
    }

    let updateCategory id update = task {
        match tryParseOid id with
        | None -> return Error "Invalid id"
        | Some id -> 
            return! categories
            |> updateOneAsync 
                {| ``_id`` = oid id |}
                {| ``$set`` = update |}
    }

    let updateCategoryImage filename id = task {
        match tryParseOid id with
        | None -> return Error "Invalid id"
        | Some id -> 
            return! categories
            |> updateOneAsync 
                {| ``_id`` = oid id |}
                (setFields {| image = filename |})
    } 

    // TASKS

    let getAllTasks () = task {
        return! tasks 
            |> getAll
            |> toListAsync
    }


    let getTasksByCategory categoryId = task {
        return! tasks 
            |> find {| category = categoryId |}
            |> toListAsync
    }


    let getTaskById (id: string) = task {
        match tryParseOid id with
        | None -> return None
        | Some id -> 
            return! tasks
                |> find {| _id = oid id |}
                |> oneAsync
    }


    let insertTask lsharpTask = task {
        let! category =  
            tasks 
            |> find {| _id = oid lsharpTask.category |}
            |> oneAsync

        match category with
        | None -> return Error "Invalid category"
        | Some _ -> 
            try
                do! tasks |> insertOneAsync lsharpTask
                return Ok "Success"
            with
                | _ -> return Error "Error while adding the task"
    }

    let updateTask lsharpTask id = task {
        match tryParseOid id with
        | None -> return Error "Invalid id"
        | Some id -> 
            return! tasks 
            |> updateOneAsync 
                {| _id = oid id |}
                (setFields lsharpTask)
    }

    let updateTaskFile filename id = task {
        match tryParseOid id with
        | None -> return Error "Invalid id"
        | Some id -> 
            return! tasks 
            |> updateOneAsync 
                {| _id = oid id |}
                (setFields {| file = filename |})
    }

    let updateTaskImage filename id = task {
        match tryParseOid id with
        | None -> return Error "Invalid id"
        | Some id -> 
            return! tasks 
            |> updateOneAsync 
                {| _id = oid id |}
                (setFields {| image = filename |})
    }

