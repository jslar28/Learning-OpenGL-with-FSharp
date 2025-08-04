module Learning_OpenGL_with_FSharp.Chapters.Chapter01

open System
open System.Drawing
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Silk.NET.Input

let private onRender (gl: GL) =
    gl.ClearColor Color.Firebrick
    gl.Clear (uint32 GLEnum.ColorBufferBit)
    
let private onLoad (window: IWindow) =
    // The signature of the KeyDown event handler is IKeyboard -> Key -> int -> unit,
    // so we need to create a function that matches this signature.
    let onKeyDown _ key _ = 
        if key = Key.Escape then
            printf "Escape key pressed, closing window...\n"
            window.Close()
    
    // The KeyUp event handler has the same signature, but we don't need to do anything on key up at the moment.
    let onKeyUp _ _ _ = ()
        
    let input = window.CreateInput()

    input.Keyboards |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (Action<IKeyboard, Key, int>(onKeyDown))
        keyboard.add_KeyUp (Action<IKeyboard, Key, int>(onKeyUp))
    )

let renderWindow = 
    let mutable options =  WindowOptions.Default
    options.Title <- "Learning OpenGL in F#"
    let window = Window.Create options
    
    let mutable gl: GL | null = null

    // Initialize gl in Load event (which is called after the window is created)
    window.add_Load (fun () ->
        gl <- GL.GetApi(window)
        onLoad(window)
    )

    window.add_Render (
         fun dt ->
            match gl with
            | null -> ()
            | gl -> onRender gl
    )

    window.Run ()
