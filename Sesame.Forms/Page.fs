namespace Sesame.Forms

open System

open Sesame

type FsContentPage (app: WeakReference<Xamarin.Forms.Application>, view: View) as this =
    inherit Xamarin.Forms.ContentPage ()

    let mutable context = Unchecked.defaultof<_>

    do
        context <- new FormsContext (app)
        this.Content <- context.CreateView view
        context.SubscribeView view this.Content
        (this.Content :> obj :?> IView).InitializedEvent.Trigger ()

    override this.OnAppearing () =

        if context.PageInitialized then
            context.SubscribeView view this.Content

        context.PageInitialized <- true

        base.OnAppearing ()

        (this.Content :> obj :?> IView).AppearingEvent.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        (this.Content :> obj :?> IView).DisappearingEvent.Trigger ()

        context.ClearDisposables ()

    static member Create (app, view) =
        FsContentPage (WeakReference<Xamarin.Forms.Application> (app), view)

type ToolbarItem =
    {
        Text: string
        Icon: string
        Activated: unit -> unit
    }

[<RequireQualifiedAccess>]
type Page =
    | Content of content: View * title: string * toolbarItems: ToolbarItem list

    member this.Build (app: Xamarin.Forms.Application) =
        match this with
        | Content (comp, title, toolbarItems) ->
            let contentPage = FsContentPage (WeakReference<Xamarin.Forms.Application> (app), comp)
            contentPage.Title <- title
            toolbarItems
            |> List.iter (fun x ->
                Xamarin.Forms.ToolbarItem (x.Text, x.Icon, Action (x.Activated))
                |> contentPage.ToolbarItems.Add
            )

            try
                app.MainPage.Navigation.PushAsync (contentPage) |> ignore
            with | _ ->
                app.MainPage <- contentPage |> Xamarin.Forms.NavigationPage
