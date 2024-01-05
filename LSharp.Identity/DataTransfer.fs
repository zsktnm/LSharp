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

type ChangePasswordDTO = {
    OldPassword: string;
    NewPassword: string;
}