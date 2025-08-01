﻿using BlazorBYOD.Server;
using BlazorBYOD.Server.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Register AI/Search/OpenAI services in one line:
builder.Services.AddAI(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.MapApiEndpoints();

await app.RunAsync();