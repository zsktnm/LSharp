namespace LSharp.Problems

open Data
open Giraffe
open FluentValidation
open MongoDB.Bson
open Microsoft.AspNetCore.Http
open System

module DataTransfer =

    let listOfErrors (errors: Results.ValidationResult) = 
        errors.Errors 
        |> Seq.map (fun err -> err.ErrorMessage)

    let readDto<'a> (ctx: HttpContext) = task { 
        let! entity = ctx.BindModelAsync<'a>()
        let validator = 
            ctx.GetService<AbstractValidator<'a>>()

        match! validator.ValidateAsync(entity) with
        | result when not result.IsValid -> 
            return Error (listOfErrors result)
        | _ -> 
            return Ok entity
    }


    [<CLIMutable>]
    type CategoryDTO = {
        name: string;
        level: int;
    }


    let toCategory (dto: CategoryDTO) = {
        _id = ObjectId.GenerateNewId();
        name = dto.name;
        image = null;
        level = dto.level
    }


    type CategoryValidator() =
        inherit AbstractValidator<CategoryDTO>()
        do  
            base.RuleFor(fun c -> c.name)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100)
                |> ignore
            base.RuleFor(fun c -> c.level)
                .GreaterThan(0)
                .LessThan(100)
                |> ignore


    [<CLIMutable>]
    type TaskDTO = {
        name: string;
        category: string;
        require: int;
        exp: int;
        description: string;
    }


    let toLsharpTask (dto: TaskDTO) = {
        _id = ObjectId.GenerateNewId();
        name = dto.name;
        category = dto.category;
        require = dto.require;
        exp = dto.exp;
        description = dto.description;
        file = null;
        image = null;
    }


    type TaskValidator() =
        inherit AbstractValidator<TaskDTO>()
        do  
            base.RuleFor(fun t -> t.name)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100)
                |> ignore
            base.RuleFor(fun t -> t.require)
                .GreaterThan(0)
                .LessThan(100)
                |> ignore
            base.RuleFor(fun t -> t.category)
                .NotEmpty()
                |> ignore
            base.RuleFor(fun t -> t.exp)
                .GreaterThan(0)
                .LessThan(10000)
                |> ignore
            base.RuleFor(fun t -> t.description)
                .NotEmpty()
                |> ignore



    [<CLIMutable>]
    type CodeDTO = {
        taskId: string;
        code: string;
    }


    type CodeValidator() =
        inherit AbstractValidator<CodeDTO>()
        do  
            base.RuleFor(fun c -> c.taskId)
                .NotEmpty()
                .Must(fun id -> fst (ObjectId.TryParse(id)))
                .WithMessage("Invalid task id")
                |> ignore
            base.RuleFor(fun c -> c.code)
                .NotEmpty()
                .MaximumLength(1_000_000)
                |> ignore


    [<CLIMutable>]
    type SolutionViewDTO = {
        _id: ObjectId;
        user: string;
        task: string;
        getExp: bool;
        published: bool;
        solutions: SolutionItem array;
        comments: SolutionComment array;
        likes: int;
        isLiked: bool;
    }

    let toSolutionView userId (solution: Solution) = {
        _id = solution._id;
        user = solution.user;
        task = solution.task;
        getExp = solution.getExp;
        published = solution.published;
        comments = solution.comments;
        solutions = solution.solutions;
        likes = solution.likes |> Array.length;
        isLiked = solution.likes |> Array.contains userId
    }


    [<CLIMutable>]
    type PostCommentDTO = {
        solution: string;
        text: string;
    }


    type PostCommentValidator() =
        inherit AbstractValidator<PostCommentDTO>()
        do  
            base.RuleFor(fun c -> c.solution)
                .Must(fun id -> fst (ObjectId.TryParse(id)))
                .WithMessage("Invalid task id")
                |> ignore
            base.RuleFor(fun c -> c.text)
                .NotEmpty()
                .MaximumLength(2500)
                |> ignore
    

    
    [<CLIMutable>]
    type RemoveDTO = {
        solution: string;
        comment: SolutionComment;
    }


    type RemoveCommentValidator() =
        inherit AbstractValidator<RemoveDTO>()
        do  
            base.RuleFor(fun c -> c.solution)
                .NotEmpty()
                .MaximumLength(2500)
                |> ignore
