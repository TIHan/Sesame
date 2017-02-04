namespace Sesame.Forms.Sample.UITest

open System
open NUnit.Framework
open Xamarin.UITest
open Xamarin.UITest.Queries

module TestDSL =

    type UITest<'T> = UITest of (IApp -> 'T) with

        member this.Run (app: IApp) =
            match this with
            | UITest f -> f app

    type UITestBuilder() =

        member x.Bind (v: UITest<'a>, f: 'a -> UITest<'b>) : UITest<'b> = 
            UITest (
                fun app ->
                    match v with
                    | UITest vf -> 
                        let result = (vf app)
                        match f result with
                        | UITest f -> f app
            )

        member x.Return v = UITest (fun _ -> v)

        member x.ReturnFrom o = o

        member x.Delay f = f ()

        member x.Zero () = UITest (fun _ -> ())

    let uiTest = UITestBuilder()

    type UIQuery = UIQuery of (AppQuery -> AppQuery) with

        member this.Run (appQuery: AppQuery) =
            match this with
            | UIQuery f -> f appQuery

        static member Run (appQuery: AppQuery, q: UIQuery) =
            q.Run (appQuery)

        static member Run (appQuery: AppQuery, qs: UIQuery list) =
            (appQuery, qs)
            ||> List.fold (fun appQuery q ->
                q.Run (appQuery)
            )

    let scrollUp (withinMarked: string) scrollStrategy swipePercentage swipeSpeed withInertia =
        fun (app: IApp) ->
            app.ScrollUp (withinMarked, scrollStrategy, swipePercentage, swipeSpeed, withInertia)
        |> UITest

    let waitForElement (qs: UIQuery list) timeoutMessage (timeout: TimeSpan option) (retryFrequency: TimeSpan option) (postTimeout: TimeSpan option) =
        fun (app: IApp) ->

            let timeout =
                Option.toNullable timeout

            let retryFrequency =
                Option.toNullable retryFrequency

            let postTimeout =
                Option.toNullable postTimeout

            let qf = fun appQuery -> UIQuery.Run (appQuery, qs)

            app.WaitForElement (qf, timeoutMessage, timeout, retryFrequency, postTimeout)
        |> UITest

    let childByIndex (index: int) =
        UIQuery (fun appQuery -> appQuery.Child index)

    let childByClassName (className: string) =
        UIQuery (fun appQuery -> appQuery.Child className)

    let marked text =
        UIQuery (fun appQuery -> appQuery.Marked text)

    let text str =
        UIQuery (fun appQuery -> appQuery.Text str)

    let index i =
        UIQuery (fun appQuery -> appQuery.Index i)

    let descendant =
        UIQuery (fun appQuery -> appQuery.Descendant null)

    let descendantByIndex (i: int) =
        UIQuery (fun appQuery -> appQuery.Descendant i)

open TestDSL

type Tests() =

  //[<TestCase (Platform.Android)>]
  [<TestCase (Platform.iOS)>]
  member this.AppLaunches (platform: Platform) =
    let platform = platform
    let app = AppInitializer.startApp (platform)
    app.Screenshot ("First screen.") |> ignore

    let queryExoplanetItems =
        [
            marked "ExoplanetStackLayout"
            descendant
            text "HD 106270"
        ]

    let test =
        uiTest {
            do! scrollUp "KeplerList" ScrollStrategy.Gesture 0.5 50 false

            let! results = waitForElement queryExoplanetItems "Cannot find planets." (Some <| TimeSpan.FromSeconds (30.)) (Some <| TimeSpan.FromSeconds(1.)) None

            printfn "done."
        }

    test.Run app

    ()
