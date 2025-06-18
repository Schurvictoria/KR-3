using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCors();

app.Map("/orders/{**rest}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        var targetUri = "http://orders-service:80/" + context.Request.Path.Value!.Replace("/orders", "api/orders");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri)
        {
            Content = context.Request.Body.CanRead ? new StreamContent(context.Request.Body) : null
        };
        
        foreach (var header in context.Request.Headers)
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            
        var response = await client.SendAsync(requestMessage);
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();
            
        await response.Content.CopyToAsync(context.Response.Body);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"Error processing request: {ex.Message}");
    }
});

app.Map("/payments/{**rest}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        var targetUri = "http://payments-service:80/" + context.Request.Path.Value!.Replace("/payments", "api/accounts");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri)
        {
            Content = context.Request.Body.CanRead ? new StreamContent(context.Request.Body) : null
        };
        
        foreach (var header in context.Request.Headers)
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            
        var response = await client.SendAsync(requestMessage);
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();
            
        await response.Content.CopyToAsync(context.Response.Body);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"Error processing request: {ex.Message}");
    }
});

app.Run(); 