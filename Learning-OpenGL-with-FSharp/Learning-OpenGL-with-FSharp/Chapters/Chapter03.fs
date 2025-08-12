module Learning_OpenGL_with_FSharp.Chapters.Chapter03

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
let mutable program: uint = 0u

let mutable clearColor = Color.Firebrick

let private generateAndBindVertexArray : uint32 =
    match gl with
    | null ->
        printfn "OpenGL context is not initialized when generating Vertex Array."
        0u
    | ctx ->
        let vao = ctx.GenVertexArray()
        ctx.BindVertexArray(vao)
        vao
        
let private generateAndBindVertexBuffer =
    match gl with
    | null ->
        printfn "OpenGL context is not initialized when generating Vertex Buffer."
        0u
    | ctx ->
        let vbo = ctx.GenBuffer()
        ctx.BindBuffer(BufferTargetARB.ArrayBuffer, vbo)
        vbo
        
let private generateAndBindElementBuffer =
    match gl with
    | null ->
        printfn "OpenGL context is not initialized when generating Vertex Element Buffer."
        0u
    | ctx ->
        let ebo = ctx.GenBuffer()
        ctx.BindBuffer(BufferTargetARB.ArrayBuffer, ebo)
        ebo
        
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

let private createSimpleVertexShader (gl: GL) =
    let vertexCode =
        @"
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
        }"
        
    let vertexShader = gl.CreateShader(ShaderType.VertexShader);
    gl.ShaderSource(vertexShader, vertexCode)        
    gl.CompileShader(vertexShader);

    let vertexCompileStatus = gl.GetShader(vertexShader, ShaderParameterName.CompileStatus);
    if (vertexCompileStatus <> int GLEnum.True) then
        failwith("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vertexShader))
        
    vertexShader
    
let private createSimpleFragmentShader (gl: GL) =
    // Unlike a vertex shader, a fragment shader must always have at least one out attribute.
    // This attribute is used to specify the color of the pixel that will be drawn on the screen.
    // In OpenGL, the output color in the fragment shader is a normalized 32-bit float.
    // Therefore, each of the RGBA values must be between 0 and 1.
    let fragmentCode =
        @"
        #version 330 core

        out vec4 out_color;

        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }"
                
    let fragmentShader = gl.CreateShader(ShaderType.FragmentShader)
    gl.ShaderSource(fragmentShader, fragmentCode)
    gl.CompileShader(fragmentShader)
    
    let fragmentCompileStatus = gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus)
    if (fragmentCompileStatus <> int GLEnum.True) then
        failwith("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fragmentShader))
        
    fragmentShader
    
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
    
    let vao = generateAndBindVertexArray
    generateAndBindVertexBuffer |> ignore
    generateAndBindElementBuffer |> ignore
    
    match gl with
    | null -> failwith "OpenGL context is not initialized."
    | ctx ->
        bufferData ctx simpleQuadVertices BufferTargetARB.ArrayBuffer
        bufferData ctx simpleQuadIndices BufferTargetARB.ArrayBuffer

        let vertexShader = createSimpleVertexShader ctx
        let fragmentShader = createSimpleFragmentShader ctx
        
        program <- ctx.CreateProgram()
        
        ctx.AttachShader(program, vertexShader);
        ctx.AttachShader(program, fragmentShader);

        ctx.LinkProgram(program);

        let linkStatus = ctx.GetProgram(program, ProgramPropertyARB.LinkStatus);
        if (linkStatus <> int GLEnum.True) then
            failwith("Program failed to link: " + ctx.GetProgramInfoLog(program))
            
        ctx.DetachShader(program, vertexShader);
        ctx.DetachShader(program, fragmentShader);
        ctx.DeleteShader(vertexShader);
        ctx.DeleteShader(fragmentShader)
        
        let positionLoc = 0u; // The location of the vertex attribute in the vertex shader (aPosition)
        ctx.EnableVertexAttribArray(positionLoc)
        ctx.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, uint32 (3 * sizeof<float>), IntPtr(0).ToPointer())
        
        // Unbind the Vertex Array Object and Vertex Buffer Object to avoid accidental modifications
        ctx.BindVertexArray(0u)
        ctx.BindBuffer(BufferTargetARB.ArrayBuffer, 0u)
        ctx.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0u)
        
        ctx.BindVertexArray(vao)
        ctx.UseProgram(program)
        ctx.DrawElements(PrimitiveType.Triangles, uint32 simpleQuadIndices.Length, DrawElementsType.UnsignedInt, IntPtr(0).ToPointer())
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
