namespace Sesame.Forms.Sample.UITest

open System
open System.IO
open Xamarin.UITest
open Xamarin.UITest.Queries

module AppInitializer =
  let startApp (platform: Platform) =
    if platform = Platform.Android then
      ConfigureApp.Android.StartApp () :> IApp
    else
      ConfigureApp.iOS.PreferIdeSettings().AppBundle("../../../iOS/bin/iPhoneSimulator/Debug/Sesame.Forms.Sample.iOS.app").StartApp () :> _
