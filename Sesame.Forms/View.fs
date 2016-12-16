namespace Sesame.Forms

open System

open Sesame

type XView = Xamarin.Forms.View

type CustomViewProperty<'T when 'T :> XView> =
    | Once of (FormsContext -> 'T -> unit)
    | Subscribe of (FormsContext -> 'T -> unit)
    | OnceAndSubscribe of (FormsContext -> 'T -> unit) * (FormsContext -> XView -> unit)

and CustomView =
    {
        Create: FormsContext -> XView
        Subscribe: FormsContext -> XView -> unit
    }

and View =
    | Custom of CustomView

and FormsContext (app: WeakReference<Xamarin.Forms.Application>) =
    inherit Context (Xamarin.Forms.Device.BeginInvokeOnMainThread)

    member this.CreateView (view: View) =
        match view with
        | Custom customView -> customView.Create this

    member this.SubscribeView (view: View) =
        match view with
        | Custom customView -> customView.Subscribe this

    member this.Application =
        match app.TryGetTarget () with
        | true, app -> app
        | _ -> null

    member val PageInitialized = false with get, set

type IView =

    abstract InitializedEvent : Event<unit>

    abstract AppearingEvent : Event<unit>

    abstract DisappearingEvent : Event<unit>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module View =

    let lift<'T when 'T :> XView and 'T :> IView> f (props: CustomViewProperty<'T> list) =
        {
            Create =
                fun context ->
                    let xView : 'T = f context

                    props
                    |> List.iter (function
                        | Once f -> f context xView
                        | OnceAndSubscribe (f, _) -> f context xView
                        | _ -> ()
                    )

                    xView :> XView

            Subscribe =
                fun context xView ->
                    props
                    |> List.iter (function
                        | Subscribe f -> f context (xView :?> 'T)
                        | OnceAndSubscribe (_, f) -> f context (xView :?> 'T)
                        | _ -> ()
                    ) 
        }
        |> View.Custom

    let onceProperty f =
        Once f

    let subscribeProperty f =
        Subscribe f

    let onceAndSubscribeProperty f g =
        OnceAndSubscribe (f, g)
