var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(x => x.AddDefaultPolicy(policyBuilder =>
    policyBuilder.WithOrigins("https://chat.openai.com").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("openapi", new OpenApiInfo
    {
        Description = "A plugin that allows the user to create and manage a TODO list using ChatGPT. If you do not know the user's username, ask them first before making queries to the plugin. Otherwise, use the username \"global\".",
        Version = "v1",
        Title = "TODO Plugin"
    });
    c.AddServer(new OpenApiServer() { Url = "http://localhost:5000" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(x => x.RouteTemplate = "{documentName}.yaml");
    app.UseSwaggerUI(x =>
    {
        x.RoutePrefix = "";
        x.SwaggerEndpoint("/openapi.yaml", "TODO Plugin");
    });
}

var todos = new Dictionary<string, List<string>>();

app.MapPost("/todos/{username}", (string username, [FromBody] AddTodoRequest request) =>
{
    var todo = request.Todo;
    if (!todos.ContainsKey(username))
    {
        todos[username] = new List<string>();
    }
    todos[username].Add(todo);
    return todo;
})
.Produces<string>()
.WithOpenApi(operation =>
{
    operation.OperationId = "addTodo";
    operation.Summary = "Add a todo to the list";
    var parameter = operation.Parameters[0];
    parameter.Description = "The name of the user.";
    return operation;
});


app.MapGet("/todos/{username}", (string username) =>
    Results.Json(todos.TryGetValue(username, out var todo) ? todo : Array.Empty<string>())
)
.Produces<List<string>>()
.WithOpenApi(operation =>
{
    operation.OperationId = "getTodos";
    operation.Summary = "Get the list of todos";

    var parameter = operation.Parameters[0];
    parameter.Description = "The name of the user.";

    operation.Responses["200"].Description = "The list of todos";
    return operation;
});


app.MapDelete("/todos/{username}", (string username, [FromBody] DeleteTodoRequest request) =>
{
    var todoIdx = request.TodoIdx;
    if (todos.ContainsKey(username) && 0 <= todoIdx && todoIdx < todos[username].Count)
    {
        todos[username].RemoveAt(todoIdx);
    }
})
.Produces<List<string>>()
.WithOpenApi(operation =>
{
    operation.OperationId = "getTodos";
    operation.Summary = "Delete a todo from the list";
    operation.Parameters[0].Description = "The name of the user.";
    return operation;
});

app.MapGet("/logo.png", () => Results.File("logo.png", contentType: "image/png"))
    .ExcludeFromDescription();

app.MapGet("/.well-known/ai-plugin.json", () => Results.File("ai-plugin.json", contentType: "text/json"))
    .ExcludeFromDescription();

app.Run();

/// <summary>
/// AddTodoRequest Dto
/// </summary>
/// <param name="Todo">The todo to add to the list.</param>
internal record AddTodoRequest(string Todo);

/// <summary>
/// DeleteTodoRequest Dto
/// </summary>
/// <param name="TodoIdx">The index of the todo to delete.</param>
internal record DeleteTodoRequest(int TodoIdx);