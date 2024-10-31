using Microsoft.OpenApi.Models;

namespace TweetViewer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 注入 WebSocketHandler 作為單例
            services.AddSingleton<WebSocketHandler>();
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
            if (env.IsDevelopment())
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
                    var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
                    await webSocketHandler.Handle(context);
                });
            });
        }
    }

}
