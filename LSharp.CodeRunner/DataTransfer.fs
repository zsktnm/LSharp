module Lsharp.CodeRunner.DataTransfer

type CodeDTO = {
    code: string;
}

type TaskResult = {
    TaskId: string;
    UserId: string;
    IsValid: bool;
}

