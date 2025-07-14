open System.Drawing
open Silk.NET.OpenGL
open Silk.NET.Windowing

[<EntryPoint>]
let main argv =
    printfn "Starting..."

    let mutable options =  WindowOptions.Default
    options.Title <- "Learning OpenGL in F#"
    
    let window = Window.Create options       
    let mutable gl: GL = null

    // Initialize gl in Load event (which is called after the window is created)
    window.add_Load (fun () ->
        gl <- GL.GetApi(window)
    )

    window.add_Render (
         fun dt ->
            if not (isNull gl) then
                gl.ClearColor Color.Firebrick
                gl.Clear (uint32 GLEnum.ColorBufferBit))

    window.Run ()
    0
