module Learning_OpenGL_with_FSharp.Chapters.Chapter02

open System
open System.Drawing
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Silk.NET.Input
open Microsoft.FSharp.NativeInterop

#nowarn "9" // Ignore warnings about fixed size arrays
        
// Since initializing OpenGL will throw an exception if the OpenGL window is not loaded,
// we need to handle the OpenGL context initialization in a way that allows us to use it
// in multiple scopes. We can do this by using a mutable variable that will hold the GL instance.
let mutable gl: GL | null = null

let mutable clearColor = Color.Firebrick

let private generateAndBindVertexArray =
    match gl with
    | null -> printfn "OpenGL context is not initialized when generating Vertex Array."
    | ctx ->
        let vao = ctx.GenVertexArray()
        ctx.BindVertexArray(vao)
        
let private generateAndBindVertexBuffer =
    match gl with
    | null -> printfn "OpenGL context is not initialized when generating Vertex Buffer."
    | ctx ->
        let vbo = ctx.GenBuffer()
        ctx.BindBuffer(BufferTargetARB.ArrayBuffer, vbo)
        
let private generateAndBindElementBuffer =
    match gl with
    | null -> printfn "OpenGL context is not initialized when generating Vertex Element Buffer."
    | ctx ->
        let ebo = ctx.GenBuffer()
        ctx.BindBuffer(BufferTargetARB.ArrayBuffer, ebo)
        
let private bufferData<'T when 'T : unmanaged> (ctx: GL) (data: 'T[]) (target: BufferTargetARB) =
    // Pin the data array onto the stack memory, so that it won't be garbage collected
    use indicesDataIntPtr = fixed data
    // Convert the typed pointer to a void pointer for OpenGL's BufferData function
    let indicesDataVoidPtr = NativePtr.toVoidPtr indicesDataIntPtr 
    // Calculate the total size in bytes of the vertex data to upload to the GPU
    let indicesCount = data.Length
    let indicesDataBytesSize = unativeint (indicesCount * sizeof<uint32>)
    ctx.BufferData(target, indicesDataBytesSize, indicesDataVoidPtr, BufferUsageARB.StaticDraw)    

let private simpleQuadVertices = 
    let vertices: float32[] = 
        [|
           -0.5f; -0.5f; 0.0f;  // Bottom Left
           0.5f; -0.5f; 0.0f;   // Bottom Right
           0.5f;  0.5f; 0.0f;   // Top Right
           -0.5f;  0.5f; 0.0f   // Top Left
        |] 
    vertices
    
let private simpleQuadIndices = 
    [|
        0u; 1u; 2u; // First Triangle
        2u; 3u; 0u  // Second Triangle
    |]

let private onRender (gl: GL) =
    gl.ClearColor clearColor
    gl.Clear (uint32 GLEnum.ColorBufferBit)
    
let private onLoad (window: IWindow) =
    // The signature of the KeyDown event handler is IKeyboard -> Key -> int -> unit,
    // so we need to create a function that matches this signature.
    let onKeyDown _ key _ = 
        if key = Key.Escape then
            printf "Escape key pressed, closing window...\n"
            window.Close()
        if key = Key.A then clearColor <- Color.DarkSlateBlue
        if key = Key.F then clearColor <- Color.Firebrick
        
    // The KeyUp event handler has the same signature, but we don't need to do anything on key up at the moment.
    let onKeyUp _ _ _ = ()

    let input = window.CreateInput()

    input.Keyboards |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (Action<IKeyboard, Key, int>(onKeyDown))
        keyboard.add_KeyUp (Action<IKeyboard, Key, int>(onKeyUp))
    )
    
    generateAndBindVertexArray
    generateAndBindVertexBuffer
    
    match gl with
    | null -> failwith "OpenGL context is not initialized."
    | ctx ->
        bufferData ctx simpleQuadVertices BufferTargetARB.ArrayBuffer
        bufferData ctx simpleQuadIndices BufferTargetARB.ArrayBuffer
    ()

let renderWindow = 
    let mutable options =  WindowOptions.Default
    options.Title <- "Learning OpenGL in F#"
    let window = Window.Create options

    // Initialize gl in Load event (which is called after the window is created)
    window.add_Load (fun () ->
        gl <- window.CreateOpenGL()
        onLoad(window)
    )

    window.add_Render (
         fun dt ->
            match gl with
            | null -> ()
            | gl -> onRender gl
    )

    window.Run ()
