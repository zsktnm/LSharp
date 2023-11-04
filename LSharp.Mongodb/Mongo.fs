namespace LSharp.Mongodb
open MongoDB
open MongoDB.Bson
open MongoDB.Driver
open BuildHelpers

module Mongo = 
    open System
    open MongoDB.Driver.Linq
    open System.Text.Json
    

    let client (connectionString: string) = MongoClient(connectionString)


    let database (database: string) (client: MongoClient) = 
        client.GetDatabase(database)


    let collection<'a> (collection: string) (database: IMongoDatabase) = 
        database.GetCollection<'a>(collection)


    let toOption object = 
        if obj.ReferenceEquals(object, null) then
            None
        else
            Some object
    

    let serializeFilter filter : FilterDefinition<'a> =
        JsonSerializer.Serialize(filter) 
        |> FilterDefinition.op_Implicit


    let serializeUpdate update : UpdateDefinition<'a> = 
        JsonSerializer.Serialize(update) 
        |> UpdateDefinition<'a>.op_Implicit

    let tryParseOid id = 
        let result = ObjectId.TryParse(id)
        match result with
        | (false, _) -> None
        | (true, objId) -> Some objId 


    let find filter (collection: IMongoCollection<'a>) = 
        let f = serializeFilter filter
        collection.Find(f);


    let getAll (collection: IMongoCollection<'a>) = 
        collection.Find(FilterDefinition<'a>.Empty)
        

    let findAsync filter (collection: IMongoCollection<'a>) = task {
        let json = JsonSerializer.Serialize(filter)
        return! collection.FindAsync(json)
    }


    let one (found: IFindFluent<'a, 'a>) = 
        found.FirstOrDefault() |> toOption


    let oneAsync (found: IFindFluent<'a, 'a>) = task {
        let! result = found.FirstOrDefaultAsync()
        return toOption result
    }


    let enumerateAll (found: IFindFluent<'a, 'a>) = 
        found.ToEnumerable()


    let take count (found: IFindFluent<'a, 'a>) = 
        found.Limit(count)


    let skip count (found: IFindFluent<'a, 'a>) = 
        found.Skip(count)


    let count (found: IFindFluent<'a, 'a>) = 
        found.CountDocuments()


    let toListAsync (found: IFindFluent<'a, 'a>) = task {
        return! found.ToListAsync()
    }


    let findByIdAsync id (collection: IMongoCollection<'a>) = task {
        match tryParseOid id with
        | None -> return None
        | Some id -> 
            return! collection
            |> find {| _id = oid id |}
            |> oneAsync
    }
    

    let insertOne (document: 'a) (collection: IMongoCollection<'a>) = 
        collection.InsertOne(document) 


    let insertOneAsync (document: 'a) (collection: IMongoCollection<'a>) = 
        collection.InsertOneAsync(document) 
    

    let insertMany (documents: 'a seq) (collection: IMongoCollection<'a>) = 
        collection.InsertMany(documents)


    let updateOne 
        filter
        update 
        (collection: IMongoCollection<'a>) = 
            let result = 
                collection.UpdateOne(
                    filter |> serializeFilter, 
                    update |> serializeUpdate
                )
            match result with
            | result when result.IsAcknowledged -> Ok "Updated"
            | _ -> Error "Error while updating"


    let updateOneAsync 
        filter 
        update 
        (collection: IMongoCollection<'a>) = task {
            let! result = 
                collection.UpdateOneAsync(
                    filter |> serializeFilter, 
                    update |> serializeUpdate
                )
            match result with
            | result when result.IsAcknowledged -> return Ok "Updated"
            | _ -> return Error "Error while updating"
    }


    let updateByIdAsync 
        id 
        update 
        (collection: IMongoCollection<'a>) = task {
            let updateResult = 
                match tryParseOid id with
                | None -> Error "Invalid Id"
                | Some id -> 
                    Ok (collection.UpdateOneAsync(
                        {| _id = oid id |} |> serializeFilter, 
                        update |> serializeUpdate
                    ))
            match updateResult with
            | Ok result -> 
                let! r = result
                if r.IsAcknowledged then 
                    return Ok "Success"
                else
                    return Error "Error while update"
            | Error err -> return Error err

        }


    let updateMany 
        (filter: FilterDefinition<'a>) 
        (update: UpdateDefinition<'a>) 
        (collection: IMongoCollection<'a>) = 
            let result = 
                collection.UpdateMany(filter |> serializeFilter, update)
            match result with
            | result when result.IsAcknowledged -> Ok "Updated"
            | _ -> Error "Error while updating"


    let replaceOne 
        filter 
        document 
        (collection: IMongoCollection<'a>) = 
            let result = 
                collection.ReplaceOne(filter |> serializeFilter, document)
            match result with
            | result when result.IsAcknowledged -> Ok "Updated"
            | _ -> Error "Error while updating"

    let replaceOneAsync
        filter 
        document 
        (collection: IMongoCollection<'a>) = task {
            let result = 
                collection.ReplaceOneAsync(filter |> serializeFilter, document)
            match! result with
            | result when result.IsAcknowledged -> return Ok "Updated"
            | _ -> return Error "Error while updating"
    }

    let deleteOne 
        filter
        (collection: IMongoCollection<'a>) = 
            let result = 
                collection.DeleteOne(filter |> serializeFilter)
            match result with
            | result when result.IsAcknowledged -> Ok "Deleted"
            | _ -> Error "Error while deleting"

    let deleteOneAsync
        filter
        (collection: IMongoCollection<'a>) = task {
            let! result = 
                collection.DeleteOneAsync(filter |> serializeFilter)
            match result with
            | result when result.DeletedCount > 0 -> return Ok "Deleted"
            | result when result.IsAcknowledged -> return Error "Nothing to delete"
            | _ -> return Error "Error while deleting"
    }   


    let deleteByIdAsync
        id
        (collection: IMongoCollection<'a>) = task {
            match tryParseOid id with
            | None -> return Error "Invalid Id"
            | Some id -> 
                return! collection
                |> deleteOneAsync {| _id = oid id |}
    }


    let deleteMany 
        (filter: FilterDefinition<'a>) 
        (collection: IMongoCollection<'a>) = 
            let result = 
                collection.DeleteMany(filter |> serializeFilter)
            match result with
            | result when result.IsAcknowledged -> Ok "Deleted"
            | _ -> Error "Error while deleting"



