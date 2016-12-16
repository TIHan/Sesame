namespace Sesame.Forms.Sample

open Sesame
open Sesame.Forms

module Test =

    let createView () =
        stackLayout [ horizontalOptions LayoutOptions.Center ] 
            [
                label [ text "Welcome to Forms!" ]
            ]

type App() as this =
    inherit Xamarin.Forms.Application()

    do
        Page.Content(Test.createView(), []).Build(this)
