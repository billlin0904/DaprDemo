using Dapr.Client;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using TweetProvider.Actors;

namespace TweetProvider
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
