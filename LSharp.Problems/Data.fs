module LSharp.Problems.Data

open LSharp.Mongodb
open LSharp.Mongodb.BuildHelpers
open LSharp.Mongodb.Mongo
open MongoDB.Bson
open FluentValidation
open System
open System.Globalization
open LSharp.Helpers.ActionResults


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
    code: string;
    test: string;
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
    task: string;
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
        |> collection<Solution> solutionsName

    
let nowIso () =
    DateTime
        .Now
        .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

let updateToAction result =
    match result with
    | Sucessful x ->  Success x
    | ClientError err ->  BadRequest err
    | ServerError err ->  InternalError err

let updateToActionTask result = task {
    match! result with
    | Sucessful x -> return Success x
    | ClientError err -> return BadRequest err
    | ServerError err -> return InternalError err
}

// CATEGORIES

let getCategoryById (id: string) = 
    categories
    |> findByIdAsync id
    |> ActionResult.fromOptionTask "Category is not found"


let getAllCategories () = 
    categories 
    |> getAll
    |> toListAsync


let addCategory category = 
    categories 
    |> insertOneAsync category


let updateCategory id update =
    categories 
    |> updateByIdAsync id {| ``$set`` = update |}
    |> updateToActionTask


let updateCategoryImage filename id = 
    categories 
    |> updateByIdAsync id (setFields {| image = filename |})
    |> updateToActionTask


let deleteCategory id = 
    categories
    |> deleteByIdAsync id
    |> updateToActionTask


// TASKS

let getAllTasks () = 
    tasks 
    |> getAll
    |> toListAsync


let getTasksByCategory categoryId = 
    tasks 
    |> find {| category = categoryId |}
    |> toListAsync



let getTaskById (id: string) = 
    tasks 
    |> findByIdAsync id
    |> ActionResult.fromOptionTask "Task not found"



let insertTask (lsharpTask: LsharpTask) = 
    let insert t tasks = task {
        do! insertOneAsync t tasks
        return Success "Success"
    }
    categories
    |> findByIdAsync lsharpTask.category
    |> ActionResult.fromOptionTask "invalid category"
    |> ActionResult.bindTask (fun _ -> insert lsharpTask tasks)



let updateTask lsharpTask id = 
    tasks 
    |> updateByIdAsync id (setFields lsharpTask)
    |> updateToActionTask



let updateTaskFile filename id = 
    tasks 
    |> updateByIdAsync id (setFields {| file = filename |})
    |> updateToActionTask



let updateTaskImage filename id =
    tasks 
    |> updateByIdAsync id (setFields {| image = filename |})
    |> updateToActionTask



let deleteTask id = 
    tasks 
    |> deleteByIdAsync id 
    |> updateToActionTask



// SOLUTIONS

let getSolution userId taskId = 
    solutions 
    |> find {| user = userId.ToString(); task = taskId.ToString() |}
    |> oneAsync
    |> ActionResult.fromOptionTask "Not found"


let solve userId taskId code = 
    let createSolution () = {
        _id = ObjectId.GenerateNewId();
        task = taskId;
        user = userId;
        getExp = false;
        published = false;
        comments = Array.empty;
        likes = Array.empty;
        solutions = [| 
            { 
                datetime = nowIso ();
                code = code;
                isValid = false;
            } 
        |]
    } 

    let insertSolution () = 
        solutions
        |> insertOneAsync (createSolution ())

    let updateCode (solution: Solution) code = 
    // TODO: refactor
        let last = 
            solution.solutions 
            |> Array.last

        if last.isValid then
            solutions 
            |> updateOneAsync 
                {| _id = oid (solution._id.ToString()) |}  
                {| 
                    ``$push`` = 
                    {|
                        solutions = 
                        {|
                            datetime = nowIso ();
                            code = code;
                            isValid = false;
                        |}
                    |}
                |}
            |> ActionResult.fromResultTask InternalError
        else
            solutions 
            |> updateOneAsync 
                {| 
                    _id = oid (solution._id.ToString()); 
                    solutions = {| 
                        ``$elemMatch`` = {|
                            datetime = last.datetime;
                            code = last.code;
                        |}
                    |} 
                |}  
                {| 
                    ``$set`` = 
                    {|
                        ``solutions.$.code`` = code
                    |}
                |}
            |> ActionResult.fromResultTask InternalError

    task {
    // TODO: refactor
        let! task = getTaskById taskId
        let! solution = getSolution userId taskId
        match (task, solution) with
        | (NotFound msg, _) -> return NotFound msg
        | (_, NotFound _) -> 
            do! insertSolution ()
            return Success "Success"
        | (_, Success solution) -> 
            return! updateCode solution code
    }

let like userId solutionId = 

    let likeSolution userId (solution: Solution) = 
        let hasLike = 
            solution.likes
            |> Array.exists ((=) userId)
            
        if hasLike then
            solutions 
            |> updateByIdAsync 
                (solution._id.ToString())
                {| ``$pull`` = {| likes = userId |} |}
            |> updateToActionTask
        else
            solutions
            |> updateByIdAsync
                (solution._id.ToString())
                {| ``$push`` = {| likes = userId |} |}
            |> updateToActionTask

    task {
        let! solution = 
            solutions 
            |> findByIdAsync solutionId

        match solution with
        | None -> return NotFound "Solution not found"
        | Some s -> return! likeSolution userId s
    }


let getSolutionById id = 
    solutions 
    |> findByIdAsync id

let getSolutionsByTask taskId = 
    solutions
    |> find {| task = taskId; |}
    |> toListAsync


let getSolutionsByUser userId = 
    solutions
    |> find {| user = userId; |}
    |> toListAsync


    // TODO: refactor
let commentSolution userId solutionId text = 
    let postComment userId (solution: Solution) text = 
        solutions
        |> updateOneAsync
            {| _id = oid (solution._id.ToString()) |}
            {|
                ``$push`` = 
                {|
                    comments = {
                        user = userId;
                        datetime = nowIso ();
                        text = text
                    }
                |}
            |}
    
    task {
        let! solution = 
            solutions
            |> findByIdAsync solutionId

        match solution with
        | None -> return Error "Solution is not found"
        | Some s -> return! postComment userId s text
    }

let deleteComment userId solution (comment: SolutionComment) =
    // TODO: refactor

    let remove userId solution comment = 
        solutions
        |> updateOneAsync
            {| _id = oid (solution._id.ToString()) |}
            {|
                ``$pull`` = 
                {|
                    comments = {
                        user = userId;
                        datetime = comment.datetime;
                        text = comment.text
                    }
                |}
            |}

    task {
        let! toDelete = 
            solutions
            |> findByIdAsync solution

        match toDelete with
        | None -> return Error "Solution is not found"
        | Some s -> return! remove userId s comment
    }