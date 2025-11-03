using DistributedLock.Demo;
using FluentResults;
using Medallion.Threading;
using Medallion.Threading.Azure;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Mvc;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")!)
);

// Usando o pacote RedLock.net

//builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
//{
//    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var readLockMultiplexer = new RedLockMultiplexer(multiplexer);

//    return RedLockFactory.Create([readLockMultiplexer], loggerFactory);
//});

//builder.Services.AddSingleton<IDistributedLockService, RedLockDotNetService>();

// Usando o pacote DistributedLock.Redis

builder.Services.AddSingleton<IDistributedLockProvider>(sp =>
{
    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
    return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
});

// Usando o pacote DistributedLock.Azure

//builder.Services.AddSingleton<IDistributedLockProvider>(sp =>
//{
//    var blobContainerClient = new Azure.Storage.Blobs.BlobContainerClient(
//        builder.Configuration.GetConnectionString("AzureStorageAccountConnection")!,
//        "distributed-locks"
//    );
//    return new AzureBlobLeaseDistributedSynchronizationProvider(blobContainerClient);
//});

builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/lock-test-custom", async ([FromServices] IConnectionMultiplexer redis) =>
{
    var key = "my-distributed-lock-key";
    var db = redis.GetDatabase();

    if (await db.LockTakeAsync(key, Environment.MachineName, TimeSpan.FromSeconds(30)))
    {
        try
        {
            // Simulando uma operação
            await Task.Delay(20000);

            return Results.Ok("Lock acquired and operation completed.");
        }
        finally
        {
            await db.LockReleaseAsync(key, Environment.MachineName);
        }
    }

    return Results.InternalServerError($"Could not acquire lock for key {key}");
});

app.MapGet("/lock-test-lib", async ([FromServices] IDistributedLockService lockService) =>
{
    var key = "my-distributed-lock-key";
    var result = await lockService.ExecuteWithLock(key, TimeSpan.FromSeconds(5), async () =>
    {
        // Simulando uma operação
        await Task.Delay(20000);

        return Result.Ok();
    });

    return result.IsSuccess ?
        Results.Ok("Lock acquired and operation completed.") :
        Results.InternalServerError(result.Errors[0].Message);
});

app.Run();