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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCloudEvents();
            app.UseAuthorization();
            app.UseWebSockets(); // 使用 WebSocket 支持
            app.UseEndpoints(endpoints =>
            {
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
