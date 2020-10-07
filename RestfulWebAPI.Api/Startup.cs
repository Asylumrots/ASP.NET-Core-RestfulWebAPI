using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using RestfulWebAPI.Api.Data;
using RestfulWebAPI.Api.Services;

namespace RestfulWebAPI.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //����Э�� Accpet application/json application/xml
            services.AddControllers(setup =>
            {
                setup.ReturnHttpNotAcceptable = true;//����http�����ղ���ȷ���ݸ�ʽ��ʱ�򷵻�406 Ĭ��Ϊfalse�⴦����Ϊture
                //setup.OutputFormatters.Add(new XmlDataConstractSerializerOutputFormatters); //�ɸ�ʽ
            }).AddNewtonsoftJson(setup =>
                {
                    setup.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }).AddXmlDataContractSerializerFormatters()   //���xml��ʽ Ĭ��ֻ��json
                .ConfigureApiBehaviorOptions(setup =>
                {
                    setup.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Type = "http://www.baidu.com",
                            Title = "������һ������",
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Detail = "��鿴��ϸ��Ϣ",
                            Instance = context.HttpContext.Request.Path
                        };

                        problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);

                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            //ע�� Mapper����ģ��ӳ�䣩 ��ǰ��װ��MapperNuger��
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //ע�����
            services.AddScoped<ICompanyRepository, CompanyRepository>();

            //ע��DbContext����
            services.AddDbContext<MyDbContext>(option =>
            {
                option.UseSqlite("Data Source=routine.db");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else //���׳�δ������쳣ʱ�û���ҳ��
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unexpected Error!");
                    });
                });
            }

            app.UseStaticFiles();//����̬�ļ�

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
