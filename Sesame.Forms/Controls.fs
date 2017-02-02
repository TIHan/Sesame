namespace Sesame.Forms

open System
open System.Collections.ObjectModel

open Sesame

type FsStackLayout () =
    inherit Xamarin.Forms.StackLayout ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsAbsoluteLayout () =
    inherit Xamarin.Forms.StackLayout ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsLabel () =
    inherit Xamarin.Forms.Label ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsEntry () =
    inherit Xamarin.Forms.Entry ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsButton () =
    inherit Xamarin.Forms.Button ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsImage () =
    inherit Xamarin.Forms.Image ()

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()

type FsListView (app: WeakReference<Xamarin.Forms.Application>) =
    inherit Xamarin.Forms.ListView (Xamarin.Forms.ListViewCachingStrategy.RecycleElement)

    override this.CreateDefault (item: obj) = item :?> Xamarin.Forms.Cell

    interface IView with

        member val InitializedEvent = Event<unit> ()

        member val AppearingEvent = Event<unit> ()

        member val DisappearingEvent = Event<unit> ()


[<AutoOpen>]
module ViewComponentProperties =

    type LayoutOptions = Xamarin.Forms.LayoutOptions

    type TextAlignment = Xamarin.Forms.TextAlignment

    type ImageSource = Xamarin.Forms.ImageSource

    type Aspect = Xamarin.Forms.Aspect

    type Color = Xamarin.Forms.Color

    type AbsoluteLayoutFlags = Xamarin.Forms.AbsoluteLayoutFlags

    type Rectangle = Xamarin.Forms.Rectangle

    type StackOrientation = Xamarin.Forms.StackOrientation

    let inline orientation< 
                                    ^T when ^T : (member set_Orientation : StackOrientation -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                     > value =
        View.onceProperty (fun _ xView -> (^T : (member set_Orientation : StackOrientation -> unit) (xView, value)))

    let inline verticalOptions< 
                                    ^T when ^T : (member set_VerticalOptions : LayoutOptions -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                     > value =
        View.onceProperty (fun _ xView -> (^T : (member set_VerticalOptions : LayoutOptions -> unit) (xView, value)))

    let inline horizontalOptions< 
                                    ^T when ^T : (member set_HorizontalOptions : LayoutOptions -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_HorizontalOptions : LayoutOptions -> unit) (xView, value)))

    let inline xAlign< 
                                    ^T when ^T : (member set_XAlign : TextAlignment -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_XAlign : TextAlignment -> unit) (xView, value)))

    let inline widthRequest< 
                                    ^T when ^T : (member set_WidthRequest : float -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_WidthRequest : float -> unit) (xView, value)))

    let inline heightRequest< 
                                    ^T when ^T : (member set_HeightRequest : float -> unit) and
                                    ^T :> XView and 
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_HeightRequest : float -> unit) (xView, value)))

    let inline source< 
                                    ^T when ^T : (member set_Source : ImageSource -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_Source : ImageSource -> unit) (xView, value)))

    let inline aspect< 
                                    ^T when ^T : (member set_Aspect : Aspect -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_Aspect : Aspect -> unit) (xView, value)))   

    let inline backgroundColor< 
                                    ^T when ^T : (member set_BackgroundColor : Color -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > value =
        View.onceProperty (fun _ xView -> (^T : (member set_BackgroundColor : Color -> unit) (xView, value))) 

    let inline absoluteLayoutFlags value =
        View.onceProperty (fun _ xView -> Xamarin.Forms.AbsoluteLayout.SetLayoutFlags (xView, value))   

    let inline absoluteLayoutBounds value =
        View.onceProperty (fun _ xView -> Xamarin.Forms.AbsoluteLayout.SetLayoutBounds (xView, value))

    let inline children< ^T when 
                    ^T :> XView and
                    ^T :> IView and  
                    ^T : (member get_Children : unit -> XView System.Collections.Generic.IList)>
                        (children: View list) =
        let childrenViews = ResizeArray ()

        View.onceAndSubscribeProperty
            (fun context view ->
                children
                |> List.iter (fun child ->
                    let childView = context.CreateView child
                    ( ^T : (member get_Children : unit -> XView System.Collections.Generic.IList) (view)).Add (childView)
                    childrenViews.Add (WeakReference<XView> (childView))
                )
            )
            (fun context view ->
                (children, childrenViews)
                ||> Seq.iter2 (fun child childView -> 
                    match childView.TryGetTarget () with
                    | (true, childView) -> context.SubscribeView child childView
                    | _ -> ()
                )

                (view :> obj :?> IView).InitializedEvent.Publish
                |> Observable.subscribe (fun () ->
                    childrenViews
                    |> Seq.iter (fun childView ->
                        match childView.TryGetTarget () with
                        | (true, childView) -> 
                            (childView :> obj :?> IView).InitializedEvent.Trigger ()
                        | _ -> ()
                    )
                )
                |> context.AddDisposable

                (view :> obj :?> IView).AppearingEvent.Publish
                |> Observable.subscribe (fun () ->
                    childrenViews
                    |> Seq.iter (fun childView ->
                        match childView.TryGetTarget () with
                        | (true, childView) -> 
                            (childView :> obj :?> IView).AppearingEvent.Trigger ()
                        | _ -> ()
                    )
                )
                |> context.AddDisposable
            )

    module Dynamic =

        let inline text< 
                                    ^T when ^T : (member set_Text : string -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > (va: Val<string>) =
            View.onceProperty (fun context view ->
                context.Sink view (fun view value -> (^T : (member set_Text : string -> unit) (view, value))) va
            )

        let inline isRefreshing< 
                                    ^T when ^T : (member set_IsRefreshing : bool -> unit) and
                                    ^T :> XView and
                                    ^T :> IView
                                    > (va: Val<bool>) =
            View.onceProperty (fun context view ->
                context.Sink view (fun view value -> (^T : (member set_IsRefreshing : bool -> unit) (view, value))) va
            )

        let inline itemsSource< ^T when ^T :> FsListView> (va: ValList<Cell>) =
            View.onceProperty (
                (fun context view ->

                    let f = fun (view: ^T) index (value: Cell) ->
                        let items = view.ItemsSource :?> ObservableCollection<Xamarin.Forms.Cell>
                        let cell = value.Build (WeakReference<Xamarin.Forms.Application> (context.Application))
                        if index < items.Count then
                            items.[index] <- cell
                        else
                            items.Add (cell)

                    let g = fun (view: ^T) index ->
                        let items = view.ItemsSource :?> ObservableCollection<Xamarin.Forms.Cell>

                        if index < items.Count then
                            items.RemoveAt (index)

                    let h = fun (view: ^T) ->
                        let items = view.ItemsSource :?> ObservableCollection<Xamarin.Forms.Cell>

                        items.Clear ()

                    va |> context.SinkList view f g h
                )
            )

    module Event =
       
        let inline textChanged< 
                                    ^T when ^T :> XView
                                    and ^T :> IView
                                    and ^T : (member add_TextChanged : EventHandler<Xamarin.Forms.TextChangedEventArgs> -> unit)
                                    and ^T : (member remove_TextChanged : EventHandler<Xamarin.Forms.TextChangedEventArgs> -> unit)> f =
            View.subscribeProperty (fun context view' ->
                let del = EventHandler<Xamarin.Forms.TextChangedEventArgs> (fun _ args -> f args.NewTextValue)
                (^T : (member add_TextChanged : EventHandler<Xamarin.Forms.TextChangedEventArgs> -> unit) (view', del))
                { new IDisposable with

                    member this.Dispose () =
                        (^T : (member remove_TextChanged : EventHandler<Xamarin.Forms.TextChangedEventArgs> -> unit) (view', del))
                }
                |> context.AddDisposable
            )

        let inline clicked<         ^T when ^T :> XView
                                    and ^T :> IView
                                    and ^T : (member add_Clicked : EventHandler -> unit)
                                    and ^T : (member remove_Clicked : EventHandler -> unit)> f =
            View.subscribeProperty (fun context view' ->
                let del = EventHandler (fun _ _ -> f ())
                (^T : (member add_Clicked : EventHandler -> unit) (view', del))
                { new IDisposable with

                    member this.Dispose () =
                        (^T : (member remove_Clicked : EventHandler -> unit) (view', del))
                }
                |> context.AddDisposable
            )

        let inline refreshing<      ^T when ^T :> XView
                                    and ^T :> IView
                                    and ^T : (member add_Refreshing : EventHandler -> unit)
                                    and ^T : (member remove_Refreshing : EventHandler -> unit)> f =
            View.subscribeProperty (fun context view' ->
                let del = EventHandler (fun _ _ -> f ())
                (^T : (member add_Refreshing : EventHandler -> unit) (view', del))
                { new IDisposable with

                    member this.Dispose () =
                        (^T : (member remove_Refreshing : EventHandler -> unit) (view', del))
                }
                |> context.AddDisposable
            )

        let inline initialized f =
            View.subscribeProperty (fun context xView ->
                (xView :> IView).InitializedEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

        let inline appearing f =
            View.subscribeProperty (fun context xView ->
                (xView :> IView).AppearingEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

        let inline disappearing f =
            View.subscribeProperty (fun context xView ->
                (xView :> IView).DisappearingEvent.Publish
                |> Observable.subscribe f
                |> context.AddDisposable
            )

    module Command =

        let navigationPush (va: CmdVal<Page>) =
            View.onceProperty (fun context view ->
                va |> context.SinkCmd context.Application (fun app page ->
                    page.Build app
                )
            )

    let inline text value =
        Dynamic.text (Val.constant value)

[<AutoOpen>]
module Views =

    let stackLayout props children' =
        View.lift
            (fun _ -> FsStackLayout ()) 
            ([ children children' ] @ props)

    let absoluteLayout props children' =
        View.lift
            (fun _ -> FsAbsoluteLayout ()) 
            ([ children children' ] @ props)

    let label props =
        View.lift (fun _ -> FsLabel ()) props

    let entry props onTextChanged =
        View.lift 
            (fun _ -> FsEntry ()) 
            ([ Event.textChanged onTextChanged ] @ props)

    let button props onClick =
        View.lift 
            (fun _ -> FsButton ())
            ([ Event.clicked onClick ] @ props)

    let image props =
        View.lift (fun _ -> FsImage ()) props

    let listView props items =
        View.lift (fun context -> 
            let view = FsListView (WeakReference<Xamarin.Forms.Application> (context.Application))
            view.ItemsSource <- ObservableCollection<Xamarin.Forms.Cell> ()
            view.IsPullToRefreshEnabled <- true
            view
        ) ([ Dynamic.itemsSource items ] @ props)
