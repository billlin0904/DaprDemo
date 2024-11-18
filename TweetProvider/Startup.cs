using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Quartz;
using TweetProvider.Actors;
using Quartz.Spi;
using TweetProvider.Jobs;
using TweetProvider.DbContexts;

namespace TweetProvider
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var daprClient = new DaprClientBuilder().Build();
            var secretStoreName = "secretstore";

            Configuration = new ConfigurationBuilder()
                .AddConfiguration(Configuration)
                .AddDaprSecretStore(secretStoreName, daprClient)
                .Build();

            services.AddSingleton<IConfiguration>(Configuration);

            // 加入 Dapr 支援
            services.AddControllers().AddDapr();
            // 加入 Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TweetProvider API", Version = "v1" });
            });

            services.AddActors(option =>
            {
                option.Actors.RegisterActor<OrderStatusActor>();
            });

            services.AddDbContext<PublishEventDbContext>(options =>
            {
                var connectionString = Configuration["ConnectionStrings:PublishEventDB"];
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            services.AddQuartz();
            services.AddTransient<PublishEventDbContext>();
            services.AddSingleton<IJobFactory, PublishEventJobFactory>();
            services.AddTransient<PublishEventJob>();
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            //if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // 啟用 Swagger 中介軟體
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TweetProvider API v1");
                    c.RoutePrefix = string.Empty; // 這會將 Swagger 設為應用程式的根路徑
                });
            }

            //var quartz = app.ApplicationServices.GetRequiredService<QuartzStartup>();
            //lifetime.ApplicationStarted.Register(quartz.Start);
            //lifetime.ApplicationStopped.Register(quartz.Stop);

            app.UseRouting();
            app.UseCloudEvents();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                // 加入 Dapr 的訂閱端點，讓 Dapr 自動發現 Pub/Sub 訂閱
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
                endpoints.MapActorsHandlers();
            });
        }
    }
}
