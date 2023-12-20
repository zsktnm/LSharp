module Lsharp.Registration

open Giraffe.ViewEngine
open Layout


[<CLIMutable>]
type RegistrationViewModel = {
    Email: string;
    Password: string;
    RepeatPassword: string;
    Errors: string list;
}
 
let newViewModel () = {
    Email = ""
    Password = ""
    RepeatPassword = ""
    Errors = []
}

let Registration (viewModel: RegistrationViewModel) = 
    let markup = 
        form [ _class "container"; _method "POST" ] [
            h1 [ _class "title" ] [ str "Регистрация" ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Email: " ]
                div [ _class "control" ] [
                    input [ 
                        _class "input" 
                        _type "email" 
                        _placeholder "mail@example.com"
                        _name "Email" 
                        _value viewModel.Email
                    ]
                ]
            ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Пароль: " ]
                div [ _class "control" ] [
                    input [ 
                        _class "input"
                        _type "password"
                        _placeholder "Укажите надежный пароль"
                        _name "Password" 
                        _value viewModel.Password
                        _required
                    ]
                ]
            ]
            div [ _class "field" ] [
                label [ _class "label" ] [ str "Повторите пароль: " ]
                div [ _class "control" ] [
                    input [ 
                        _class "input" 
                        _type "password" 
                        _placeholder "Повторите свой надежный пароль" 
                        _name "RepeatPassword" 
                        _value viewModel.RepeatPassword
                        _required
                    ]
                ]
            ]

            ul [] (viewModel.Errors 
                |> List.map (fun err -> li [] [str err]))

            div [ _class "field" ] [
                div [ _class "control" ] [
                    button [ _class "button is-primary mt-5" ] [ str "Зарегистрироваться" ]
                ]
            ]
        ]

    Layout markup