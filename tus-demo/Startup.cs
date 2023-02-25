using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tus_demo.Endpoints;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;

namespace tus_demo
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
            services.AddSingleton(CreateTusConfiguration);

            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseTus(httpContext => Task.FromResult(httpContext.RequestServices.GetService<DefaultTusConfiguration>()));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapGet("/files/{fileId}", DownloadFileEndpoint.HandleRoute);
            });
        }

        private DefaultTusConfiguration CreateTusConfiguration(IServiceProvider serviceProvider)
        {
            var env = (IWebHostEnvironment)serviceProvider.GetRequiredService(typeof(IWebHostEnvironment));

            //�ļ��ϴ�·��
            var tusFiles = Path.Combine(env.WebRootPath, "tusfiles");

            return new DefaultTusConfiguration
            {
                UrlPath = "/files",
                //�ļ��洢·��
                Store = new TusDiskStore(tusFiles, true),
                //Ԫ�����Ƿ������ֵ
                MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
                //�ļ����ں��ٸ���
                Expiration = new AbsoluteExpiration(TimeSpan.FromMinutes(5)),
                //�¼����������¼������������裩
                Events = new Events
                {
                    //�ϴ�����¼��ص�
                    OnFileCompleteAsync = async ctx =>
                    {
                        //��ȡ�ϴ��ļ�
                        var file = await ctx.GetFileAsync();

                        //��ȡ�ϴ��ļ�Ԫ����
                        var metadatas = await file.GetMetadataAsync(ctx.CancellationToken);

                        //��ȡ�����ļ�Ԫ�����е�Ŀ���ļ�����
                        var fileNameMetadata = metadatas["name"];

                        //Ŀ���ļ�����base64���룬����������Ҫ����
                        var fileName = fileNameMetadata.GetString(Encoding.UTF8);

                        var extensionName = Path.GetExtension(fileName);

                        //���ϴ��ļ�ת��Ϊʵ��Ŀ���ļ�
                        File.Move(Path.Combine(tusFiles, ctx.FileId), Path.Combine(tusFiles, $"{ctx.FileId}"));


                        //var terminationStore = ctx.Store as ITusTerminationStore;

                        //await terminationStore!.DeleteFileAsync(file.Id, ctx.CancellationToken);
                    }
                }
            };
        }

    }
}
