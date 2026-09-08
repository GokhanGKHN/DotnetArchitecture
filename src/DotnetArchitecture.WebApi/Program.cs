using DotnetArchitecture.Application;
using DotnetArchitecture.Persistence;
using DotnetArchitecture.WebApi.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Katman servisleri                                                                                                                                                     
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);

// 2. Global Exception Handler & ProblemDetails kayıtları                                                                                                                   
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. Exception Handler Middleware'i en başta devreye alıyoruz!                                                                                                             
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
