namespace LSharp.Problems

open LSharp.Mongodb
open LSharp.Mongodb.BuildHelpers
open LSharp.Mongodb.Mongo
open MongoDB.Bson
open FluentValidation
open System
open System.Globalization

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


    let getSolution userId taskId = 
        solutions 
        |> find {| user = userId.ToString(); task = taskId.ToString() |}
        |> oneAsync


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
            else
                solutions 
                |> updateOneAsync 
                    {| _id = oid (solution._id.ToString()); solutions = last |}  
                    {| 
                        ``$set`` = 
                        {|
                            ``solutions.$.code`` = code
                        |}
                    |}

        task {
            let! task = getTaskById taskId
            let! solution = getSolution userId taskId
            match (task, solution) with
            | (None, _) -> return Error "Invalid task"
            | (_, None) -> 
                do! insertSolution ()
                return Ok "Success"
            | (_, Some solution) -> 
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
             else
                solutions
                |> updateByIdAsync
                    (solution._id.ToString())
                    {| ``$push`` = {| likes = userId |} |}

        task {
            let! solution = 
                solutions 
                |> findByIdAsync solutionId

            match solution with
            | None -> return Error "Solution not found"
            | Some s -> return! likeSolution userId s
        }


    let getSolutionById id = 
        solutions 
        |> findByIdAsync id

    let getSolutionsByTask taskId = 
        solutions
        |> find {| task = taskId |}
        |> toListAsync

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