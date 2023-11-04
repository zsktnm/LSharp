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
    let [<Literal>] solutionsName = "solutions" 

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


    [<CLIMutable>]
    type SolutionItem = {
        datetime: string;
        code: string;
        isValid: bool;
    }

    [<CLIMutable>]
    type SolutionComment = {
        user: string;
        datetime: string;
        text: string;
    }

    [<CLIMutable>]
    type Solution = {
        _id: ObjectId;
        user: string;
        getExp: bool;
        published: bool;
        solutions: SolutionItem array;
        comments: SolutionComment array;
        likes: string array;
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

    let solutions = 
        connectionString 
         |> client 
         |> database databaseName
         |> collection<LsharpTask> solutionsName


    

    // CATEGORIES

    let getCategoryById (id: string) = task {
        return! categories
        |> findByIdAsync id
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
        return! categories |>
        updateByIdAsync id {| ``$set`` = update |}
    }

    let updateCategoryImage filename id = task {
        return! categories |>
        updateByIdAsync id (setFields {| image = filename |})
    } 

    let deleteCategory id = task {
        return! categories |>
        deleteByIdAsync id 
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
        return! tasks 
        |> findByIdAsync id
    }


    let insertTask (lsharpTask: LsharpTask) = task {
        let! category = 
            categories 
            |> findByIdAsync lsharpTask.category

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
        return! tasks |> 
        updateByIdAsync id (setFields lsharpTask)
    }


    let updateTaskFile filename id = task {
        return! tasks |>
        updateByIdAsync id (setFields {| file = filename |})
    }


    let updateTaskImage filename id = task {
        return! tasks |>
        updateByIdAsync id (setFields {| image = filename |})
    }


    let deleteTask id = task {
        return! tasks |>
        deleteByIdAsync id 
    }


    // SOLUTIONS

