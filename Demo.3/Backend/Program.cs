﻿using System;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;
using Serilog.Filters;

class Program
{
    public const string ConnectionString = @"Data Source=(local);Database=OnlyOnce.Demo3.Orders;Integrated Security=True";

    static void Main(string[] args)
    {
        Start().GetAwaiter().GetResult();
    }

    static async Task Start()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.With(new ExceptionMessageEnricher())
            .Filter.ByExcluding(Matching.FromSource("NServiceBus.Transport.Msmq.QueuePermissions"))
            .Filter.ByExcluding(Matching.FromSource("NServiceBus.SubscriptionReceiverBehavior"))
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{ExceptionMessage}{NewLine}")
            .CreateLogger();

        LogManager.Use<SerilogFactory>();

        Console.Title = "Orders";

        var config = new EndpointConfiguration("OnlyOnce.Demo3.Orders");
        config.UsePersistence<InMemoryPersistence>();
        config.UseTransport<MsmqTransport>().Transactions(TransportTransactionMode.ReceiveOnly);
        config.Recoverability().Immediate(x => x.NumberOfRetries(5));
        config.LimitMessageProcessingConcurrencyTo(10);
        config.Recoverability().AddUnrecoverableException(typeof(DbEntityValidationException));
        config.SendFailedMessagesTo("error");
        config.Pipeline.Register(new DeduplicatingBehavior(), "Deduplicates incoming messages.");
        config.EnableInstallers();

        SqlHelper.EnsureDatabaseExists(ConnectionString);

        using (var receiverDataContext = new OrdersDataContext(new SqlConnection(ConnectionString)))
        {
            receiverDataContext.Database.Initialize(true);
        }

        var endpoint = await Endpoint.Start(config).ConfigureAwait(false);

        Console.WriteLine("Press <enter> to exit.");
        Console.ReadLine();

        await endpoint.Stop().ConfigureAwait(false);
    }
}