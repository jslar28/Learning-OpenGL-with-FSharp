module Learning_OpenGL_with_FSharp.Chapters.Chapter00
open Silk.NET.Maths;
open Silk.NET.Windowing

let renderWindow =
    let options =
        let mutable o = WindowOptions.Default
        o.Size <- Vector2D<int>(800, 600)
        o.Title <- "My first Silk.NET application!"
        o
    
    let window = Window.Create options
    window.Run()