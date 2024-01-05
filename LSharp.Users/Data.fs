namespace LSharp.Users

open LSharp.Mongodb.Mongo
open LSharp.Mongodb.BuildHelpers
open LSharp.Helpers.ActionResults
open LSharp.Users.LevelUp

module Data = 
    let [<Literal>] connectionString = "mongodb://localhost:27017"
    let [<Literal>] databaseName = "LsharpData"
    let [<Literal>] collectionName = "users"


    [<CLIMutable>]
    type User = {
        _id: string;
        name: string;
        avatar: string;
        exp: int;
        next: int;
        level: int
    }

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


    let usersCollection = 
            connectionString 
            |> client 
            |> database databaseName
            |> collection<User> collectionName


    let findUserByIdAsync id =
        usersCollection 
        |> find {| _id = id |}
        |> oneAsync
        |> ActionResult.fromOptionTask "Id is not found"

    let createAnonimous id = task {
        match! findUserByIdAsync id with
        | Success user -> 
            return ActionResult.BadRequest ["Пользователь уже существует"]
        | NotFound _ ->
            let user = { 
                    _id = id; 
                    name = "Аноним"; 
                    exp = 0; 
                    next = LevelUp.levels.Head;
                    avatar = "";
                    level = 1;
            }
            do! usersCollection
                |> insertOneAsync user
            return ActionResult.Success user
        | _ -> 
            return ActionResult.InternalError [ "Не удалось обновить запись. Попробуйте позже." ]
        }
    

    let updateUserNameAsync newName id = 
        usersCollection 
        |> updateOneAsync  
            {| _id = id |}
            (setFields {| name = newName |})
        |> ActionResult.fromResultTask InternalError


    let updateUserAvatarAsync newPhoto id =
        usersCollection
        |> updateOneAsync 
            {| _id = id |}
            (setFields {| avatar = newPhoto |})
        |> ActionResult.fromResultTask InternalError


    let updateUser id user = 
        if user._id <> id then
            BadRequest "invalid Id"
        else
            usersCollection 
            |> replaceOne {| _id = id |} user
            |> ActionResult.fromResult InternalError

    
    let updateUserAsync id user = task {
        if user._id <> id then
            return BadRequest "invalid Id"
        else
            return! usersCollection 
            |> replaceOneAsync {| _id = id |} user
            |> ActionResult.fromResultTask InternalError
    }

    let addExpToUser id exp =
        findUserByIdAsync id
        |> ActionResult.bindTask (fun user -> 
            usersCollection
            |> updateOneAsync 
                {| _id = user._id |}
                {| 
                    ``$set`` = {|
                        exp = user.exp + exp;
                        level = getLevel (user.exp + exp);
                        next = getNextExp (user.exp + exp);
                    |}
                |}
            |> ActionResult.fromResultTask BadRequest
        )

