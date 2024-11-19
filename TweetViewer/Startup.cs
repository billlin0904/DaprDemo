using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using TweetViewer.Services;

namespace TweetViewer
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

            var connectionString = Configuration["ConnectionStrings:GameConfigRedis"];
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionString));
            services.AddSingleton<PlayerSettingsService>();
            // 注入 WebSocketHub 作為單例
            services.AddSingleton<WebSocketHub>();
            // 加入 Dapr 支援
            services.AddDaprClient();
            services.AddControllers().AddDapr();
            // 加入 Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TweetViewer API", Version = "v1" });
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TweetViewer API v1");
                    c.RoutePrefix = string.Empty; // 這會將 Swagger 設為應用程式的根路徑
                });
            }

            app.UseRouting();
            app.UseCloudEvents();
            app.UseAuthorization();
            app.UseWebSockets(); // 使用 WebSocket 支持
            app.UseEndpoints(endpoints =>
            {
                // 加入 Dapr 的訂閱端點，讓 Dapr 自動發現 Pub/Sub 訂閱
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();

                // 設置 WebSocket 路徑
                endpoints.Map("/ws", async context =>
                {
                    var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHub>();
                    await webSocketHandler.Handle(context);
                });
            });
        }
    }

}
