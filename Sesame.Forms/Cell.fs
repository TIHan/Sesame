namespace Sesame.Forms

open System

open Sesame

type FsCell (app: WeakReference<Xamarin.Forms.Application>, view: View) as this =
    inherit Xamarin.Forms.ViewCell ()

    let mutable context = Unchecked.defaultof<_>

    do
        context <- new FormsContext (app)
        this.View <- context.CreateView view
        context.SubscribeView view this.View
        (this.View :> obj :?> IView).InitializedEvent.Trigger ()

    override this.OnAppearing () =

        if context.PageInitialized then
            context.SubscribeView view this.View

        context.PageInitialized <- true

        base.OnAppearing ()

        (this.View :> obj :?> IView).AppearingEvent.Trigger ()

    override this.OnDisappearing () =
        base.OnDisappearing ()

        (this.View :> obj :?> IView).DisappearingEvent.Trigger ()

        context.ClearDisposables ()

    override this.OnBindingContextChanged () =
        base.OnBindingContextChanged ()

[<RequireQualifiedAccess>]
type Cell =
    | View of view: View * height: double

    member this.Build (app) =
        match this with
        | View (view, height) ->
            let cell = FsCell (app, view)
            cell.Height <- height
            cell :> Xamarin.Forms.Cell