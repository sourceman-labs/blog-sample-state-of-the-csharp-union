using Api.Contracts;

namespace Api;

public sealed class ApiHost
{
    private ApiHost() { }

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<OrderStore>();

        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
            o.SerializerOptions.RespectNullableAnnotations = true;
            o.SerializerOptions.RespectRequiredConstructorParameters = true;
        });

        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.UseStatusCodePages();
        app.MapOpenApi();
        app.MapOrders();

        return app;
    }
}
