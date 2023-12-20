module LSharp.Identity.DataTransfer

open FluentValidation

type UserDTO = {
    Email: string;
    Password: string;
}

type TokenDTO = {
    Token: string;
    RefreshToken: string;
}

