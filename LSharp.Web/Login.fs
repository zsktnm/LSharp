module Lsharp.Login

open Giraffe.ViewEngine
open Layout


[<CLIMutable>]
type LoginViewModel = {
    Email: string;
    Password: string;
    Errors: string list;
}

[<CLIMutable>]
type TokenDTO = {
    Token: string;
    RefreshToken: string;
}
 
let newViewModel () = {
    Email = ""
    Password = ""
    Errors = []
}

let Login (viewModel: LoginViewModel) = 
    let markup = 
        div [ _class "content-wrapper" ] [
            form [ _class "vertical-stack max-600 box-shadow p-1"; _method "POST" ] [
                h1 [ _class "title" ] [ str "Вход в систему" ]
                label [ _class "label" ] [ str "Email: " ]
                input [ 
                    _class "input" 
                    _type "email" 
                    _placeholder "mail@example.com"
                    _name "Email" 
                    _value viewModel.Email
                ]
                label [ _class "label" ] [ str "Пароль: " ]
                input [ 
                    _class "input"
                    _type "password"
                    _placeholder "Введите пароль"
                    _name "Password" 
                    _value viewModel.Password
                    _required
                ]

                ul [ _class "validation-errors" ] (viewModel.Errors 
                    |> List.map (fun err -> li [] [str err]))

                button [ _class "button primary-color" ] [ str "Вход в систему" ]

            ]
        ]

    Layout markup