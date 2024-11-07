using Dapr.Client;
using Dapr.Extensions.Configuration;
using TweetProvider;
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            //.ConfigureAppConfiguration(config =>
            //{
            //    config.AddDaprSecretStore("eshopondapr-secretstore",
            //        new DaprClientBuilder().Build());
            //})
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}