using TweetViewer;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .UseSerilog((context, services, configuration) =>
            {
                var seqServerUrl = context.Configuration["SeqServerUrl"];
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .WriteTo.Console()
                    .WriteTo.Seq(seqServerUrl!)
                    .Enrich.WithProperty("ApplicationName", "TweetViewer API");
            });
}

