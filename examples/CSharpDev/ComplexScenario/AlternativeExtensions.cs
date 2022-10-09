using System;
using Microsoft.FSharp.Core;
using NBomber.Contracts;

namespace CSharpDev.ComplexScenario;

public static class AlternativeExtensions
{
    public static T Measure<T>(this IStepContext<Unit, Unit> context, string actionName, Func<Response<T>> func)
    {
        // Do some tracking here and push metrics through context etc.
        var response = func();

        if (response.IsError)
        {
            throw new Exception($"{actionName}: Some error with code {response.StatusCode}.");
        }

        return response.Data;
    }
}

public class Response<T>
{
    public T Data { get; }

    public int StatusCode { get; }

    public bool IsError { get; }

    // .. other fields

    public Response(T data, int statusCode, bool isError)
    {
        Data = data;
        StatusCode = statusCode;
        IsError = isError;
    }
}

public static class MyResponse
{
    public static Response<T> Ok<T>(T data) => new Response<T>(data, 200, false);
    public static Response<T> Fail<T>() => new Response<T>(default, 400, true);
}
